using ACS.SaveManager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One level-selection button. Unlocking uses the highest unlocked level index.
/// </summary>
public class LevelBtn : MonoBehaviour
{
    public Button button;
    public Image IconImage;
    public Image LockImage;
    public Image highlighter;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI ObjectiveText;

    private int levelNumber = -1;
    System.Action OnClick = null;

    public int LevelIndex => levelNumber;

    private void OnEnable()
    {
        Refresh();
    }

    public void Setup(int levelIndex, string objective, Sprite icon, System.Action onClick)
    {
        levelNumber = levelIndex;

        OnClick = onClick;
        if (levelText)
            levelText.text = $"Level {levelNumber + 1}";

        if (ObjectiveText)
            ObjectiveText.text = objective;

        if (IconImage && icon)
            IconImage.sprite = icon;
        else
            IconImage?.gameObject.SetActive(false);

        button.image.sprite = icon;

        Refresh();
    }

    private void Refresh()
    {
        if (levelNumber < 0 || !button)
            return;

        int highestUnlockedLevel = SaveManager.Prefs.GetInt(SharedVariables.UnlockedLevels, 0);
        bool isUnlocked = levelNumber <= highestUnlockedLevel;
        bool isCurrentLevel = levelNumber == SaveManager.Prefs.GetInt(SharedVariables.CurrentLevelNo, 0);

        LockImage?.gameObject.SetActive(!isUnlocked);
        SetHighlight(isCurrentLevel);
        
        button.enabled = isUnlocked;
        button.onClick.RemoveAllListeners();

        if (isUnlocked)
            button.onClick.AddListener(SelectLevel);

        if (button.image)
            button.image.color = isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
    }

    public void SetHighlight(bool isSelected)
    {
        highlighter?.gameObject.SetActive(isSelected);
    }

    private void SelectLevel()
    {
        SaveManager.Prefs.SetBool(SharedVariables.FreeMode, true, true);
        SaveManager.Prefs.SetInt(SharedVariables.CurrentLevelNo, levelNumber, true);
        SaveManager.Prefs.SetInt(SharedVariables.RepeatingLevels, levelNumber, true);
        OnClick?.Invoke();
        Debug.Log($"Level {levelNumber + 1} selected.", this);

    }

    private void ShowAds()
    {
#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY || ACS_UNITY_LEVELPLAY
        ACS.AdsManager.AdsManager.Instance.ShowInterAds();
#endif
    }
}
