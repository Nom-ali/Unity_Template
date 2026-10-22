using MyBox;
using ACS;
using ACS.TextAnimation;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupPanel : MonoBehaviour, IInitialization
{
    [SerializeField] private AddListeners listeners;
    [Separator("********** Popup Panel **********")]
    [SerializeField] internal TextMeshProUGUI TitleText;
    [SerializeField] internal TextMeshProUGUI MessageText;
    [Space]
    [SerializeField] internal Button FirstBtn = null;
    [SerializeField] internal Button SecondBtn = null;

    private AudioSource _audioSource;
    private UIManagerBase manager;
    private Coroutine playback;
    private ACS.TextAnimation.TextAnimation typing;
    private UnityAction firstAction, secondAction;
    private uint request;

    public void Init(UIManagerBase uIManager)
    {
        manager = uIManager;
        if (isActiveAndEnabled) listeners?.Init(manager);
    }
    private void OnEnable()
    {
        if (!manager) manager = GetComponentInParent<UIManagerBase>();
        if (manager) listeners?.Init(manager);
    }
    private void Show() => UIManagerBase.PlayPanelAnimation(gameObject, true);
    private void Hide()
    {
        CancelPlayback();
        UIManagerBase.PlayPanelAnimation(gameObject, false);
    }
    private void CancelPlayback()
    {
        ++request;
        if (playback != null) StopCoroutine(playback);
        playback = null;
        typing?.Kill();
        typing = null;
        if (_audioSource) _audioSource.Stop();
        if (FirstBtn && firstAction != null) FirstBtn.onClick.RemoveListener(firstAction);
        if (SecondBtn && secondAction != null) SecondBtn.onClick.RemoveListener(secondAction);
        firstAction = secondAction = null;
    }
    private void PlaySound()
    {
        var audioPlayer = AudioPlayer.Instance;
        if (!audioPlayer || audioPlayer.sfxSettings == null) return;
        var clipData = audioPlayer.sfxSettings.GetClipById(AudioType.Typing);
        if (clipData == null) return;
        if (!_audioSource) _audioSource = GetComponent<AudioSource>();
        if (!_audioSource) _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.clip = clipData.clip;
        _audioSource.loop = clipData.loop;
        _audioSource.volume = audioPlayer.sfxSettings.volume * clipData.volumeMultiplier;
        _audioSource.Play();
    }
    public void ShowPopup(PopupSetting popupSetting, UIManagerBase uIManager)
    {
        if (popupSetting == null) return;
        CancelPlayback();
        uint version = request;
        Init(uIManager);
        if (FirstBtn) FirstBtn.gameObject.SetActive(false);
        if (SecondBtn) SecondBtn.gameObject.SetActive(false);
        if (TitleText) TitleText.text = popupSetting.Title;
        Show();
        if (version != request || !isActiveAndEnabled) return;
        playback = StartCoroutine(PlayPopup(popupSetting, version));
    }
    private IEnumerator PlayPopup(PopupSetting settings, uint version)
    {
        // Yield once so the tracked coroutine is assigned before any user callback.
        yield return null;
        while (MessageText && !MessageText.gameObject.activeInHierarchy)
        {
            if (version != request) yield break;
            yield return null;
        }
        if (version != request) yield break;
        if (MessageText)
        {
            bool finished = false;
            typing = MessageText.WriteLetter(settings.Message ?? "", settings.TextDuration)
                .OnComplete(() => finished = true);
            PlaySound();
            typing.Start();
            while (!finished && MessageText)
            {
                if (version != request) yield break;
                yield return null;
            }
            typing = null;
        }
        if (version != request) yield break;
        if (_audioSource) _audioSource.Stop();
        BindButton(FirstBtn, settings.EnableFirstBtn, settings.FirstBtnText,
            settings.FirstBtnAction, settings.m_FirstBtnAction, version, ref firstAction);
        BindButton(SecondBtn, settings.EnableSecondBtn, settings.SecondBtnText,
            settings.SecondBtnAction, settings.m_SecondBtnAction, version, ref secondAction);
        settings.OnComplete?.Invoke();
        if (version != request) yield break;
        if (settings.AutoClosePopup)
        {
            // Preserve the existing post-typing close delay, independent of game pause.
            yield return new WaitForSecondsRealtime(Mathf.Max(0, settings.TextDuration + settings.AutoCloseDelay));
            if (version == request) { playback = null; Hide(); }
        }
        if (version == request) playback = null;
    }
    private void BindButton(Button button, bool enabled, string label, System.Action action,
        ButtonActionSimple buttonAction, uint version, ref UnityAction binding)
    {
        if (!button) return;
        button.gameObject.SetActive(enabled);
        if (!enabled) return;
        var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text) text.text = label;
        binding = () =>
        {
            if (version != request) return;
            AudioPlayer.Instance?.PlayButtonSound();
            action?.Invoke();
            if (version != request) return;
            if (manager) manager.OnButtonClicked(buttonAction);
            if (version == request) Hide();
        };
        button.onClick.AddListener(binding);
    }
    private void OnDisable()
    {
        CancelPlayback();
        listeners?.OnDisableRemoveAll();
    }
}

[System.Serializable]
public class PopupSetting
{
    [SerializeField] internal string Title = "";
    [SerializeField] internal string Message = "";
    [SerializeField] internal float TextDuration = 1;
    [SerializeField] internal bool AutoClosePopup = false;
    [ConditionalField(nameof(AutoClosePopup), false)]
    [SerializeField] internal float AutoCloseDelay = 1f;
    [SerializeField] internal bool EnableFirstBtn = false;
    [SerializeField] internal string FirstBtnText = "OK";
    [SerializeField] internal System.Action FirstBtnAction = null;
    [SerializeField] internal ButtonActionSimple m_FirstBtnAction;
    [SerializeField] internal bool EnableSecondBtn = false;
    [SerializeField] internal string SecondBtnText = "";
    [SerializeField] internal System.Action SecondBtnAction = null;
    [SerializeField] internal ButtonActionSimple m_SecondBtnAction;
    [SerializeField] internal bool AdsIcon = false;
    [SerializeField] internal System.Action OnComplete = null;

    // Parameterless constructor
    public PopupSetting()
    {
    }

    //public PopupSetting(string message, string title = "", string firstBtnText = "", System.Action firstAction = null,
    //    bool enableSecondbtn = false, string secondBtnText = "", Action secondAction = null, bool adsIcon = false, Action onComplete = null)
    //{
    //    Title = title;
    //    Message = message;
    //    FirstBtnText = firstBtnText;
    //    FirstBtnAction = firstAction;
    //    EnableSecondBtn = enableSecondbtn;
    //    SecondBtnText = secondBtnText;
    //    SecondBtnAction = secondAction;
    //    AdsIcon = adsIcon;
    //    OnComplete = onComplete;
    //}
}



