using Newtonsoft.Json;

namespace PdfEncryptor
{
    [JsonObject("appConfiguration")]
    public class AppConfiguration
    {
        [JsonProperty("userPassword")]
        public string UserPassword { get; set; }

        [JsonProperty("ownerPassword")]
        public string OwnerPassword { get; set; }

        [JsonProperty("outputFolder")]
        public string OutputFolder { get; set; }

        [JsonProperty("sourceFolder")]
        public string SourceFolder { get; set; }

        [JsonProperty("sourcePassword")]
        public string SourcePassword { get; set; }

        [JsonProperty("deleteSourceFile")]
        public bool DeleteSourceFile { get; set; }
    }
}