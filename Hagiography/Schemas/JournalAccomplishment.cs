using ConsoleLib.Console;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Kernelmethod.Hagiography.Schemas {
    [JsonObject(MemberSerialization.OptIn)]
    public class JournalAccomplishment {
        [JsonProperty("time")]
        public long Time { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("snapshot")]
        public string Snapshot { get; set; }

        public static string SerializeSnapshot(SnapshotRenderable[] Snapshot) {
            var builder = new StringBuilder(1024);

            for (int i = 0; i < Snapshot.Length; ++i) {
                var rend = Snapshot[i];
                var tile = new Tile {
                    Path = rend.Tile,
                    RenderString = rend.RenderString,
                    ColorString = rend.ColorString,
                    DetailColor = rend.DetailColor.ToString(),
                    TileColor = rend.TileColor,
                    HFlip = rend.HFlip,
                    VFlip = rend.VFlip
                };

                builder.Append(tile.ToString());

                if (i != Snapshot.Length - 1)
                    builder.Append("|");
            }

            return builder.ToString();
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class JournalAccomplishmentsCreate {
        [JsonProperty("game_record_id")]
        public string GameRecordId { get; set; }

        [JsonProperty("accomplishments")]
        public List<JournalAccomplishment> Accomplishments { get; set; }
    }
}
