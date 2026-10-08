using ACS.TextAnimation;
using System.Collections;
using TMPro;
using UnityEngine;

public class SmallPopup : MonoBehaviour
{
    [SerializeField] internal TextMeshProUGUI Message;

    private AnimationBase panel = null;

    void ShowPopup()
    {
        if(panel)
            panel.Show();
        else
            gameObject.SetActive(true);
    }

    internal void HidePopup()
    {
        if (panel)
            panel.Hide();
        else
            gameObject.SetActive(false);
    }

    public void ShowPopup(string message, bool autoOff, float delayedOff)
    {
        if (!panel && TryGetComponent(out AnimationBase animationBase))
        {
            panel = animationBase;
        }
        Message.text = "";
        ShowPopup();
        StartCoroutine(showPopup(message, autoOff, delayedOff));
    }
    
    private IEnumerator showPopup(string message, bool autoOff, float delayedOff)
    {

        yield return new WaitForSeconds(0.4f);
        
        if (Message) Message.WriteLetter(message, 0.5f).Start();

        if (autoOff)
        {
            yield return new WaitForSeconds(delayedOff);

            HidePopup();
        }
    }
}
