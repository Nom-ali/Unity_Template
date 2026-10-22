using ACS;
using ACS.SaveManager;
using ACS.Timer;
using NextLevelGames;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UIManager : UIManagerBase
{
    public static UIManager Instance;

    [Header("---------- Unity Services ----------")]
    [SerializeField] private ACS.UnityServices.UnityUpdateController unityServicesPrefab;

    [SerializeField] internal Timer timerScript;

    [Header("---------- In-Game ----------")]
    [SerializeField] private Button PlayBtn;
    [SerializeField] private Button LevelComplete2x;
    [SerializeField] private CoinsAnimation coinsAnimation;

    [SerializeField] private Button SkipLevelBtn;
    [SerializeField] private Button RewardedAdbtn_MM;
    [SerializeField] private CoinsAnimation RewardedAdbtn_coinsAnimation;

    [SerializeField] private TextMeshProUGUI[] levelText;

    #region Singleton
    public override void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        base.Awake();

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        timerScript?.gameObject.SetActive(false);

        SaveManager.Prefs.SetBool(SharedVariables.FreeMode, false);
        UpdateLevelText();
    }
    #endregion Singleton

  
    internal override IEnumerator Start()
    {
        yield return base.Start();

        initialized = true;
        StartInternetCheck();

        var unityStartup = ACS.UnityServices.UnityUpdateController.Ensure(unityServicesPrefab);
        if (unityStartup) yield return unityStartup.WaitForStartup();
        var services = ACS.UnityServices.UnityServicesManager.Instance;
        if (services && services.ProgressSync.IsEnabled)
        {
            var sync = services.ProgressSync.SynchronizeAsync();
            float deadline = Time.realtimeSinceStartup + 10;
            while (!sync.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
        }

        FakeLoadScene(new ButtonActionSimple() { OnBtnClick = OnClickAction.ShowPanel, Panel = PanelType.MainMenu});
        
        if(EnableCoinSystem) CoinBar?.gameObject.SetActive(true);

        Levelcomplete2xListener();
    }

    private UnityAction levelCompleteAction, skipAction, rewardedMenuAction, playAction;
    private static void BindButton(Button button, ref UnityAction owned, UnityAction next)
    {
        if (!button) return;
        if (owned != null) button.onClick.RemoveListener(owned);
        owned = next;
        if (owned != null) button.onClick.AddListener(owned);
    }
    private bool initialized;
    private Coroutine internetCheck;
    private void OnEnable() { if (initialized) StartInternetCheck(); }
    private void StartInternetCheck()
    {
        if (checkInternet && internetConnectivity != null && internetCheck == null)
            internetCheck = StartCoroutine(internetConnectivity.CheckInternet());
    }
    private void OnDisable()
    {
        if (internetCheck != null) StopCoroutine(internetCheck);
        internetCheck = null;
        internetConnectivity?.Stop();
    }
    private void OnDestroy()
    {
        BindButton(LevelComplete2x, ref levelCompleteAction, null);
        BindButton(SkipLevelBtn, ref skipAction, null);
        BindButton(RewardedAdbtn_MM, ref rewardedMenuAction, null);
        BindButton(PlayBtn, ref playAction, null);
        if (Instance == this) Instance = null;
    }

    void Levelcomplete2xListener()
    {
        if (LevelComplete2x)
        {
            BindButton(LevelComplete2x, ref levelCompleteAction, () =>
            {
#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY || ACS_UNITY_LEVELPLAY
                ACS.AdsManager.AdsManager.Instance.ShowRewardedAds(() =>
                {
                    LevelComplete2x.interactable = false;
                    coinsAnimation?.Init();
                });
#endif
            });
        }

        if (SkipLevelBtn)
        {
            BindButton(SkipLevelBtn, ref skipAction, () =>
            {
#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY || ACS_UNITY_LEVELPLAY
                ACS.AdsManager.AdsManager.Instance.ShowRewardedAds(() =>
                {
                    //SecurePlayerPrefs.SetInt("Level", SecurePlayerPrefs.GetInt("Level", 1) + 1);
                    UpdateLevelText();
                });
#endif
            });

        }

        if (RewardedAdbtn_MM)
        {
            BindButton(RewardedAdbtn_MM, ref rewardedMenuAction, () =>
            {
#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY || ACS_UNITY_LEVELPLAY
                ACS.AdsManager.AdsManager.Instance.ShowRewardedAds(() =>
                {
                    RewardedAdbtn_coinsAnimation?.Init();
                });
#endif
            });
        }


    }

    internal void PlayBtnAction(System.Action action)
    {
        BindButton(PlayBtn, ref playAction, () =>
        {
            action?.Invoke();
        });

    }

    internal void UpdateLevelText()
    {
        if (levelText != null && levelText.Length > 0)
        {
            foreach (var text in levelText)
            {
                //if (text != null) text.text = "Level " + SecurePlayerPrefs.GetInt("Level", 1); 
            }
        }
    }

}

