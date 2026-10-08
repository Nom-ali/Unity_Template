#if ACS_DOTWEEN
using DG.Tweening;
#endif
using ACS.SaveManager;
using UnityEngine;
using UnityEngine.UI;

public class SettingScriptSlider : MonoBehaviour
{
    [SerializeField] Button SettingBtn;
    [SerializeField] Image settingSlider;

    [Space]
    [SerializeField] private setting MusicSetting;
    [SerializeField] private setting SoundSetting;
    [SerializeField] private setting VibrationSetting;


    private void Awake()
    {
        settingSlider.fillAmount = 0;
    }

    private void Start()
    {
        SetAtStart();
        ToggleSlider();
        AddListerners();
    }

    void ToggleSlider()
    {
        SettingBtn.onClick.RemoveAllListeners();
        SettingBtn.onClick.AddListener(() =>
        {
            Hide();
        });
    }

    public void Show() 
    {
        settingSlider.fillAmount = 0;
#if ACS_DOTWEEN
        settingSlider.DOFillAmount(1, 0.5f).SetEase(Ease.OutQuad);
#else
        settingSlider.fillAmount = 1;
#endif
    }

    void Hide()
    {
        settingSlider.fillAmount = 1;
#if ACS_DOTWEEN
        settingSlider.DOFillAmount(0, 0.5f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            UIManager.Instance.HidePanel_Ind();
        });
#else
        settingSlider.fillAmount = 0;
        UIManager.Instance.HidePanel_Ind();
#endif
    }

    void AddListerners()
    {
        //Music
        if (MusicSetting.OnBtn)
        {
            MusicSetting.OnBtn.onClick.RemoveAllListeners();
            MusicSetting.OnBtn.onClick.AddListener(() =>
            {
                AudioPlayer.Instance?.ToggleMusic(false);
                SetAtStart();
            });
        }
        if (MusicSetting.OffBtn)
        {
            MusicSetting.OffBtn.onClick.RemoveAllListeners();
            MusicSetting.OffBtn.onClick.AddListener(() => 
            {
                AudioPlayer.Instance?.ToggleMusic(true);
                SetAtStart();
            });
        }

        //Sound
        if (SoundSetting.OnBtn)
        {
            SoundSetting.OnBtn.onClick.RemoveAllListeners();
            SoundSetting.OnBtn.onClick.AddListener(() =>
            {
                AudioPlayer.Instance?.ToggleSound(false);
                SetAtStart();
            });
        }
        if (SoundSetting.OffBtn)
        {
            SoundSetting.OffBtn.onClick.RemoveAllListeners();
            SoundSetting.OffBtn.onClick.AddListener(() =>
            {
                AudioPlayer.Instance?.ToggleSound(true);
                SetAtStart();
            });
        }

        //Vibration
        if (VibrationSetting.OnBtn)
        {
            VibrationSetting.OnBtn.onClick.RemoveAllListeners();
            VibrationSetting.OnBtn.onClick.AddListener(() =>
            {
                AudioPlayer.Instance?.ToggleVibration(false);
                SetAtStart();
            });

        }
        if (VibrationSetting.OffBtn)
        {
            VibrationSetting.OffBtn.onClick.RemoveAllListeners();
            VibrationSetting.OffBtn.onClick.AddListener(() =>
            {
                AudioPlayer.Instance?.ToggleVibration(false);
                SetAtStart();
            });
        }
    }

    void SetAtStart()
    {
        bool checkMusic = SaveManager.Prefs.GetBool(SharedVariables.Music, true) == true;
        MusicSetting.OnBtn?.gameObject.SetActive(checkMusic);
        MusicSetting.OffBtn?.gameObject.SetActive(!checkMusic);

        bool checkSound = SaveManager.Prefs.GetBool(SharedVariables.Sound, true) == true;
        SoundSetting.OnBtn?.gameObject.SetActive(checkSound);
        SoundSetting.OffBtn?.gameObject.SetActive(!checkSound);

        bool checkVibration = SaveManager.Prefs.GetBool(SharedVariables.Vibration, true) == true;
        VibrationSetting.OnBtn?.gameObject.SetActive(checkVibration);
        VibrationSetting.OffBtn?.gameObject.SetActive(!checkVibration);
    }

}

//[System.Serializable]
//public struct setting
//{
//    public Button OnBtn;
//    public Button OffBtn;
//}
