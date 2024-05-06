using Newtonsoft.Json;
using System.IO;
using XRL;

namespace Kernelmethod.Hagiography {
    [JsonObject(MemberSerialization.OptIn)]
    public class HagiographyConfig {
        [JsonProperty("Token")]
        public string? Token = null;

        public static string ConfigPath() =>
            DataManager.SyncedPath("Kernelmethod_Hagiography.json");

        public static HagiographyConfig? ReadConfig() {
            var path = ConfigPath();
            if (!File.Exists(path))
                return null;

            return (new JsonSerializer())
                .Deserialize<HagiographyConfig>(path);
        }

        public void Update() {
            (new JsonSerializer())
                .Serialize(ConfigPath(), this);
        }
    }
}
