using Newtonsoft.Json;

namespace Kernelmethod.Hagiography.Schemas {
    [JsonObject(MemberSerialization.OptIn)]
    public class GameRecordCreate {
        [JsonProperty("game_mode")]
        public string GameMode;

        [JsonProperty("character_name")]
        public string CharacterName;

        [JsonProperty("tile")]
        public string Tile;

        [JsonProperty("score")]
        public long Score;

        [JsonProperty("turns")]
        public long Turns;

        [JsonProperty("build_code")]
        public string BuildCode = null;
    }
}
