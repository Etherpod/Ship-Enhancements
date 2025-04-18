using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShipEnhancements;

public static class ErnestoModListHandler
{
    private static List<string> ModList { get; set; }
    private static List<string> ActiveModList { get; set; }

    private static HttpClientGenerator _httpClientGenerator;

    public static void Initialize()
    {
        _httpClientGenerator = new HttpClientGenerator(
            "https://raw.githubusercontent.com/Etherpod/Ship-Enhancements/refs/heads/version-1.2.0/ShipEnhancements/dialogue/ErnestoModList.json",
            client => client.Timeout = System.TimeSpan.FromMilliseconds(2500)
        );

        var response = GetModList(_httpClientGenerator.Client);
        if (response != null)
        {
            var jsonValues = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(response.Result);

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

    private static Task<string> GetModList(HttpClient httpClient)
    {
        var response = httpClient.GetAsync(httpClient.BaseAddress);
        try
        {
            response.Wait(2500);
        }
        catch
        {
            ShipEnhancements.WriteDebugMessage("Could not access mod list!", warning: true);
            return null;
        }

        var httpResponse = response.Result;
        return httpResponse.Content.ReadAsStringAsync();
    }

    public static int GetNumberErnestos()
    {
        return ActiveModList != null ? ActiveModList.Count : -1;
    }

    public static int GetMaxErnestos()
    {
        return ModList != null ? ModList.Count : -2;
    }
}
