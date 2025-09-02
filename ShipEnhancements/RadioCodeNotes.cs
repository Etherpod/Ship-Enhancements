using Newtonsoft.Json;
using ShipEnhancements.Models.Json;
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ShipEnhancements;

public class RadioCodeNotes : MonoBehaviour
{
    [SerializeField]
    private GameObject[] _notes;

    private void Awake()
    {
        RefreshNotes();
    }

    public void RefreshNotes()
    {
        int mask = ShipEnhancements.SaveData.LearnedRadioCodes;
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
                ShipEnhancements.SaveData.LearnedRadioCodes |= 1 << i;
                ShipEnhancements.UpdateSaveFile();
            }
        }
    }
}
