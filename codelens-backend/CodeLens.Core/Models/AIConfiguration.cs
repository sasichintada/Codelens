namespace CodeLens.Core.Models
{
    public class AIConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "google/gemma-3-12b-it:free";
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
        public int MaxTokens { get; set; } = 4000;
        public double Temperature { get; set; } = 0.3;
        public int TimeoutSeconds { get; set; } = 60;
        public bool EnableCaching { get; set; } = true;
    }
}