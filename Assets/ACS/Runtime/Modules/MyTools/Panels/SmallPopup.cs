using ACS;
using ACS.TextAnimation;
using System.Collections;
using TMPro;
using UnityEngine;

public class SmallPopup : MonoBehaviour
{
    [SerializeField] internal TextMeshProUGUI Message;
    private Coroutine playback;
    private ACS.TextAnimation.TextAnimation typing;
    private uint request;

    private void CancelPlayback()
    {
        ++request;
        if (playback != null) StopCoroutine(playback);
        playback = null;
        typing?.Kill();
        typing = null;
    }
    internal void HidePopup()
    {
        CancelPlayback();
        UIManagerBase.PlayPanelAnimation(gameObject, false);
    }
    public void ShowPopup(string message, bool autoOff, float delayedOff)
    {
        CancelPlayback();
        uint version = request;
        if (Message) Message.text = "";
        UIManagerBase.PlayPanelAnimation(gameObject, true);
        if (version == request && isActiveAndEnabled)
            playback = StartCoroutine(showPopup(message, autoOff, delayedOff, version));
    }
    private IEnumerator showPopup(string message, bool autoOff, float delayedOff, uint version)
    {
        yield return new WaitForSeconds(0.4f);
        if (version != request) yield break;
        if (Message)
        {
            typing = Message.WriteLetter(message ?? "", 0.5f);
            typing.Start();
        }
        if (autoOff)
        {
            yield return new WaitForSeconds(Mathf.Max(0, delayedOff));
            if (version == request) { playback = null; HidePopup(); }
        }
        if (version == request) playback = null;
    }
    private void OnDisable() => CancelPlayback();
}
