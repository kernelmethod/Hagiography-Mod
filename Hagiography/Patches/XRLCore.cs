using HarmonyLib;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
// using System.Text.Json;
using XRL;
using XRL.Core;
using XRL.UI;

using Kernelmethod.Hagiography.Schemas;

namespace Kernelmethod.Hagiography {
    [HarmonyPatch(typeof(GameSummaryUI), nameof(GameSummaryUI.Show))]
    public static class ExtractScorePatch {
        /// <summary>
        /// Small Harmony patch over GameSummaryUI.Show to extract the score passed in as an
        /// argument to the function (since it isn't available anywhere else).
        ///
        /// This could also be implemented as a transpiler patch, this method is just very
        /// mildly simpler.
        /// </summary>
        public static void Postfix(int Score) {
            XRLCoreBuildScorePatch.SCORE = Score;
        }
    }

    [HarmonyPatch(typeof(XRLCore), nameof(XRLCore.BuildScore))]
    public static class XRLCoreBuildScorePatch {
        public static long? SCORE = null;
        public const string UPLOAD_PROMPT = "UPLOAD";
        public static string[] DEFAULT_ENABLED_GAME_MODES = {
            "Classic", "Roleplay", "Wander", "Daily"
        };

        public static bool UploadEnabled() {
            return The.Game.GetStringGameState("Kernelmethod_Hagiography") == "Enabled"
                || DEFAULT_ENABLED_GAME_MODES.Contains(The.Game.GetStringGameState("GameMode"));
        }

        /// <summary>
        /// Postfix patch for XRLCore.BuildScore.
        ///
        /// This presents a modal that allows the player to upload their results to
        /// Hagiography, if they so choose.
        /// </summary>
        public static void Postfix(bool Real) {
            if (!Real || !UploadEnabled() || SCORE == null)
                return;

            var option = Popup.AskString(
                $"Would you like to upload your game to Hagiography? Type '{UPLOAD_PROMPT}' to confirm.",
                MaxLength: UPLOAD_PROMPT.Length,
                WantsSpecificPrompt: UPLOAD_PROMPT
            );

            if (option.IsNullOrEmpty() || option.ToUpper() != UPLOAD_PROMPT)
                return;

            MetricsManager.LogInfo("Uploading save");
            var render = The.Player.Render;
            var tile = new Tile {
                Path = render.Tile,
                RenderString = render.RenderString,
                ColorString = render.ColorString,
                DetailColor = render.DetailColor,
                TileColor = render.TileColor,
                HFlip = render.HFlip,
                VFlip = render.VFlip
            };

            var gameRecord = new GameRecordCreate {
                GameMode = The.Game.GetStringGameState("GameMode"),
                CharacterName = The.Game.PlayerName,
                Tile = tile.ToString(),
                Score = SCORE ?? 0,
                Turns = The.Game.Turns,
            };

            var json = JsonConvert.SerializeObject(gameRecord);
            MetricsManager.LogInfo(json);

            SCORE = null;
        }
    }
}
