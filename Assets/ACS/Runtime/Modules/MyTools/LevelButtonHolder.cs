using ACS;
using ACS.LoadingManager;
using ACS.SaveManager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Legacy level-button view that follows the same highest-unlocked-index progression as LevelBtn.
/// </summary>
public class LevelButtonHolder : MonoBehaviour
{
    public Button button;
    [SerializeField] private Image image;
    [FormerlySerializedAs("Lock")]
    [SerializeField] private GameObject lockIcon;
    [FormerlySerializedAs("LevelText")]
    [SerializeField] private Text levelText;
    [FormerlySerializedAs("LevelIndex")]
    [SerializeField] private int levelIndex = -1;

    private void Awake()
    {
        if (!button)
            TryGetComponent(out button);
        if (!image)
            TryGetComponent(out image);
        if (!levelText)
            levelText = GetComponentInChildren<Text>();
    }

    public void SetupData(ButtonInfo buttonInfo)
    {
        if (buttonInfo == null || buttonInfo.levelNo == null)
            return;

        levelIndex = buttonInfo.levelNo.Value;

        if (levelText)
        {
            levelText.gameObject.SetActive(!string.IsNullOrEmpty(buttonInfo.LevelTitle));
            if (!string.IsNullOrEmpty(buttonInfo.LevelTitle))
                levelText.text = buttonInfo.LevelTitle + (levelIndex + 1);
        }

        if (buttonInfo.sprite && image)
            image.sprite = buttonInfo.sprite;

        int highestUnlockedLevel = SaveManager.Prefs.GetInt(SharedVariables.UnlockedLevels, 0);
        bool isUnlocked = levelIndex <= highestUnlockedLevel && !buttonInfo.Locked;

        if (lockIcon)
            lockIcon.SetActive(!isUnlocked);

        if (!button)
            return;

        button.enabled = true;
        button.interactable = isUnlocked;
        button.onClick.RemoveAllListeners();

        if (isUnlocked)
            button.onClick.AddListener(() => SelectLevel(levelIndex));
    }

    private void SelectLevel(int selectedLevelIndex)
    {
        SaveManager.Prefs.SetInt(SharedVariables.CurrentLevelNo, selectedLevelIndex);
        SaveManager.Prefs.SetInt(SharedVariables.RepeatingLevels, selectedLevelIndex);
        Debug.Log($"Level {selectedLevelIndex + 1} selected.", this);

        UIManager uiManager = UIManager.Instance;
        if (uiManager && uiManager.loadingScript)
        {
            uiManager.loadingScript.LoadingAsync(SceneManager.GetActiveScene().buildIndex + 1);
            return;
        }

        Debug.LogWarning($"{name}: Scene transition is not assigned. Loading the next scene directly.", this);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}

public class ButtonInfo
{
    public Sprite sprite;
    public int? levelNo;
    public string LevelTitle;
    public bool Locked;
}
