using System.Collections;
using System.Collections.Generic;
using ACS.SaveManager;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionManager : MonoBehaviour
{
    [SerializeField] private Button nextBtn;
    [Header("UI References")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private LevelBtn levelButtonPrefab;
    [SerializeField] private Sprite[] levelIcons;
    [SerializeField] private string[] levelObjectives;

    [Header("Level Settings")]
    [SerializeField, Min(1)] private int totalLevels = 4;

    private readonly List<LevelBtn> generatedLevelButtons = new();

    private void Awake()
    {
        if (nextBtn)
            nextBtn.interactable = false;

        InitializeProgress();
        StartCoroutine(GenerateLevelButtons());
    }

    private void InitializeProgress()
    {
        int highestUnlockedLevel = Mathf.Clamp(
            SaveManager.Prefs.GetInt(SharedVariables.UnlockedLevels, 0),
            0,
            totalLevels - 1);
        SaveManager.Prefs.SetInt(SharedVariables.UnlockedLevels, highestUnlockedLevel);

        if (SaveManager.Prefs.HasKey(SharedVariables.RepeatingLevels))
            SaveManager.Prefs.DeleteKey(SharedVariables.RepeatingLevels);
    }

    private IEnumerator GenerateLevelButtons()
    {
        yield return null;

        if (!buttonContainer || !levelButtonPrefab)
        {
            Debug.LogError($"{name}: Assign a button container and level-button prefab.", this);
            yield break;
        }

        for (int i = 0; i < totalLevels; i++)
        {
            LevelBtn levelButton = Instantiate(levelButtonPrefab, buttonContainer);
            generatedLevelButtons.Add(levelButton);
            Sprite icon = levelIcons != null && i < levelIcons.Length ? levelIcons[i] : null;
            string objective = levelObjectives != null && i < levelObjectives.Length
                ? levelObjectives[i]
                : string.Empty;

            levelButton.Setup(i, objective, icon, () =>
            {
                SetSelectedLevel(levelButton);
                if (nextBtn)
                    nextBtn.interactable = true;
            });
        }

        SetSelectedLevelByIndex(SaveManager.Prefs.GetInt(SharedVariables.CurrentLevelNo, 0));

        if (levelObjectives == null || levelObjectives.Length < totalLevels)
            Debug.LogWarning($"{name}: Some levels have no objective text configured.", this);
    }

    private void SetSelectedLevel(LevelBtn selectedLevelButton)
    {
        foreach (LevelBtn levelButton in generatedLevelButtons)
        {
            if (levelButton)
                levelButton.SetHighlight(levelButton == selectedLevelButton);
        }
    }

    private void SetSelectedLevelByIndex(int selectedLevelIndex)
    {
        foreach (LevelBtn levelButton in generatedLevelButtons)
        {
            if (levelButton)
                levelButton.SetHighlight(levelButton.LevelIndex == selectedLevelIndex);
        }
    }
}
