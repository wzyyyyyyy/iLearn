using Polly;
using System.Net.Http;
using System.Net.Sockets;

namespace iLearn.Helpers.Extensions
{
    public static class HttpClientRetryExtensions
    {
        private static readonly IAsyncPolicy<HttpResponseMessage> DefaultRetryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<SocketException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds(Math.Pow(4, retryAttempt)) +
                    TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)));


        public static async Task<HttpResponseMessage> SendWithRetryAsync(
            this HttpClient httpClient,
            HttpRequestMessage request,
            CancellationToken cancellationToken = default)
        {
            return await SendWithRetryAsync(httpClient, request, DefaultRetryPolicy, cancellationToken);
        }

        public static async Task<HttpResponseMessage> SendWithRetryAsync(
            this HttpClient httpClient,
            HttpRequestMessage request,
            IAsyncPolicy<HttpResponseMessage> retryPolicy,
            CancellationToken cancellationToken = default)
        {
            // 第一次尝试直接使用原始请求，避免不必要的克隆
            var firstAttempt = true;

            return await retryPolicy.ExecuteAsync(async () =>
            {
                HttpRequestMessage requestToSend;

                if (firstAttempt)
                {
                    firstAttempt = false;
                    requestToSend = request;
                }
                else
                {
                    requestToSend = await CloneRequestAsync(request);
                }

                return await httpClient.SendAsync(requestToSend, cancellationToken);
            });
        }


        public static async Task<HttpResponseMessage> SendWithRetryAsync(
            this HttpClient httpClient,
            Func<HttpRequestMessage> requestFactory,
            CancellationToken cancellationToken = default)
        {
            return await SendWithRetryAsync(httpClient, requestFactory, DefaultRetryPolicy, cancellationToken);
        }

        public static async Task<HttpResponseMessage> SendWithRetryAsync(
            this HttpClient httpClient,
            Func<HttpRequestMessage> requestFactory,
            IAsyncPolicy<HttpResponseMessage> retryPolicy,
            CancellationToken cancellationToken = default)
        {
            return await retryPolicy.ExecuteAsync(async () =>
            {
                using var request = requestFactory();
                return await httpClient.SendAsync(request, cancellationToken);
            });
        }

        public static async Task<HttpResponseMessage> GetWithRetryAsync(
            this HttpClient httpClient,
            string requestUri,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            return await SendWithRetryAsync(httpClient, request, cancellationToken);
        }

        public static async Task<HttpResponseMessage> PostWithRetryAsync(
            this HttpClient httpClient,
            string requestUri,
            HttpContent content,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
            return await SendWithRetryAsync(httpClient, request, cancellationToken);
        }

        public static async Task<HttpResponseMessage> PutWithRetryAsync(
            this HttpClient httpClient,
            string requestUri,
            HttpContent content,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, requestUri) { Content = content };
            return await SendWithRetryAsync(httpClient, request, cancellationToken);
        }

        public static async Task<HttpResponseMessage> DeleteWithRetryAsync(
            this HttpClient httpClient,
            string requestUri,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            return await SendWithRetryAsync(httpClient, request, cancellationToken);
        }

        public static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(
            int retryCount = 5,
            Func<int, TimeSpan> sleepDurationProvider = null)
        {
            sleepDurationProvider ??= retryAttempt =>
                TimeSpan.FromMilliseconds(Math.Pow(4, retryAttempt)) +
                TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100));

            return Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(retryCount, sleepDurationProvider);
        }

        private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
        {
            var clone = new HttpRequestMessage(original.Method, original.RequestUri)
            {
                Version = original.Version,
                VersionPolicy = original.VersionPolicy
            };

            if (original.Content != null)
            {
                switch (original.Content)
                {
                    case StringContent stringContent:
                        await CloneStringContentAsync(stringContent, clone);
                        break;
                    case ByteArrayContent byteArrayContent:
                        await CloneByteArrayContentAsync(byteArrayContent, clone);
                        break;
                    case StreamContent streamContent:
                        await CloneStreamContentAsync(streamContent, clone);
                        break;
                    case ReadOnlyMemoryContent memoryContent:
                        await CloneReadOnlyMemoryContentAsync(memoryContent, clone);
                        break;
                    default:
                        await CloneGenericContentAsync(original.Content, clone);
                        break;
                }
            }

            CopyHeaders(original.Headers, clone.Headers);

            CopyRequestOptions(original, clone);

            return clone;
        }

        private static async Task CloneStringContentAsync(StringContent original, HttpRequestMessage clone)
        {
            var content = await original.ReadAsStringAsync();
            var mediaType = original.Headers.ContentType?.MediaType ?? "text/plain";
            var encoding = original.Headers.ContentType?.CharSet ?? "utf-8";

            clone.Content = new StringContent(content, System.Text.Encoding.GetEncoding(encoding), mediaType);
        }

        private static async Task CloneByteArrayContentAsync(ByteArrayContent original, HttpRequestMessage clone)
        {
            var contentBytes = await original.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);
            CopyContentHeaders(original, clone.Content);
        }

        private static async Task CloneStreamContentAsync(StreamContent original, HttpRequestMessage clone)
        {
            var buffer = await original.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(buffer);
            CopyContentHeaders(original, clone.Content);
        }

        private static async Task CloneReadOnlyMemoryContentAsync(ReadOnlyMemoryContent original, HttpRequestMessage clone)
        {
            var buffer = await original.ReadAsByteArrayAsync();
            clone.Content = new ReadOnlyMemoryContent(buffer);
            CopyContentHeaders(original, clone.Content);
        }

        private static async Task CloneGenericContentAsync(HttpContent original, HttpRequestMessage clone)
        {
            var buffer = await original.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(buffer);
            CopyContentHeaders(original, clone.Content);
        }

        private static void CopyHeaders<T>(T sourceHeaders, T targetHeaders)
            where T : System.Net.Http.Headers.HttpHeaders
        {
            foreach (var header in sourceHeaders)
            {
                targetHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        private static void CopyContentHeaders(HttpContent source, HttpContent target)
        {
            CopyHeaders(source.Headers, target.Headers);
        }

        private static void CopyRequestOptions(HttpRequestMessage source, HttpRequestMessage target)
        {
            foreach (var option in source.Options)
            {
                target.Options.Set(new HttpRequestOptionsKey<object>(option.Key), option.Value);
            }
        }
    }
}
