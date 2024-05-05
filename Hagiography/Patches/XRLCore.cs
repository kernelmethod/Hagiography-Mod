using HarmonyLib;
using System.Linq;
using XRL;
using XRL.Core;
using XRL.UI;

namespace Kernelmethod.Hagiography {
    [HarmonyPatch(typeof(XRLCore), nameof(XRLCore.BuildScore))]
    public class XRLCoreBuildScorePatch {
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
        public static void Postfix() {
            if (!UploadEnabled())
                return;

            var option = Popup.AskString(
                $"Would you like to upload your game to Hagadias? Type '{UPLOAD_PROMPT}' to confirm.",
                MaxLength: UPLOAD_PROMPT.Length,
                WantsSpecificPrompt: UPLOAD_PROMPT
            );

            if (option.IsNullOrEmpty() || option.ToUpper() != UPLOAD_PROMPT)
                return;

            MetricsManager.LogInfo("Uploading save");
        }
    }
}
