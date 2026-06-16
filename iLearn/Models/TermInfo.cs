using System.Text.Json;

namespace iLearn.Models
{
    public class TermInfo
    {
        public string Year { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string Num { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string Selected { get; set; } = string.Empty;

        public override string ToString()
        {
            return int.TryParse(Year, out var startYear)
                ? $"{Year}-{startYear + 1}学年{Name}"
                : Name;
        }

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
