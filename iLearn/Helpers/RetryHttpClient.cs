﻿using Polly;
using System.Net;
using System.Net.Http;

namespace iLearn.Helpers
{
    public class RetryHttpClient : HttpClient
    {
        public RetryHttpClient(HttpMessageHandler handler) : base(new RetryHandler(handler))
        {
        }

        public RetryHttpClient() : base(new RetryHandler())
        {
        }
    }

    public class RetryHandler(HttpMessageHandler innerHandler = null) : DelegatingHandler(innerHandler ?? new HttpClientHandler())
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy = Policy
            .Handle<HttpRequestException>() 
            .Or<TaskCanceledException>() 
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode) 
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds(Math.Pow(4, retryAttempt)) +
                    TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)));


        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var clonedRequest = await CloneRequestAsync(request);
                return await base.SendAsync(clonedRequest, cancellationToken);
            });
        }

        private async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
        {
            var clone = new HttpRequestMessage(original.Method, original.RequestUri)
            {
                Version = original.Version
            };

            // 复制内容
            if (original.Content != null)
            {
                var content = await original.Content.ReadAsByteArrayAsync();
                clone.Content = new ByteArrayContent(content);

                foreach (var header in original.Content.Headers)
                {
                    clone.Content.Headers.Add(header.Key, header.Value);
                }
            }

            foreach (var header in original.Headers)
            {
                clone.Headers.Add(header.Key, header.Value);
            }

            return clone;
        }
    }
}