using System;
using System.Threading.Tasks;
using UnityEngine.Networking;
using XRL;
using XRL.UI;

namespace Kernelmethod.Hagiography {
    [HasModSensitiveStaticCache]
    public static class ApiManager {
        [ModSensitiveStaticCache(true)]
        public static HagiographyConfig _Config = new HagiographyConfig();

        public static string? Token {
            get => _Config.Token;
            set {
                _Config.Token = value;
            }
        }

        [ModSensitiveCacheInit]
        public static void Initialize() {
            try {
                _Config = HagiographyConfig.ReadConfig() ?? _Config;
            }
            catch (Exception ex) {
                LogError("error reading Hagiography config: " + ex.ToString());
            }
        }

        public static string BaseRequestUri() {
            return "http://localhost:8000";
        }

        /// <summary>
        /// Show modal to ask user for their token for uploading records
        /// to Hagiography.
        /// </summary>
        public static async Task<bool> AskForToken() {
            while (true) {
                Token = await Popup.AskStringAsync(
                    "Enter your Hagiography token\n(Do NOT share this token with others!)",
                    Default: Token ?? "",
                    MaxLength: 256
                );

                if (Token.IsNullOrEmpty())
                    break;

                if (await CheckToken()) {
                    _Config.Update();
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// Check whether the user's API token is valid.
        /// </summary>
        public static async Task<bool> CheckToken() {
            var uri = $"{BaseRequestUri()}/api/auth/apikey/check";

            using (var request = UnityWebRequest.Get(uri)) {
                request.SetRequestHeader("Authorization", $"Bearer {Token}");
                var result = request.SendWebRequest();

                while (!result.isDone) {
                    await Task.Delay(50);
                }

                if (request.result == UnityWebRequest.Result.ConnectionError) {
                    await Popup.ShowAsync("Unable to connect to Hagiography. Please try again.");
                    return false;
                }

                if (request.responseCode == 400) {
                    await Popup.ShowAsync("This account has been disabled.");
                    return false;
                }

                if (request.responseCode == 401) {
                    await Popup.ShowAsync("Invalid token. Please re-enter your token.");
                    return false;
                }

                if (500 <= request.responseCode) {
                    LogError(
                        $"API token validation error: status = {request.responseCode}, content = {request.downloadHandler.text}"
                    );
                    await Popup.ShowAsync("Server error; please try again later.");
                    return false;
                }

                if (request.responseCode != 200) {
                    LogError(
                        $"API token validation error: status = {request.responseCode}, content = {request.downloadHandler.text}"
                    );
                    await Popup.ShowAsync("Unknown error; error has been logged to Player.log");
                    return false;
                }

                await Popup.ShowAsync("Token verified!");
                return true;
            }
        }

        public static void LogInfo(string Message) {
            MetricsManager.LogInfo("(HAGIOGRAPHY) " + Message);
        }

        public static void LogError(string Message) {
            MetricsManager.LogError("(HAGIOGRAPHY) " + Message);
        }
    }
}
