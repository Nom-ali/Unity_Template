using ACS.Animations;
using ACS.SaveManager;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

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
        if (settingSlider != null) settingSlider.fillAmount = 0;
    }

    private void Start()
    {
        SetAtStart();
        ToggleSlider();
        AddListerners();
    }

    private readonly Dictionary<Button, UnityAction> ownedBindings = new();
    private void BindButton(Button button, UnityAction callback)
    {
        if (!button) return;
        if (ownedBindings.TryGetValue(button, out var previous)) button.onClick.RemoveListener(previous);
        ownedBindings[button] = callback;
        button.onClick.AddListener(callback);
    }
    private void OnDestroy()
    {
        foreach (var binding in ownedBindings)
            if (binding.Key) binding.Key.onClick.RemoveListener(binding.Value);
        ownedBindings.Clear();
    }

    void ToggleSlider()
    {
        BindButton(SettingBtn, () =>
        {
            Hide();
        });
    }

    private AnimationHandle fillAnimation;
    private void OnDisable() { fillAnimation?.Cancel(); }
    public void Show() 
    {
        fillAnimation?.Cancel();
        settingSlider.fillAmount = 0;
        fillAnimation = settingSlider.AnimateFillAmount(1, 0.5f).SetOwner(this).SetEase(Ease.OutQuad);
    }

    void Hide()
    {
        fillAnimation?.Cancel();
        settingSlider.fillAmount = 1;
        fillAnimation = settingSlider.AnimateFillAmount(0, 0.5f).SetOwner(this).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            var manager = GetComponentInParent<ACS.UIManagerBase>();
            if (!manager) manager = UIManager.Instance;
            if (!manager || manager.IndPanelsList == null) return;
            foreach (var entry in manager.IndPanelsList)
            {
                if (entry.Panel && transform.IsChildOf(entry.Panel.transform))
                {
                    manager.HidePanel_Ind(entry.panelType);
                    return;
                }
            }
        });
    }

    void AddListerners()
    {
        //Music
        if (MusicSetting.OnBtn)
        {
            BindButton(MusicSetting.OnBtn, () =>
            {
                AudioPlayer.Instance?.ToggleMusic(false);
                SetAtStart();
            });
        }
        if (MusicSetting.OffBtn)
        {
            BindButton(MusicSetting.OffBtn, () =>
            {
                AudioPlayer.Instance?.ToggleMusic(true);
                SetAtStart();
            });
        }

        //Sound
        if (SoundSetting.OnBtn)
        {
            BindButton(SoundSetting.OnBtn, () =>
            {
                AudioPlayer.Instance?.ToggleSound(false);
                SetAtStart();
            });
        }
        if (SoundSetting.OffBtn)
        {
            BindButton(SoundSetting.OffBtn, () =>
            {
                AudioPlayer.Instance?.ToggleSound(true);
                SetAtStart();
            });
        }

        //Vibration
        if (VibrationSetting.OnBtn)
        {
            BindButton(VibrationSetting.OnBtn, () =>
            {
                AudioPlayer.Instance?.ToggleVibration(false);
                SetAtStart();
            });

        }
        if (VibrationSetting.OffBtn)
        {
            BindButton(VibrationSetting.OffBtn, () =>
            {
                AudioPlayer.Instance?.ToggleVibration(true);
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
