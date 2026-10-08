using System.Collections.Generic;
using ACS.SaveManager;
using UnityEngine;
using System;
using TMPro;


public class ObservableObject<T>
{
    protected T _value = default;
    internal bool ShowDebugLogs { get; set; } = false;

    internal event Action BeforeValueChange;
    internal event Action OnValueChanged;

    private bool RunOnce = false;

    internal T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                if (ShowDebugLogs) Debug.Log($"New Value {value} is  not equal to the old value.");
                _value = value;
                OnValueChanged?.Invoke(); 
            }
            else if(!RunOnce)
            {
                if (ShowDebugLogs) Debug.Log($"New Value {value} is equal to the old value. \n Running Notify once.");
                OnValueChanged?.Invoke();
                RunOnce = true;
            }
        }
    }

    internal void Initialize(string Key, T defaultValue)
    {
        OnValueChanged += () => 
        {
            if (ShowDebugLogs) Debug.Log("Updating texting value: " + Value);
            SaveManager.Prefs.SetObject(Key, Value, ShowDebugLogs);
        };
        Value = SaveManager.Prefs.GetObject(Key, defaultValue, ShowDebugLogs);
    }

    internal void Initialize(string Key, T defaultValue, TextMeshProUGUI coinsText)
    {
        OnValueChanged += () =>
        {
            if (coinsText) coinsText.text = Value.ToString();
            if (ShowDebugLogs) Debug.Log("Updating texting value: " + Value);
            SaveManager.Prefs.SetObject(Key, Value, ShowDebugLogs);
        };
        Value = SaveManager.Prefs.GetObject(Key, defaultValue, ShowDebugLogs);
    }

    internal void Initialize(string Key, T defaultValue, TextMeshProUGUI coinsText, bool debug)
    {
        OnValueChanged += () => 
        {
            ShowDebugLogs = debug;
            if (coinsText) coinsText.text = Value.ToString();
            if (ShowDebugLogs) Debug.Log("Updating texting value: " + Value);
            SaveManager.Prefs.SetObject(Key, Value, ShowDebugLogs);
        };
        Value = SaveManager.Prefs.GetObject(Key, defaultValue, ShowDebugLogs);
    }
}



