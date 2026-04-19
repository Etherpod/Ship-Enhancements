using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace ShipEnhancements;

public static class NetworkFileHandler
{
    private static List<string> ModList { get; set; }
    private static List<string> ActiveModList { get; set; }
    private static TextAsset ActiveDialogue { get; set; }
    private static TextAsset ActiveChangelog { get; set; }

    private static HttpClientGenerator _modListClientGenerator;
    private static HttpClientGenerator _dialogueClientGenerator;
    private static HttpClientGenerator _changelogClientGenerator;

    public static void Initialize()
    {
        _modListClientGenerator = new HttpClientGenerator(
            "https://raw.githubusercontent.com/Etherpod/Ship-Enhancements/refs/heads/main/ShipEnhancements/dialogue/ErnestoModList.json",
            client => client.Timeout = System.TimeSpan.FromMilliseconds(2500)
        );
        
        _dialogueClientGenerator = new HttpClientGenerator(
            "https://raw.githubusercontent.com/Etherpod/Ship-Enhancements/refs/heads/main/ShipEnhancements/dialogue/ErnestoDialogue.txt",
            client => client.Timeout = System.TimeSpan.FromMilliseconds(2500)
        );
        
        _changelogClientGenerator = new HttpClientGenerator(
            "https://raw.githubusercontent.com/Etherpod/Ship-Enhancements/refs/heads/version-2.3.0/ShipEnhancements/dialogue/changelog.txt",
            client => client.Timeout = System.TimeSpan.FromMilliseconds(2500)
        );

        var modListResponse = DownloadFile(_modListClientGenerator.Client);
        if (modListResponse != null && modListResponse.Result != "404: Not Found")
        {
            var jsonValues = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(modListResponse.Result);
            ModList = jsonValues["Mods"];
            
            if (ModList != null)
            {
                List<string> activeMods = [];

                foreach (string id in ModList)
                {
                    if (ShipEnhancements.Instance.ModHelper.Interaction.ModExists(id))
                    {
                        activeMods.Add(id);
                    }
                }

                ActiveModList = activeMods;
                ShipEnhancements.Instance.ModHelper.Storage.Save(jsonValues, 
                    "dialogue/ErnestoModList.json");
            
                ShipEnhancements.WriteDebugMessage("Number of Ernestos: " + ActiveModList.Count);
            }
        }
        else
        {
            var modList = LoadLocalFile("dialogue/ErnestoModList.json");
            if (modList != null)
            {
                var jsonValues = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(modList);
                ModList = jsonValues["Mods"];
                
                List<string> activeMods = [];

                foreach (string id in ModList)
                {
                    if (ShipEnhancements.Instance.ModHelper.Interaction.ModExists(id))
                    {
                        activeMods.Add(id);
                    }
                }

                ActiveModList = activeMods;
                ShipEnhancements.WriteDebugMessage("Number of Ernestos: " + ActiveModList.Count);
            }
            else
            {
                ModList = null;
                ActiveModList = null;
            }
        }

        var dialogueResponse = DownloadFile(_dialogueClientGenerator.Client);
        if (dialogueResponse != null && dialogueResponse.Result != "404: Not Found")
        {
            ActiveDialogue = new TextAsset(dialogueResponse.Result);
            File.WriteAllText(Path.Combine(ShipEnhancements.Instance.ModHelper.Manifest.ModFolderPath +
                "dialogue/ErnestoDialogue.txt"), dialogueResponse.Result.Replace("\n", System.Environment.NewLine));
        }
        else
        {
            var dialogue = LoadLocalFile("dialogue/ErnestoDialogue.txt");
            if (dialogue != null)
            {
                ActiveDialogue = new TextAsset(dialogue);
            }
            else
            {
                ActiveDialogue = new TextAsset(" DIALOGUE_BODY_SEPARATOR ");
            }
        }

        var changelogResponse = DownloadFile(_changelogClientGenerator.Client);
        if (changelogResponse != null && changelogResponse.Result != "404: Not Found")
        {
            ActiveChangelog = new TextAsset(changelogResponse.Result);
            File.WriteAllText(Path.Combine(ShipEnhancements.Instance.ModHelper.Manifest.ModFolderPath +
                "dialogue/changelog.txt"), changelogResponse.Result.Replace("\n", System.Environment.NewLine));
        }
        else
        {
            var changelog = LoadLocalFile("dialogue/changelog.txt");
            if (changelog != null)
            {
                ActiveChangelog = new TextAsset(changelog);
            }
            else
            {
                ActiveChangelog = new TextAsset(
                    "---404: Not Found\nCould not load the changelog!\nReinstall Ship Enhancements and check back here. " +
                    "If it still isn't working, please don't hesitate to contact me! (Etherpod)"
                );
            }
        }
    }

    private static Task<string> DownloadFile(HttpClient httpClient)
    {
        var response = httpClient.GetAsync(httpClient.BaseAddress);
        try
        {
            response.Wait(2500);
        }
        catch
        {
            ShipEnhancements.WriteDebugMessage("Could not access GitHub!", warning: true);
            return null;
        }

        var httpResponse = response.Result;
        return httpResponse.Content.ReadAsStringAsync();
    }

    private static string LoadLocalFile(string path)
    {
        var fullPath = Path.Combine(
            ShipEnhancements.Instance.ModHelper.Manifest.ModFolderPath,
            path);
        
        if (File.Exists(fullPath))
        {
            return File.ReadAllText(fullPath);
        }

        return null;
    }

    public static int GetNumberErnestos()
    {
        return ActiveModList != null ? ActiveModList.Count : -1;
    }

    public static int GetMaxErnestos()
    {
        return ModList != null ? ModList.Count : -2;
    }

    public static TextAsset GetErnestoQuestions()
    {
        return ActiveDialogue;
    }

    public static TextAsset GetChangelog()
    {
        return ActiveChangelog;
    }
}
