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

        public static string Token {
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
            var message = "Enter your Hagiography key (do NOT share this key with others!)\n\n"
                + "To get a key, login to Hagriography in your browser and "
                + $"go to \n\n{BaseRequestUri()}/profile\n\n"
                + "In your profile, click 'Generate API key'.\n";

            var originalToken = Token;

            while (true) {
                var token = await Popup.AskStringAsync(
                    message,
                    Default: Token ?? "",
                    MaxLength: 256
                );

                if (token.IsNullOrEmpty())
                    break;

                Token = token;
                if (await CheckToken()) {
                    _Config.Update();
                    return true;
                }
            }

            // Failed to verify token. Set back to its original value.
            Token = originalToken;
            return true;
        }

        /// <summary>
        /// Check whether the user's API token is valid.
        /// </summary>
        public static async Task<bool> CheckToken() {
            if (Token.IsNullOrEmpty()) {
                var message = "You have not configured an API key.\n\n"
                    + "Go to Options > Hagiography > 'Enter key for Hagiography' "
                    + "to register an API key.";
                await Popup.ShowAsync(message);
                return false;
            }

            var uri = $"{BaseRequestUri()}/api/auth/apikeys/check";

            using (var request = UnityWebRequest.Get(uri)) {
                request.SetRequestHeader("X-Access-Token", Token);
                var result = request.SendWebRequest();

                while (!result.isDone) {
                    await Task.Delay(50);
                }

                if (request.responseCode == 200) {
                    await Popup.ShowAsync("Token verified!");
                    return true;
                }

                LogError(
                    $"API key validation error: status = {request.responseCode}, content = {request.downloadHandler.text}"
                );

                if (request.result == UnityWebRequest.Result.ConnectionError) {
                    await Popup.ShowAsync("Unable to connect to Hagiography. Please try again.");
                }
                else if (request.responseCode == 400) {
                    await Popup.ShowAsync("This account has been disabled.");
                }
                else if (request.responseCode == 401) {
                    await Popup.ShowAsync("Invalid key. Please re-enter your key.");
                }
                else if (500 <= request.responseCode) {
                    await Popup.ShowAsync("Server error; please try again later.");
                }
                else {
                    await Popup.ShowAsync("Unknown error; error has been logged to Player.log");
                }

                return false;
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
