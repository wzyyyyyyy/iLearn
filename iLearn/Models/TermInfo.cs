using System.Text.Json;

namespace iLearn.Models
{
    public class TermInfo
    {
        public string Year { get; set; }
        public string EndDate { get; set; }
        public string Num { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public string StartDate { get; set; }
        public string Selected { get; set; }

        public static List<TermInfo> Parse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var dataListElement = doc.RootElement
                .GetProperty("data")
                .GetProperty("dataList");

            return JsonSerializer.Deserialize<List<TermInfo>>(dataListElement.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<TermInfo>();
        }
    }
}
