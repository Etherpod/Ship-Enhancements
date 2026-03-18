using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace ShipEnhancements;

public static class ErnestoNetworkHandler
{
    private static List<string> ModList { get; set; }
    private static List<string> ActiveModList { get; set; }
    private static TextAsset ActiveDialogue { get; set; }

    private static HttpClientGenerator _modListClientGenerator;
    private static HttpClientGenerator _dialogueClientGenerator;

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
                ShipEnhancements.Instance.ModHelper.Storage.Save(modListResponse.Result, 
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
            ShipEnhancements.Instance.ModHelper.Storage.Save(dialogueResponse.Result, 
                "dialogue/ErnestoDialogue.txt");
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
        return File.ReadAllText(Path.Combine(
            ShipEnhancements.Instance.ModHelper.Manifest.ModFolderPath,
            path));
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
}
