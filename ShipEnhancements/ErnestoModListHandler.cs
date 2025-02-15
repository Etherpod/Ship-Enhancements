using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShipEnhancements;

public static class ErnestoModListHandler
{
    public static List<string> ModList { get; private set; }
    public static List<string> ActiveModList { get; private set; }

    private static HttpClientGenerator _httpClientGenerator;

    public static void Initialize()
    {
        _httpClientGenerator = new HttpClientGenerator(
            "https://raw.githubusercontent.com/Etherpod/Ship-Enhancements/refs/heads/version-1.2.0/ShipEnhancements/dialogue/ErnestoModList.json",
            client => client.Timeout = System.TimeSpan.FromMilliseconds(2500)
        );

        var response = GetModList(_httpClientGenerator.Client);
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

    private static Task<string> GetModList(HttpClient httpClient)
    {
        var response = httpClient.GetAsync(httpClient.BaseAddress);
        response.Wait(2500);

        if (!response.IsCompletedSuccessfully)
        {
            ShipEnhancements.WriteDebugMessage("Could not access mod list!", warning: true);
        }

        var httpResponse = response.Result;
        return httpResponse.Content.ReadAsStringAsync();
    }

    public static int GetNumberErnestos()
    {
        return ActiveModList.Count;
    }
}
