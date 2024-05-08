using HarmonyLib;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using XRL;
using XRL.Core;
using XRL.UI;
using UnityEngine.Networking;

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

        public static string BaseRequestUri() {
            return "http://localhost:8000";
        }

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
            try {
                if (!Real || !UploadEnabled() || SCORE == null || ApiManager.Token.IsNullOrEmpty())
                    return;

                if (!Options.AutomaticUpload) {
                    var option = Popup.AskString(
                        $"Would you like to upload your game to Hagiography? Type '{UPLOAD_PROMPT}' to confirm.",
                        MaxLength: UPLOAD_PROMPT.Length,
                        WantsSpecificPrompt: UPLOAD_PROMPT
                    );

                    if (option.IsNullOrEmpty() || option.ToUpper() != UPLOAD_PROMPT)
                        return;
                }

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

                Loading.LoadTask("Uploading game record", delegate {
                    Task.Run(async delegate {
                        await UploadGameRecord(gameRecord);
                    });
                });
            }
            finally {
                SCORE = null;
            }
        }

        public static async Task<int> UploadGameRecord(GameRecordCreate record) {
            var json = JsonConvert.SerializeObject(record);
            MetricsManager.LogInfo(json);

            var uri = BaseRequestUri();
            using (var request = UnityWebRequest.Put($"{uri}/api/records/create", json)) {
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-Access-Token", ApiManager.Token);
                var result = request.SendWebRequest();
                while (!result.isDone) {
                    await Task.Delay(50);
                }

                if (request.result == UnityWebRequest.Result.ConnectionError) {
                    ApiManager.LogError(request.error);
                }
                else {
                    ApiManager.LogError(
                        $"response code: {request.responseCode}; content = {request.downloadHandler.text}"
                    );
                }
            }

            return 0;
        }
    }
}
