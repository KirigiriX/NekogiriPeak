using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace NekogiriMod
{
    [BepInPlugin("kirigiri.peak.nekogiri", "NekogiriPeak", "1.0.0.0")]
    public class NekogiriMod : BaseUnityPlugin
    {
        private void Awake()
        {
            // Set up plugin logging
            Logger.LogInfo(@"
 ██ ▄█▀ ██▓ ██▀███   ██▓  ▄████  ██▓ ██▀███   ██▓
 ██▄█▒ ▓██▒▓██ ▒ ██▒▓██▒ ██▒ ▀█▒▓██▒▓██ ▒ ██▒▓██▒
▓███▄░ ▒██▒▓██ ░▄█ ▒▒██▒▒██░▄▄▄░▒██▒▓██ ░▄█ ▒▒██▒
▓██ █▄ ░██░▒██▀▀█▄  ░██░░▓█  ██▓░██░▒██▀▀█▄  ░██░
▒██▒ █▄░██░░██▓ ▒██▒░██░░▒▓███▀▒░██░░██▓ ▒██▒░██░
▒ ▒▒ ▓▒░▓  ░ ▒▓ ░▒▓░░▓   ░▒   ▒ ░▓  ░ ▒▓ ░▒▓░░▓  
░ ░▒ ▒░ ▒ ░  ░▒ ░ ▒░ ▒ ░  ░   ░  ▒ ░  ░▒ ░ ▒░ ▒ ░
░ ░░ ░  ▒ ░  ░░   ░  ▒ ░░ ░   ░  ▒ ░  ░░   ░  ▒ ░
░  ░    ░     ░      ░        ░  ░     ░      ░  
                                                 
");
            Logger.LogInfo("NekogiriPeak has loaded!");

            // Create a Harmony instance and apply the patch
            var harmony = new Harmony("kirigiri.peak.nekogiri");
            harmony.PatchAll();  // Automatically patch all methods that have the PatchAttribute

            // Optionally log that the patch has been applied
            Logger.LogInfo("Made with <3 By Kirigiri \nhttps://discord.gg/TBs8Te5nwn");
        }
        [HarmonyPatch(typeof(CloudAPI), nameof(CloudAPI.CheckVersion))]
        public class CloudAPICheckVersionPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(Action<LoginResponse> response)
            {
                Debug.Log("[NekogiriPeak] Patching CloudAPI.CheckVersion");

                GameHandler.AddStatus<QueryingGameTimeStatus>(new QueryingGameTimeStatus());

                string filePath = Path.Combine(Application.dataPath, "..", "Kirigiri", "server.txt");
                string url;

                try
                {
                    url = File.ReadAllText(filePath).Trim();
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to read server.txt: " + e.Message);
                    GameHandler.ClearStatus<QueryingGameTimeStatus>();
                    return false;
                }
                Debug.Log("[NekogiriPeak] Using server URL from server.txt: " + url);

                Debug.Log("Sending GET Request to: " + url);
                UnityWebRequest request = UnityWebRequest.Get(url);

                request.SendWebRequest().completed += delegate (AsyncOperation _)
                {
                    GameHandler.ClearStatus<QueryingGameTimeStatus>();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.Log("Got error: " + request.error);
                        if (request.result != UnityWebRequest.Result.ConnectionError)
                        {
                            response?.Invoke(new LoginResponse
                            {
                                VersionOkay = true,
                                HoursUntilLevel = 1337,
                                MinutesUntilLevel = 1337,
                                SecondsUntilLevel = 1337,
                                Message = "Thank you for playing PEAK! Pro tip, tapping SPRINT while climbing makes you do a LUNGE!"
                            });
                        }
                        return;
                    }

                    string responseText = request.downloadHandler.text;
                    Debug.Log("Got message: " + responseText);
                    LoginResponse loginResponse = JsonUtility.FromJson<LoginResponse>(responseText);
                    response?.Invoke(loginResponse);
                };

                return false;
            }
        }
    }
}
