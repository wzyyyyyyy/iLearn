using HtmlAgilityPack;
using iLearn.Helpers;
using iLearn.Models;
using Microsoft.VisualBasic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace iLearn.Services
{
    public class ILearnApiService
    {
        private readonly HttpClient httpClient;
        private const string CAS_URL = "https://cas.jlu.edu.cn/tpass/login";
        public bool Logined { get; private set; } = false;

        public ILearnApiService()
        {
            var cookieContainer = new CookieContainer();

            var handler = new HttpClientHandler()
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10,
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(5)
            };

            httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
            httpClient.DefaultRequestHeaders.Add("Accept-Charset", "*");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62");
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            if (Logined) return true;
            var casResponse = await httpClient.GetAsync($"{CAS_URL}?service=https://jwcidentity.jlu.edu.cn/iplat-pass-jlu/thirdLogin/jlu/login");
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
            var casTicketResponse = await httpClient.PostAsync($"{CAS_URL}?service=https://jwcidentity.jlu.edu.cn/iplat-pass-jlu/thirdLogin/jlu/login", formContent);

            var htmlTicket = await casTicketResponse.Content.ReadAsStringAsync();
            var ticketDoc = new HtmlDocument();
            ticketDoc.LoadHtml(htmlTicket);

            // iLearn登录流程
            var ts0 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var ilearnGetNonceUrl0 = $"https://ilearn.jlu.edu.cn/cas-server/login?service=https://ilearntec.jlu.edu.cn/&get-lt=true&callback=jsonpcallback&n={ts0 + 1}&_={ts0}";
            var ilearnGetNonceResp0 = await httpClient.GetAsync(ilearnGetNonceUrl0);
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

            var ilearnGetNonceResp = await httpClient.GetAsync(ilearnGetNonceUrl);
            ilearnGetNonceResp.EnsureSuccessStatusCode();

            var responseText = await ilearnGetNonceResp.Content.ReadAsStringAsync();
            var jsonText = responseText.Substring(14, responseText.Length - 18); // 去掉logincallback()包装
            var ilearnCasReturn = JsonDocument.Parse(jsonText);

            // 完成登录
            var ssoUrl = $"https://ilearn.jlu.edu.cn/iplat/ssoservice?ssoservice=https://ilearntec.jlu.edu.cn/&ticket={GetStringFromJson(ilearnCasReturn, "ticket")}";
            await httpClient.GetAsync(ssoUrl);
            _ = await httpClient.GetAsync("https://ilearntec.jlu.edu.cn/coursecenter/main/index");

            Logined = true;

            return true;
        }

        public async Task<List<TermInfo>> GetTermsAsync()
        {
            if (!Logined) throw new InvalidOperationException("Not logged in.");
            var response = await httpClient.PostAsync("https://ilearntec.jlu.edu.cn/studycenter/platform/common/termList", new StringContent(""));
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return TermInfo.Parse(json);
        }

        public async Task<List<ClassInfo>> GetClassesAsync(string year,string term)
        {
            if (!Logined) throw new InvalidOperationException("Not logged in.");
            var response = await httpClient.GetAsync($"https://ilearntec.jlu.edu.cn/studycenter/platform/classroom/myClassroom?termYear={year}&term={term}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return ClassInfo.Parse(json);
        }

        public async Task<List<LiveAndRecordInfo>> GetLiveAndRecordInfoAsync(string termId, string classId)
        {
            if (!Logined) throw new InvalidOperationException("Not logged in.");
            var response = await httpClient.GetAsync($"https://ilearntec.jlu.edu.cn/coursecenter/liveAndRecord/getLiveAndRecordInfoList?memberId=&termId={termId}&roomType=0&identity=2&liveStatus=0&submitStatus=0&weekNum=&dayNum=&timeRange=&teachClassId={classId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return LiveAndRecordInfo.Parse(json);
        }

        private string GetStringFromJson(JsonDocument doc, string propertyName)
        {
            return doc.RootElement.GetProperty(propertyName).GetString() ?? string.Empty;
        }
    }
}
