using ACS;
using ACS.SaveManager;
using UnityEngine;
using UnityEngine.UI;

public class SettingScript : MonoBehaviour
{
    public AddListeners addListeners;
    [Space]
    [SerializeField] private setting MusicSetting;
    [SerializeField] private setting SoundSetting;
    [SerializeField] private setting VibrationSetting;

    private Button analyticsPrivacyButton;
    private Button playerAccountButton;
    private void Update()
    {
        var manager = ACS.UnityServices.UnityServicesManager.Instance;
        bool accountsAvailable = manager && manager.PlayerAccountsEnabled;
        if (accountsAvailable && !playerAccountButton)
        {
            playerAccountButton = ACS.UnityServices.UnityServicePanels.Button(transform, "Account",
                new Vector2(.25f, .13f), new Vector2(.75f, .20f));
            playerAccountButton.onClick.AddListener(() =>
            {
                var current = ACS.UnityServices.UnityServicesManager.Instance;
                if (current) current.GetComponent<ACS.UnityServices.UnityPlayerAccountsUI>()?.Show();
            });
        }
        if (playerAccountButton) playerAccountButton.gameObject.SetActive(accountsAvailable);
        bool available = manager && manager.AnalyticsStatus == ACS.UnityServices.ServiceStatus.Ready;
        if (available && !analyticsPrivacyButton)
        {
            analyticsPrivacyButton = ACS.UnityServices.UnityServicePanels.Button(transform, "Analytics Privacy",
                new Vector2(.25f, .04f), new Vector2(.75f, .11f));
            analyticsPrivacyButton.onClick.AddListener(() =>
            {
                var current = ACS.UnityServices.UnityServicesManager.Instance;
                if (current) current.Analytics.OpenPrivacyChoice();
            });
        }
        if (analyticsPrivacyButton) analyticsPrivacyButton.gameObject.SetActive(available);
    }

    private void OnEnable()
    {
        UIManagerBase uIManager = FindFirstObjectByType<UIManagerBase>();
        addListeners?.Init(uIManager);
    }

    private void Start()
    {
        SetAtStart();
        AddListerners();
    }

    void AddListerners()
    {
        AddButtonListeners(MusicSetting, AudioPlayer.Instance.ToggleMusic);
        AddButtonListeners(SoundSetting, AudioPlayer.Instance.ToggleSound);
        AddButtonListeners(VibrationSetting, AudioPlayer.Instance.ToggleVibration);

        AddSliderListener(MusicSetting, AudioPlayer.Instance.SetMusicVolume);
        AddSliderListener(SoundSetting, AudioPlayer.Instance.SetSFXVolume);
        AddSliderListener(VibrationSetting, value => AudioPlayer.Instance.ToggleVibration(value >= 0.5f));
    }

    private void AddButtonListeners(setting currentSetting, System.Action<bool> toggleSetting)
    {
        if (currentSetting.ControlType != SettingControlType.Buttons)
            return;

        if (currentSetting.OnBtn)
        {
            currentSetting.OnBtn.onClick.RemoveAllListeners();
            currentSetting.OnBtn.onClick.AddListener(() =>
            {
                toggleSetting(false);
                SetAtStart();
            });
        }
        if (currentSetting.OffBtn)
        {
            currentSetting.OffBtn.onClick.RemoveAllListeners();
            currentSetting.OffBtn.onClick.AddListener(() =>
            {
                toggleSetting(true);
                SetAtStart();
            });
        }
    }

    private void AddSliderListener(setting currentSetting, System.Action<float> setValue)
    {
        if (currentSetting.ControlType != SettingControlType.Slider || !currentSetting.Slider)
            return;

        currentSetting.Slider.onValueChanged.RemoveAllListeners();
        currentSetting.Slider.onValueChanged.AddListener(value =>
            setValue(Mathf.InverseLerp(currentSetting.Slider.minValue, currentSetting.Slider.maxValue, value)));
    }

    private void SetAtStart()
    {
        SetControlVisibility(MusicSetting);
        SetControlVisibility(SoundSetting);
        SetControlVisibility(VibrationSetting);

        SetButtonState(MusicSetting, SaveManager.Prefs.GetBool(SharedVariables.Music, true));
        SetButtonState(SoundSetting, SaveManager.Prefs.GetBool(SharedVariables.Sound, true));
        SetButtonState(VibrationSetting, SaveManager.Prefs.GetBool(SharedVariables.Vibration, true));

        SetSliderValue(MusicSetting, SaveManager.Prefs.GetFloat(SharedVariables.MusicVolume, 1f));
        SetSliderValue(SoundSetting, SaveManager.Prefs.GetFloat(SharedVariables.SoundVolume, 1f));
        SetSliderValue(VibrationSetting, SaveManager.Prefs.GetBool(SharedVariables.Vibration, true) ? 1f : 0f);
    }

    private static void SetControlVisibility(setting currentSetting)
    {
        bool useSlider = currentSetting.ControlType == SettingControlType.Slider;
        currentSetting.Slider?.gameObject.SetActive(useSlider);

        if (currentSetting.OnBtn)
            currentSetting.OnBtn.gameObject.SetActive(!useSlider);
        if (currentSetting.OffBtn)
            currentSetting.OffBtn.gameObject.SetActive(!useSlider);
    }

    private static void SetButtonState(setting currentSetting, bool isEnabled)
    {
        if (currentSetting.ControlType != SettingControlType.Buttons)
            return;

        currentSetting.OnBtn?.gameObject.SetActive(isEnabled);
        currentSetting.OffBtn?.gameObject.SetActive(!isEnabled);
    }

    private static void SetSliderValue(setting currentSetting, float value)
    {
        if (currentSetting.ControlType == SettingControlType.Slider && currentSetting.Slider)
        {
            float sliderValue = Mathf.Lerp(currentSetting.Slider.minValue, currentSetting.Slider.maxValue, Mathf.Clamp01(value));
            currentSetting.Slider.SetValueWithoutNotify(sliderValue);
        }
    }
}

[System.Serializable]
public enum SettingControlType
{
    Buttons,
    Slider,
    Empty,
}

[System.Serializable]
public struct setting
{
    public SettingControlType ControlType;
    [MyBox.ConditionalField(nameof(ControlType), false, SettingControlType.Buttons)]
    public Button OnBtn;
    [MyBox.ConditionalField(nameof(ControlType), false, SettingControlType.Buttons)]
    public Button OffBtn;
    [MyBox.ConditionalField(nameof(ControlType), false, SettingControlType.Slider)]
    public Slider Slider;
}




