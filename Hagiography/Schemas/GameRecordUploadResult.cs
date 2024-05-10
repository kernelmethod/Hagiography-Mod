using Newtonsoft.Json;

namespace Kernelmethod.Hagiography.Schemas {
    /// <summary>
    /// The result returned by the server after uploading a game
    /// record to it.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class GameRecordUploadResult {
        [JsonProperty("detail")]
        public string Detail;

        [JsonProperty("id")]
        public string RecordId;
    }
}
