using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        /// Small Harmony patch over GameSummaryUI.Show to extract some data that
        /// then gets passed in to the main patch.
        /// </summary>
        public static void Postfix(int Score) {
            XRLCoreBuildScorePatch.SCORE = Score;
            XRLCoreBuildScorePatch.ACCOMPLISHMENTS = Qud.API.JournalAPI.Accomplishments;
        }
    }

    [HarmonyPatch(typeof(XRLCore), nameof(XRLCore.BuildScore))]
    public static class XRLCoreBuildScorePatch {
        public static long? SCORE = null;
        public static List<Qud.API.JournalAccomplishment> ACCOMPLISHMENTS = new List<Qud.API.JournalAccomplishment>();

        public const string UPLOAD_PROMPT = "UPLOAD";
        public static string[] DEFAULT_ENABLED_GAME_MODES = {
            "Classic", "Roleplay", "Wander", "Daily"
        };

        public static string BaseRequestUri() {
            return ApiManager.BaseRequestUri();
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

                Loading.LoadTask("Uploading game record", delegate {
                    var task = Task.Run(async delegate {
                        await PerformUpload();
                    });

                    while (!task.IsCompleted)
                        Task.Delay(100);
                });
            }
            finally {
                SCORE = null;
                ACCOMPLISHMENTS.Clear();
            }
        }

        public static async Task<int> PerformUpload() {
            try {
                ApiManager.LogInfo("Uploading save");
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

                var recordId = await UploadGameRecord(gameRecord);
                ApiManager.LogInfo($"upload complete; record id = {recordId}");

                if (recordId.IsNullOrEmpty()) {
                    await Popup.ShowAsync("There was an error uploading your game to Hagiography.");
                    return 0;
                }

                await UploadJournalEntries(recordId);

                Loading.SetLoadingStatus("Done!");
                await Popup.ShowAsync("Your game was uploaded to Hagiography!");
            }
            finally {
                Loading.SetLoadingStatus(null);
            }

            return 0;
        }

        public static async Task<string> UploadGameRecord(GameRecordCreate record) {
            var json = JsonConvert.SerializeObject(record);
            MetricsManager.LogInfo(json);

            var uri = BaseRequestUri();
            using (var request = UnityWebRequest.Put($"{uri}/api/records/create", json)) {
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-Access-Token", ApiManager.Token);
                var task = request.SendWebRequest();
                while (!task.isDone) {
                    await Task.Delay(50);
                }

                if (request.result == UnityWebRequest.Result.ConnectionError) {
                    ApiManager.LogError(request.error);
                }
                else {
                    ApiManager.LogInfo(
                        $"response code: {request.responseCode}; content = {request.downloadHandler.text}"
                    );

                    try {
                        var uploadResult = JsonConvert.DeserializeObject<GameRecordUploadResult>(request.downloadHandler.text);
                        return uploadResult.RecordId;
                    }
                    catch {
                        ApiManager.LogError("unable to decode server response as JSON");
                    }
                }
            }

            return null;
        }

        public static async Task<int> UploadJournalEntries(string recordId) {
            // Serialize journal entries
            ApiManager.LogInfo($"num accomplishments = {ACCOMPLISHMENTS.Count}");
            var entries = new List<JournalAccomplishment>();

            foreach (var entry in Qud.API.JournalAPI.Accomplishments) {
                entries.Add(
                    new JournalAccomplishment {
                        Time = entry.Time,
                        Text = entry.Text,
                        Snapshot = JournalAccomplishment.SerializeSnapshot(entry.Screenshot)
                    }
                );
            }

            var create = new JournalAccomplishmentsCreate {
                GameRecordId = recordId,
                Accomplishments = entries,
            };
            var json = JsonConvert.SerializeObject(create);

            ApiManager.LogInfo($"uploading {create.Accomplishments.Count} accomplishments");
            ApiManager.LogInfo($"json = {json}");

            var uri = BaseRequestUri();
            using (var request = UnityWebRequest.Put($"{uri}/api/records/journal/create", json)) {
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-Access-Token", ApiManager.Token);
                var task = request.SendWebRequest();
                while (!task.isDone)
                    await Task.Delay(50);

                if (request.result == UnityWebRequest.Result.ConnectionError) {
                    ApiManager.LogError(request.error);
                }
                else {
                    ApiManager.LogInfo(
                        $"response code: {request.responseCode}; content = {request.downloadHandler.text}"
                    );
                }
            }

            return 0;
        }
    }
}
