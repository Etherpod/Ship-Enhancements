﻿using System;
using UnityEngine;

public class CockpitButtonPanel : MonoBehaviour
{
    private int _numButtons = 0;

    private void Start()
    {
        if (_numButtons == 0)
        {
            gameObject.SetActive(false);
        }
    }

    public void AddButton()
    {
        _numButtons++;
    }
}