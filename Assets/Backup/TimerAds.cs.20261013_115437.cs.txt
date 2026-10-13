using System.Collections;
using UnityEngine;

public class TimerAds : MonoBehaviour
{
    [SerializeField] private float interval = 60f;

    [Header("Popup")]
    [SerializeField] private bool ShowPopupBeforeAd = true;
    [SerializeField] private float PopupTime = 3f;
    [SerializeField] private GameObject Popup;

    private AnimationBase _animationBase;
    private AnimationBase animationBase
    {
        get
        {
            if (_animationBase == null)
                _animationBase = GetComponent<AnimationBase>();
            return _animationBase;
        }
    }
    void Hide()
    {
        if (animationBase)
            animationBase.Hide();
        else
            gameObject.SetActive(false);
    }

    void Show()
    {
        if(animationBase)
            animationBase.Show();
        else
            gameObject.SetActive(true);
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            yield return new WaitUntil(() =>
                UIManager.Instance.CurrentState() == GameStatus.GameState.None);

            yield return new WaitForSeconds(interval);

            if (UIManager.Instance.CurrentState() != GameStatus.GameState.None)
                continue;

            // Optional popup before ad
            if (ShowPopupBeforeAd && Popup != null)
            {
                Show();

                yield return new WaitForSeconds(PopupTime);

                Hide();

                // Optional hide animation delay
                yield return new WaitForSeconds(0.3f);
            }

            // Check state again before showing ad
            if (UIManager.Instance.CurrentState() == GameStatus.GameState.None)
                ShowAd();
        }
    }

    private void ShowAd()
    {
#if ACS_ADS || ACS_APPLOVIN || ACS_ADMOB || ACS_UNITYADS
        ACS.AdsManager.AdsManager.Instance?.ShowInterAds();
#endif
    }
}