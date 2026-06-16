using HtmlAgilityPack;
using iLearn.Helpers;
using iLearn.Helpers.Extensions;
using iLearn.Models;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace iLearn.Services
{
    public enum LoginStepResult
    {
        Success,
        NeedWechatCode,
        WrongPassword,
        Failed
    }

    public class ILearnApiService
    {
        private HttpClient _httpClient = null!;
        private HttpClient _noRedirectClient = null!;
        private CookieContainer _cookieContainer = null!;

        public bool Logined { get; private set; } = false;

        private string? _pendingLt;
        private string? _pendingExecution;
        private string? _pendingEventId;
        private string? _pendingUsername;
        private string? _pendingPassword;

        private const string CAS_BASE = "https://cas.jlu.edu.cn/tpass";
        private const string CAS_SERVICE = "https://jwcidentity.jlu.edu.cn/iplat-pass-jlu/thirdLogin/jlu/login";
        private const string ILEARN_CAS = "https://ilearn.jlu.edu.cn/cas-server";
        private const string ILEARN_IPLAT = "https://ilearn.jlu.edu.cn/iplat";
        private const string ILEARNTEC = "https://ilearntec.jlu.edu.cn";

        private void Init()
        {
            _cookieContainer = new CookieContainer();

            var handler = new HttpClientHandler()
            {
                CookieContainer = _cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _httpClient = CreateClient(handler);

            var noRedirectHandler = new HttpClientHandler()
            {
                CookieContainer = _cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = false,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _noRedirectClient = CreateClient(noRedirectHandler);
        }

        private static HttpClient CreateClient(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Accept-Charset", "*");
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            return client;
        }

        // ── Step 1: 提交账号密码 ──

        public virtual async Task<LoginStepResult> LoginStep1Async(string username, string password)
        {
            if (Logined) return LoginStepResult.Success;

            Init();

            var casLoginUrl = $"{CAS_BASE}/login?service={Uri.EscapeDataString(CAS_SERVICE)}";
            var casPageResp = await _httpClient.GetWithRetryAsync(casLoginUrl);
            casPageResp.EnsureSuccessStatusCode();

            var casDoc = new HtmlDocument();
            casDoc.LoadHtml(await casPageResp.Content.ReadAsStringAsync());

            var lt = GetAttributeValueOrNull(casDoc.DocumentNode.SelectSingleNode("//input[@name='lt']"), "value");
            var execution = GetAttributeValueOrNull(casDoc.DocumentNode.SelectSingleNode("//input[@name='execution']"), "value");

            if (string.IsNullOrEmpty(lt) || string.IsNullOrEmpty(execution))
                return LoginStepResult.Failed;

            var rsa = DesEncryption.StrEnc(username + password + lt, "1", "2", "3");
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("rsa", rsa),
                new KeyValuePair<string, string>("ul", username.Length.ToString()),
                new KeyValuePair<string, string>("pl", password.Length.ToString()),
                new KeyValuePair<string, string>("sl", "0"),
                new KeyValuePair<string, string>("lt", lt),
                new KeyValuePair<string, string>("execution", execution),
                new KeyValuePair<string, string>("_eventId", "submit")
            });

            var loginResp = await _noRedirectClient.PostAsync(
                $"{CAS_BASE}/login?service={Uri.EscapeDataString(CAS_SERVICE)}", form);

            if (loginResp.StatusCode == HttpStatusCode.Found)
            {
                var redirectUrl = loginResp.Headers.Location?.ToString();
                if (string.IsNullOrEmpty(redirectUrl))
                    return LoginStepResult.Failed;

                return await FollowJwcAndCompleteLoginAsync(redirectUrl)
                    ? LoginStepResult.Success
                    : LoginStepResult.Failed;
            }

            var responseHtml = await loginResp.Content.ReadAsStringAsync();

            var loginResult = LoginResponseClassifier.Classify(responseHtml);
            if (loginResult != LoginStepResult.NeedWechatCode)
                return loginResult;

            var recheckDoc = new HtmlDocument();
            recheckDoc.LoadHtml(responseHtml);

            _pendingUsername = username;
            _pendingPassword = password;
            _pendingLt = GetAttributeValueOrNull(recheckDoc.DocumentNode.SelectSingleNode("//input[@name='lt']"), "value") ?? lt;
            _pendingExecution = GetAttributeValueOrNull(recheckDoc.DocumentNode.SelectSingleNode("//input[@name='execution']"), "value") ?? "e1s2";
            _pendingEventId = GetAttributeValueOrNull(recheckDoc.DocumentNode.SelectSingleNode("//input[@name='_eventId']"), "value") ?? "submit";

            return LoginStepResult.NeedWechatCode;
        }

        // ── 获取图形验证码 ──

        public virtual async Task<byte[]> GetCasCaptchaBytesAsync()
        {
            var resp = await _httpClient.GetAsync(
                $"{CAS_BASE}/code?{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsByteArrayAsync();
        }

        // ── 请求微信验证码 ──

        public enum WechatCodeRequestResult
        {
            Success,
            CaptchaError,
            TooFrequent,
            SessionExpired,
            Failed
        }

        public virtual async Task<WechatCodeRequestResult> RequestWechatCodeAsync(string imageCaptcha)
        {
            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _cookieContainer.Add(
                new Uri($"{CAS_BASE}/"),
                new Cookie("recheck_djsendtime", (time + 120000).ToString()));

            var req = new HttpRequestMessage(HttpMethod.Get,
                $"{CAS_BASE}/recheckcode?code={Uri.EscapeDataString(imageCaptcha)}&t={time}");
            req.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
            req.Headers.TryAddWithoutValidation("Referer", $"{CAS_BASE}/login?service={Uri.EscapeDataString(CAS_SERVICE)}");

            var resp = await _httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();

            var resultCookie = _cookieContainer.GetCookies(new Uri($"{CAS_BASE}/recheckcode"))["recheck_mobile_error_info"];
            if (resultCookie == null)
                return WechatCodeRequestResult.Failed;

            return resultCookie.Value switch
            {
                "success" => WechatCodeRequestResult.Success,
                "img_code_error" => WechatCodeRequestResult.CaptchaError,
                "get_times_more" => WechatCodeRequestResult.TooFrequent,
                "get_session_error" => WechatCodeRequestResult.SessionExpired,
                _ => WechatCodeRequestResult.Failed
            };
        }

        // ── Step 2: 提交验证码完成二次认证 ──

        public virtual async Task<LoginStepResult> LoginStep2Async(string imageCaptcha, string wechatCode)
        {
            if (_pendingLt == null || _pendingUsername == null || _pendingPassword == null)
                return LoginStepResult.Failed;

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("rsa", ""),
                new KeyValuePair<string, string>("ul", ""),
                new KeyValuePair<string, string>("pl", ""),
                new KeyValuePair<string, string>("sl", ""),
                new KeyValuePair<string, string>("lt", _pendingLt),
                new KeyValuePair<string, string>("execution", _pendingExecution ?? "e1s2"),
                new KeyValuePair<string, string>("_eventId", _pendingEventId ?? "submit"),
                new KeyValuePair<string, string>("code", imageCaptcha),
                new KeyValuePair<string, string>("WxCode", wechatCode),
                new KeyValuePair<string, string>("not_exit_number", ""),
                new KeyValuePair<string, string>("service_id", "")
            });

            var loginResp = await _noRedirectClient.PostAsync(
                $"{CAS_BASE}/login?service={Uri.EscapeDataString(CAS_SERVICE)}", form);
            if (loginResp.StatusCode != HttpStatusCode.Found)
            {
                var html = await loginResp.Content.ReadAsStringAsync();
                return html.Contains("验证码错误") ? LoginStepResult.WrongPassword : LoginStepResult.Failed;
            }

            var redirectUrl = loginResp.Headers.Location?.ToString();
            if (string.IsNullOrEmpty(redirectUrl))
                return LoginStepResult.Failed;

            if (!await FollowJwcAndCompleteLoginAsync(redirectUrl))
                return LoginStepResult.Failed;

            ClearPendingState();
            return LoginStepResult.Success;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var result = await LoginStep1Async(username, password);
            return result == LoginStepResult.Success;
        }

        // ── iLearn 登录 (CAS ticket → iLearn session) ──

        private async Task<bool> FollowJwcAndCompleteLoginAsync(string redirectUrl)
        {
            var jwcResp = await _httpClient.GetWithRetryAsync(redirectUrl);
            jwcResp.EnsureSuccessStatusCode();

            var jwcDoc = new HtmlDocument();
            jwcDoc.LoadHtml(await jwcResp.Content.ReadAsStringAsync());

            var casUsername = jwcDoc.DocumentNode.SelectSingleNode("//input[@id='username']")?.GetAttributeValue("value", string.Empty);
            var casPassword = jwcDoc.DocumentNode.SelectSingleNode("//input[@id='password']")?.GetAttributeValue("value", string.Empty);

            if (string.IsNullOrEmpty(casUsername) || string.IsNullOrEmpty(casPassword))
                return false;

            return await CompleteILearnLoginAsync(casUsername, casPassword);
        }

        private async Task<bool> CompleteILearnLoginAsync(string username, string password)
        {
            var ts0 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var getNonceUrl = $"{ILEARN_CAS}/login?service={Uri.EscapeDataString($"{ILEARNTEC}/courselibrary-web/index?isLocation=1")}&get-lt=true&callback=jsonpcallback&n={ts0 + 1}&_={ts0}";

            var nonceResp = await _httpClient.GetWithRetryAsync(getNonceUrl);
            nonceResp.EnsureSuccessStatusCode();

            var nonceText = await nonceResp.Content.ReadAsStringAsync();
            var jsonText = nonceText.Substring(14, nonceText.Length - 16);
            using var nonceDoc = JsonDocument.Parse(jsonText);

            var lt = nonceDoc.RootElement.GetProperty("lt").GetString() ?? string.Empty;
            var execution = nonceDoc.RootElement.GetProperty("execution").GetString() ?? string.Empty;

            var passwordBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var loginUrl = $"{ILEARN_CAS}/login?" +
                $"service={Uri.EscapeDataString($"{ILEARNTEC}/courselibrary-web/index?isLocation=1")}&" +
                $"username={Uri.EscapeDataString(username)}&" +
                $"password={Uri.EscapeDataString(passwordBase64)}&" +
                $"isajax=true&isframe=true&_eventId=submit&" +
                $"lt={Uri.EscapeDataString(lt)}&" +
                $"execution={Uri.EscapeDataString(execution)}&" +
                $"type=pwd&callback=logincallback&n={ts + 1}&_={ts}";

            var loginResp = await _httpClient.GetWithRetryAsync(loginUrl);
            loginResp.EnsureSuccessStatusCode();

            var loginText = await loginResp.Content.ReadAsStringAsync();
            var loginJson = loginText.Substring(14, loginText.Length - 18);
            using var loginDoc = JsonDocument.Parse(loginJson);

            if (loginDoc.RootElement.TryGetProperty("login", out var loginStatus) &&
                loginStatus.GetString() == "fails")
                return false;

            // 用服务票据完成 ilearntec 的 session 建立
            if (loginDoc.RootElement.TryGetProperty("service", out var serviceEl))
            {
                var serviceUrl = serviceEl.GetString();
                if (!string.IsNullOrEmpty(serviceUrl))
                    _ = await _httpClient.GetWithRetryAsync(serviceUrl);
            }

            _ = await _httpClient.GetWithRetryAsync($"{ILEARN_IPLAT}/ssoservice");
            _ = await _httpClient.GetWithRetryAsync($"{ILEARNTEC}/courselibrary-web/index?isLocation=1");

            // 为 studycenter 子系统单独申请服务票据（TGT 已建立，自动跟随重定向完成 ticket 验证）
            _ = await _httpClient.GetWithRetryAsync(
                $"{ILEARN_CAS}/login?service={Uri.EscapeDataString($"{ILEARNTEC}/studycenter/platform/main/index")}");

            Logined = true;
            return true;
        }

        // ── helpers ──

        private void ClearPendingState()
        {
            _pendingLt = null;
            _pendingExecution = null;
            _pendingEventId = null;
            _pendingUsername = null;
            _pendingPassword = null;
        }

        private static string? GetAttributeValueOrNull(HtmlNode? node, string attributeName)
        {
            return node?.Attributes[attributeName]?.Value;
        }

        // ── API methods ──

        public virtual async Task<List<TermInfo>> GetTermsAsync()
        {
            if (!Logined) throw new InvalidOperationException("Not logged in.");
            var response = await _httpClient.PostWithRetryAsync(
                "https://ilearntec.jlu.edu.cn/studycenter/platform/common/termList",
                new StringContent(""));
            response.EnsureSuccessStatusCode();
            return TermInfo.Parse(await response.Content.ReadAsStringAsync());
        }

        public virtual async Task<List<ClassInfo>> GetClassesAsync(string year, string term)
        {
            if (!Logined) throw new InvalidOperationException("Not logged in.");
            var response = await _httpClient.GetWithRetryAsync(
                $"https://ilearntec.jlu.edu.cn/studycenter/platform/classroom/myClassroom?termYear={year}&term={term}");
            response.EnsureSuccessStatusCode();
            return ClassInfo.Parse(await response.Content.ReadAsStringAsync());
        }

        public virtual async Task<List<LiveAndRecordInfo>> GetLiveAndRecordInfoAsync(string termId, string classId)
        {
            if (!Logined) throw new InvalidOperationException("Not logged in.");
            var response = await _httpClient.GetWithRetryAsync(
                $"https://ilearntec.jlu.edu.cn/coursecenter/liveAndRecord/getLiveAndRecordInfoList?" +
                $"memberId=&termId={termId}&roomType=0&identity=2&liveStatus=0&submitStatus=0" +
                $"&weekNum=&dayNum=&timeRange=&teachClassId={classId}");
            response.EnsureSuccessStatusCode();
            return LiveAndRecordInfo.Parse(await response.Content.ReadAsStringAsync());
        }

        public virtual async Task<VideoInfo> GetVideoInfoAsync(string resourceId)
        {
            if (!Logined) throw new InvalidOperationException("Not logged in.");
            await _httpClient.GetWithRetryAsync(
                $"https://ilearnres.jlu.edu.cn/resource-center/zhwk/selectLanguageExists?resourceId={resourceId}");
            var response = await _httpClient.GetWithRetryAsync(
                $"https://ilearnres.jlu.edu.cn/resource-center/videoclass/videoClassInfo?resourceId={resourceId}");
            response.EnsureSuccessStatusCode();
            return VideoInfo.Parse(await response.Content.ReadAsStringAsync());
        }

        public async Task<string> JoinCourse(string courseId)
        {
            if (!Logined) throw new InvalidOperationException("Not logged in.");
            var response = await _httpClient.GetWithRetryAsync(
                $"https://ilearntec.jlu.edu.cn/studycenter/platform/classroom/joinClassroom?classroomCode={courseId}");
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("message").GetString() ?? string.Empty;
        }

        public async Task<UserInfo> GetUserInfo()
        {
            if (!Logined) throw new InvalidOperationException("Not logged in.");
            var response = await _httpClient.PostWithRetryAsync(
                $"https://ilearntec.jlu.edu.cn/studycenter/platform/public/getUserInfo",
                new StringContent(""));
            response.EnsureSuccessStatusCode();
            return UserInfo.Parse(await response.Content.ReadAsStringAsync());
        }
    }
}
