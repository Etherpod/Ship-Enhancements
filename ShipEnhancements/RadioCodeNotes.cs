using Newtonsoft.Json;
using ShipEnhancements.Models.Json;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ShipEnhancements;

public class RadioCodeNotes : MonoBehaviour
{
    [SerializeField]
    private GameObject[] _notes;

    private SaveDataJson _saveData;

    private void Awake()
    {
        var data = JsonConvert.DeserializeObject<SaveDataJson>(
            File.ReadAllText(Path.Combine(ShipEnhancements.Instance.ModHelper.Manifest.ModFolderPath, "SESaveData.json"))
        );

        if (data is null)
        {
            return;
        }

        _saveData = data;
        RefreshNotes();
    }

    public void RefreshNotes()
    {
        int mask = _saveData.LearnedRadioCodes;
        var b = new BitArray([mask]);
        bool[] bits = new bool[b.Count];
        b.CopyTo(bits, 0);
        for (int i = 0; i < _notes.Length; i++)
        {
            if (_notes[i] != null)
            {
                _notes[i].SetActive(bits[i]);
            }
        }
    }

    public void OnEnterCode(string code)
    {
        for (int i = 0; i < _notes.Length; i++)
        {
            if (_notes[i].name == code)
            {
                _saveData.LearnedRadioCodes |= 1 << i;
                WriteToSaveFile();
            }
        }
    }

    private void WriteToSaveFile()
    {
        var data = JsonConvert.SerializeObject(_saveData);
        File.WriteAllText(Path.Combine(ShipEnhancements.Instance.ModHelper.Manifest.ModFolderPath, 
            "SESaveData.json"), data);
    }
}
