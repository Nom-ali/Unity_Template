/// Save Manager System
/// this script is using Newtonsoft.Json Lib
/// make sure you import this lib
/// you can add by name from package manager "com.unity.nuget.newtonsoft-json"

using UnityEngine;
using System.IO;
using System;
using TMPro;
using System.Globalization;
using System.Collections.Generic;

#if ACS_JSON
using Newtonsoft.Json;
#endif

namespace ACS.SaveManager
{
    public static class SaveManager
    {
        public static ObservableObject<int> Currency = new();
        public static event Action ProgressRestored;
        internal static void NotifyProgressRestored()
        {
            var handlers = ProgressRestored;
            if (handlers == null) return;
            foreach (Action handler in handlers.GetInvocationList())
                try { handler(); } catch (Exception ex) { Debug.LogException(ex); }
        }

        //Converting Cash digits to Alpahbet-digits
        public static string FormatEveryThirdPower(long target)
        {
            string[] ShortNotationList = new string[12] { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };

            double value = target;
            int baseValue = 0;
            string notationValue = "";
            string toStringValue;
            if (value >= 1000) // I start using the first notation at 10k
            {
                value /= 1000;
                baseValue++;
                while (Mathf.Round((float)value) >= 1000)
                {
                    value /= 1000;
                    baseValue++;
                }

                if (baseValue < 2)
                    toStringValue = "N1"; // display 1 decimal while under 1 million
                else
                    toStringValue = "N2"; // display 2 decimals for 1 million and higher

                if (baseValue > ShortNotationList.Length) return null;
                else notationValue = ShortNotationList[baseValue];
            }
            else toStringValue = ""; // string formatting at low numbers
            return value.ToString(toStringValue) + notationValue;
        }

        public static int GetLevel(TextMeshProUGUI levelText = null, bool debug = false)
        {
            int levelNo = Prefs.GetInt(SharedVariables.CurrentLevelNo, 0, debug);

            // A manual selection must take priority over post-completion random play.
            if (Prefs.HasKey(SharedVariables.RepeatingLevels))
            {
                levelNo = Prefs.GetInt(SharedVariables.RepeatingLevels, levelNo, debug);
                Prefs.DeleteKey(SharedVariables.RepeatingLevels, debug);
                if (levelText) levelText.text = "Level : " + (levelNo + 1);
                return levelNo;
            }

            if (Prefs.GetBool(SharedVariables.GameCompleted, false, debug) == true)
            {
                levelNo = Prefs.GetInt(SharedVariables.LastPlayedLevel, 0, debug);
                if (levelText) levelText.text = "Level : " + Prefs.GetInt(SharedVariables.RandomLevel).ToString();
            }
            else
                if (levelText) levelText.text = "Level : " + (levelNo + 1);

            return levelNo;
        }

        public static bool GetLevelModule(int moduleBy, int currentLevel, bool debug = false)
        {
            int levelNo;
            if (currentLevel > 1)
                levelNo = currentLevel;
            else
                return false;

            if (Prefs.GetBool(SharedVariables.GameCompleted, false, debug) == true)
            {
                return (Prefs.GetInt(SharedVariables.RandomLevel) % moduleBy) == 0 ? true : false;
            }
            else
                return (levelNo % moduleBy) == 0 ? true : false;
        }

        public static void SaveNextLevel(bool debug = false)
        {
            int levelIndex = Prefs.GetInt(SharedVariables.CurrentLevelNo, 0, debug);
            Prefs.SetInt(SharedVariables.CurrentLevelNo, levelIndex + 1, debug);
        }


        /// <summary>
        /// Use this only when there is no level selection
        /// And levels gonna repeat over and over
        /// Also This method will also assign random levels when all levels has been played.
        /// </summary>
        /// <param name="totalLevels">total numer of level</param>
        /// <param name="debug">should debug?</param>
        public static void SaveNextLevel(int totalLevels, bool debug = false)
        {
            if (totalLevels <= 0)
            {
                Debug.LogWarning("SaveManager: Cannot save level progression because no levels are configured.");
                return;
            }

            int levelIndex = Mathf.Clamp(Prefs.GetInt(SharedVariables.CurrentLevelNo, 0, debug), 0, totalLevels - 1);
            bool gameCompleted = Prefs.GetBool(SharedVariables.GameCompleted, false, debug);
            int highestUnlockedLevel = Mathf.Clamp(Prefs.GetInt(SharedVariables.UnlockedLevels, 0, debug), 0, totalLevels - 1);

            if (!gameCompleted)
            {
                int newlyUnlockedLevel = Mathf.Min(levelIndex + 1, totalLevels - 1);
                if (newlyUnlockedLevel > highestUnlockedLevel)
                    Prefs.SetInt(SharedVariables.UnlockedLevels, newlyUnlockedLevel, debug);
            }

            if (gameCompleted == false && levelIndex < totalLevels - 1)
            {
                Prefs.SetInt(SharedVariables.CurrentLevelNo, levelIndex + 1, debug);
            }
            else if (gameCompleted == false && levelIndex == totalLevels - 1)
            {
                Prefs.SetInt(SharedVariables.CurrentLevelNo, 0, debug);
                Prefs.SetBool(SharedVariables.GameCompleted, true, debug);
                Prefs.SetInt(SharedVariables.UnlockedLevels, totalLevels - 1, debug);
                if (debug) Debug.Log("SaveManager: ----- <color=yellow> Game Completed </color> -----");
            }

            if (Prefs.GetBool(SharedVariables.GameCompleted, false, debug) == true)
            {
                int lastLevel = Prefs.GetInt(SharedVariables.LastPlayedLevel, -1, debug);
                int nextLevel;

                if (totalLevels == 1)
                {
                    nextLevel = 0;
                }
                else if (lastLevel >= 0 && lastLevel < totalLevels)
                {
                    // Pick uniformly from every level except the most recently played one.
                    nextLevel = UnityEngine.Random.Range(0, totalLevels - 1);
                    if (nextLevel >= lastLevel)
                    {
                        nextLevel++;
                    }
                }
                else
                {
                    nextLevel = UnityEngine.Random.Range(0, totalLevels);
                }

                Prefs.SetInt(SharedVariables.LastPlayedLevel, nextLevel, debug);
                Prefs.SetInt(SharedVariables.RandomLevel, Prefs.GetInt(SharedVariables.RandomLevel, totalLevels, debug) + 1, debug);
            }
        }

        public static void SaveNextLevel_(int totalLevels)
        {
            if (Prefs.GetBool(SharedVariables.FreeMode, false))
            {
                while (true)
                {
                    int randomLevel = UnityEngine.Random.Range(0, totalLevels);
                    if (Prefs.GetInt(SharedVariables.RandomLevel, 3) != randomLevel)
                    {
                        Prefs.SetInt(SharedVariables.RandomLevel, randomLevel);
                        break;
                    }
                }
            }
            else
            {
                if (Prefs.GetInt(SharedVariables.UnlockedLevels, 0, true) < totalLevels)
                {
                    if (Prefs.GetInt(SharedVariables.CurrentLevelNo, 0) == Prefs.GetInt(SharedVariables.UnlockedLevels))
                        Prefs.SetInt(SharedVariables.UnlockedLevels, Prefs.GetInt(SharedVariables.UnlockedLevels, 0) + 1, true);
                    Prefs.SetInt(SharedVariables.CurrentLevelNo, Prefs.GetInt(SharedVariables.CurrentLevelNo) + 1, true);

                    if (Prefs.GetInt(SharedVariables.CurrentLevelNo, 0, true) == totalLevels)
                    {
                        Prefs.SetInt(SharedVariables.GameCompleted, 1);
                        Prefs.SetBool(SharedVariables.FreeMode, true, true);
                        GetRandomLevel(totalLevels);
                    }
                }
                else
                {
                    Prefs.SetInt(SharedVariables.GameCompleted, 1);
                    Prefs.SetBool(SharedVariables.FreeMode, true, true);
                    GetRandomLevel(totalLevels);
                }
            }
        }

        public static void InCaseOfRestart()
        {
            Prefs.SetInt(SharedVariables.CurrentLevelNo, Prefs.GetInt(SharedVariables.CurrentLevelNo) - 1, true);
        }

        public static int GetRandomLevel(int totalLevels, bool SaveRandomLevel = true)
        {
            int randomLevel = UnityEngine.Random.Range(3, totalLevels);
            if (SaveRandomLevel) Prefs.SetInt(SharedVariables.RandomLevel, randomLevel);
            return randomLevel;
        }

        public static class FileSystem
        {
            private static readonly string SaveFolder = Application.persistentDataPath + "/SaveFiles/";

            static FileSystem()
            {
                if (!Directory.Exists(SaveFolder))
                {
                    Directory.CreateDirectory(SaveFolder);
                }
            }

            public static void DeleteSaveFile(string fileName, bool debug = false)
            {
                try
                {
                    string filePath = SaveFolder + fileName + ".json";
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        if (debug) Debug.Log($"SaveManager: Successfully deleted {fileName}.json");
                    }
                    else
                    {
                        if (debug) Debug.LogWarning($"SaveManager: File {fileName}.json not found.");
                    }
                }
                catch (Exception e)
                {
                    if (debug) Debug.LogError($"SaveManager: Failed to delete {fileName}.json: {e.Message}");
                }
            }
        }

        public static class Prefs
        {
            // Cloud sync observes only explicitly configured progress keys.
            public static event Action<string> Changed;
            public static event Action<string> Accessed;
            private static readonly HashSet<string> accessedKeys = new HashSet<string>(StringComparer.Ordinal);
            public static IEnumerable<string> AccessedKeys => accessedKeys;
            private static void Notify(Action<string> handlers, string key)
            {
                if (!string.IsNullOrEmpty(key)) accessedKeys.Add(key);
                if (handlers == null) return;
                foreach (Action<string> handler in handlers.GetInvocationList())
                    try { handler(key); } catch (Exception ex) { Debug.LogWarning("Save observer: " + ex.Message); }
            }
            #region Where Magic Happens

            public static void Save()
            {
                PlayerPrefs.Save();
            }
            public static T Get<T>(string baseKey, T defaultValue = default(T), bool debug = false)
            {
                Notify(Accessed, baseKey);
#if ACS_JSON
                var value = JsonConvert.DeserializeObject<T>(PlayerPrefs.GetString(baseKey, JsonConvert.SerializeObject(defaultValue)));
#else
                var storedValue = PlayerPrefs.GetString(baseKey, SerializeFallback(defaultValue));
                var value = DeserializeFallback(storedValue, defaultValue);
#endif
                if (debug) Debug.Log($"<color=yellow>SaveManager: Getting Value </color> of {baseKey}: {value}");
                return value;
            }

            public static string Set<T>(string baseKey, T value, bool debug = false)
            {
#if ACS_JSON
                var serialized = JsonConvert.SerializeObject(value);
#else
                var serialized = SerializeFallback(value);
#endif
                PlayerPrefs.SetString(baseKey, serialized);
                Notify(Changed, baseKey);
                if (debug) Debug.Log($"<color=yellow>SaveManager: Setting Value </color> of {baseKey}: {value}");
                return serialized;
            }

            public static void DeleteKey(string baseKey, bool debug = false)
            {
                PlayerPrefs.DeleteKey(baseKey);
                Notify(Changed, baseKey);
                if (debug) Debug.Log($"<color=yellow>SaveManager: Deleted Value </color> of {baseKey}");
            }

            public static bool Has(string baseKey, bool debug = false)
            {
                Notify(Accessed, baseKey);
                var haskey = PlayerPrefs.HasKey(baseKey);
                if (debug) Debug.Log($"<color=yellow>SaveManager: Has key </color> Value of {baseKey}: {haskey}");
                return haskey;
            }

            #endregion

            #region Methods
            public static void SetInt(string baseKey, int value, bool debug = false) => Set(baseKey, value, debug);
            public static void SetFloat(string baseKey, float value, bool debug = false) => Set(baseKey, value, debug);
            public static void SetObject<T>(string baseKey, T value, bool debug = false) => Set(baseKey, value, debug);
            public static void SetColor(string baseKey, Color value, bool debug = false) => Set(baseKey, value, debug);
            public static void SetString(string baseKey, string value, bool debug = false) => Set(baseKey, value, debug);
            public static void SetVector3(string baseKey, Vector3 value, bool debug = false) => Set(baseKey, new SerializableVector3(value), debug);
            public static void SetVector2(string baseKey, Vector2 value, bool debug = false) => Set(baseKey, new SerializableVector2(value), debug);
            public static void SetBool(string baseKey, bool value, bool debug = false) => Set(baseKey, value, debug);
            #endregion

            #region Properties
            public static int GetInt(string baseKey, int defaultValue = 0, bool debug = false) => Get(baseKey, defaultValue, debug);
            public static float GetFloat(string baseKey, float defaultValue = 0, bool debug = false) => Get(baseKey, defaultValue, debug);
            public static T GetObject<T>(string baseKey, T defaultValue = default, bool debug = false) => Get(baseKey, defaultValue, debug);
            public static Color GetColor(string baseKey, Color defaultValue, bool debug = false) => Get(baseKey, defaultValue, debug);
            public static string GetString(string baseKey, string defaultValue, bool debug = false) => Get(baseKey, defaultValue, debug);
            public static Vector3 GetVector3(string baseKey, Vector3 defaultValue, bool debug = false) => Get(baseKey, new SerializableVector3(defaultValue), debug).ToVector3();
            public static Vector2 GetVector2(string baseKey, Vector2 defaultValue, bool debug = false) => Get(baseKey, new SerializableVector2(defaultValue), debug).ToVector2();
            public static bool GetBool(string baseKey, bool defaultValue, bool debug = false) => Get(baseKey, defaultValue, debug);
            public static bool HasKey(string baseKey, bool debug = false) => Has(baseKey, debug);
            #endregion

#if !ACS_JSON
            private static string SerializeFallback<T>(T value)
            {
                if (value == null)
                {
                    return string.Empty;
                }

                if (value is string stringValue)
                {
                    return stringValue;
                }

                if (value is IFormattable formattable)
                {
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                }

                return JsonUtility.ToJson(value);
            }

            private static T DeserializeFallback<T>(string rawValue, T defaultValue)
            {
                if (string.IsNullOrEmpty(rawValue))
                {
                    return defaultValue;
                }

                try
                {
                    if (typeof(T) == typeof(string))
                    {
                        return (T)(object)rawValue;
                    }

                    if (typeof(T) == typeof(int))
                    {
                        return (T)(object)int.Parse(rawValue, CultureInfo.InvariantCulture);
                    }

                    if (typeof(T) == typeof(float))
                    {
                        return (T)(object)float.Parse(rawValue, CultureInfo.InvariantCulture);
                    }

                    if (typeof(T) == typeof(double))
                    {
                        return (T)(object)double.Parse(rawValue, CultureInfo.InvariantCulture);
                    }

                    if (typeof(T) == typeof(long))
                    {
                        return (T)(object)long.Parse(rawValue, CultureInfo.InvariantCulture);
                    }

                    if (typeof(T) == typeof(bool))
                    {
                        return (T)(object)bool.Parse(rawValue);
                    }

                    return JsonUtility.FromJson<T>(rawValue);
                }
                catch
                {
                    return defaultValue;
                }
            }
#endif
        }
    }

    [Serializable]
    public struct SerializableVector3
    {
        public float x, y, z;

        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public readonly Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    public struct SerializableVector2
    {
        public float x, y;

        public SerializableVector2(Vector2 vector)
        {
            x = vector.x;
            y = vector.y;
        }

        public readonly Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }
    }

}




