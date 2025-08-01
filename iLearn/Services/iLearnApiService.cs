﻿using HtmlAgilityPack;
using iLearn.Helpers;
using iLearn.Helpers.Extensions;
using iLearn.Models;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace iLearn.Services
{
    public class ILearnApiService
    {
        private HttpClient httpClient;
        private const string CAS_URL = "https://cas.jlu.edu.cn/tpass/login";
        private CookieContainer CookieContainer;
        public bool Logined { get; private set; } = false;

        public void Init()
        {
            CookieContainer = new CookieContainer();

            var handler = new HttpClientHandler()
            {
                CookieContainer = CookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 50,
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
            httpClient.DefaultRequestHeaders.Add("Accept-Charset", "*");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            if (Logined) return true;

            Init();

            var casResponse = await httpClient.GetWithRetryAsync($"{CAS_URL}?service=https://jwcidentity.jlu.edu.cn/iplat-pass-jlu/thirdLogin/jlu/login");
            casResponse.EnsureSuccessStatusCode();
            var casHtml = await casResponse.Content.ReadAsStringAsync();
            var casDoc = new HtmlDocument();

            casDoc.LoadHtml(casHtml);

            var casEvent = casDoc.DocumentNode.SelectSingleNode("//input[@name='_eventId']")?.GetAttributeValue("value", null);
            var casExecution = casDoc.DocumentNode.SelectSingleNode("//input[@name='execution']")?.GetAttributeValue("value", null);
            var casNonce = casDoc.DocumentNode.SelectSingleNode("//input[@name='lt']")?.GetAttributeValue("value", null);

            if (casNonce == null || casEvent == null || casExecution == null)
                return false;

            // 构建表单数据
            var formParams = new List<KeyValuePair<string, string>>
                {
                    new("rsa", DesEncryption.StrEnc(username + password + casNonce, "1", "2", "3")),
                    new("ul", username.Length.ToString()),
                    new("pl", password.Length.ToString()),
                    new("sl", "0"),
                    new("lt", casNonce),
                    new("execution", casExecution),
                    new("_eventId", casEvent)
                };

            var formContent = new FormUrlEncodedContent(formParams);
            var casTicketResponse = await httpClient.PostWithRetryAsync($"{CAS_URL}?service=https://jwcidentity.jlu.edu.cn/iplat-pass-jlu/thirdLogin/jlu/login", formContent);

            var htmlTicket = await casTicketResponse.Content.ReadAsStringAsync();
            var ticketDoc = new HtmlDocument();
            ticketDoc.LoadHtml(htmlTicket);

            // iLearn登录流程
            var ts0 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var ilearnGetNonceUrl0 = $"https://ilearn.jlu.edu.cn/cas-server/login?service=https://ilearntec.jlu.edu.cn/&get-lt=true&callback=jsonpcallback&n={ts0 + 1}&_={ts0}";
            var ilearnGetNonceResp0 = await httpClient.GetWithRetryAsync(ilearnGetNonceUrl0);
            ilearnGetNonceResp0.EnsureSuccessStatusCode();

            var responseText0 = await ilearnGetNonceResp0.Content.ReadAsStringAsync();
            var jsonText0 = responseText0.Substring(14, responseText0.Length - 16); // 去掉jsonpcallback()包装
            var ilearnCasReturn0 = JsonDocument.Parse(jsonText0);

            var casUsername = ticketDoc.DocumentNode.SelectSingleNode("//input[@id='username']")?.GetAttributeValue("value", null);
            var casPassword = ticketDoc.DocumentNode.SelectSingleNode("//input[@id='password']")?.GetAttributeValue("value", null);

            if (String.IsNullOrEmpty(casUsername) || String.IsNullOrEmpty(casPassword))
            {
                return false;
            }

            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var passwordBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(casPassword));

            var ilearnGetNonceUrl = $"https://ilearn.jlu.edu.cn/cas-server/login?" +
                $"service=https://ilearntec.jlu.edu.cn/&" +
                $"username={casUsername}&" +
                $"password={passwordBase64}&" +
                $"callback=logincallback&" +
                $"lt={GetStringFromJson(ilearnCasReturn0, "lt")}&" +
                $"execution={GetStringFromJson(ilearnCasReturn0, "execution")}&" +
                $"n={ts + 1}&" +
                $"isajax=true&" +
                $"isframe=true&" +
                $"_eventId=submit&" +
                $"_={ts}";

            var ilearnGetNonceResp = await httpClient.GetWithRetryAsync(ilearnGetNonceUrl);
            ilearnGetNonceResp.EnsureSuccessStatusCode();

            var responseText = await ilearnGetNonceResp.Content.ReadAsStringAsync();
            var jsonText = responseText.Substring(14, responseText.Length - 18); // 去掉logincallback()包装
            var ilearnCasReturn = JsonDocument.Parse(jsonText);

            // 完成登录
            var ssoUrl = $"https://ilearn.jlu.edu.cn/iplat/ssoservice?ssoservice=https://ilearntec.jlu.edu.cn/&ticket={GetStringFromJson(ilearnCasReturn, "ticket")}";
            await httpClient.GetWithRetryAsync(ssoUrl);
            _ = await httpClient.GetWithRetryAsync("https://ilearntec.jlu.edu.cn/coursecenter/main/index");

            Logined = true;

            return true;
        }

        public async Task<List<TermInfo>> GetTermsAsync()
        {
            if (!Logined) throw new InvalidOperationException("Not logged in.");
            var response = await httpClient.PostWithRetryAsync("https://ilearntec.jlu.edu.cn/studycenter/platform/common/termList", new StringContent(""));
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return TermInfo.Parse(json);
        }

        public async Task<List<ClassInfo>> GetClassesAsync(string year, string term)
        {
            if (!Logined) throw new InvalidOperationException("Not logged in.");
            var response = await httpClient.GetWithRetryAsync($"https://ilearntec.jlu.edu.cn/studycenter/platform/classroom/myClassroom?termYear={year}&term={term}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return ClassInfo.Parse(json);
        }

        public async Task<List<LiveAndRecordInfo>> GetLiveAndRecordInfoAsync(string termId, string classId)
        {
            if (!Logined) throw new InvalidOperationException("Not logged in.");
            var response = await httpClient.GetWithRetryAsync($"https://ilearntec.jlu.edu.cn/coursecenter/liveAndRecord/getLiveAndRecordInfoList?memberId=&termId={termId}&roomType=0&identity=2&liveStatus=0&submitStatus=0&weekNum=&dayNum=&timeRange=&teachClassId={classId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return LiveAndRecordInfo.Parse(json);
        }

        public async Task<VideoInfo> GetVideoInfoAsync(string resourceId)
        {
            if (!Logined)
                throw new InvalidOperationException("Not logged in.");

            await httpClient.GetWithRetryAsync($"https://ilearnres.jlu.edu.cn/resource-center/zhwk/selectLanguageExists?resourceId={resourceId}");

            var response = await httpClient.GetWithRetryAsync($"https://ilearnres.jlu.edu.cn/resource-center/videoclass/videoClassInfo?resourceId={resourceId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return VideoInfo.Parse(json);
        }

        public async Task<string> JoinCourse(string courseId)
        {
            if (!Logined) throw new InvalidOperationException("Not logged in.");
            var response = await httpClient.GetWithRetryAsync($"https://ilearntec.jlu.edu.cn/studycenter/platform/classroom/joinClassroom?classroomCode={courseId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var dataListElement = doc.RootElement
                .GetProperty("message");
            return dataListElement.GetString() ?? string.Empty;
        }

        public async Task<UserInfo> GetUserInfo()
        {
            if (!Logined) throw new InvalidOperationException("Not logged in.");
            var response = await httpClient.PostWithRetryAsync($"https://ilearntec.jlu.edu.cn/studycenter/platform/public/getUserInfo", new StringContent(""));
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return UserInfo.Parse(json);
        }

        private string GetStringFromJson(JsonDocument doc, string propertyName)
        {
            return doc.RootElement.GetProperty(propertyName).GetString() ?? string.Empty;
        }
    }
}
