using ACS;
using ACS.SaveManager;
using ACS.Timer;
using NextLevelGames;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : UIManagerBase
{
    public static UIManager Instance;

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

        if(checkInternet) StartCoroutine(internetConnectivity.CheckInternet());

        FakeLoadScene(new ButtonActionSimple() { OnBtnClick = OnClickAction.ShowPanel, Panel = PanelType.MainMenu});
        
        if(EnableCoinSystem) CoinBar?.gameObject.SetActive(true);

        Levelcomplete2xListener();
    }

    void Levelcomplete2xListener()
    {
        if (LevelComplete2x)
        {
            LevelComplete2x.onClick.RemoveAllListeners();
            LevelComplete2x.onClick.AddListener(() =>
            {
#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY || ACS_UNITY_LEVELPLAY
                ACS.AdsManager.AdsManager.Instance.ShowRewardedAds(() =>
                {
                    LevelComplete2x.interactable = false;
                    coinsAnimation.Init();
                });
#endif
            });
        }

        if (SkipLevelBtn)
        {
            SkipLevelBtn.onClick.RemoveAllListeners();
            SkipLevelBtn.onClick.AddListener(() =>
            {
#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY || ACS_UNITY_LEVELPLAY
                ACS.AdsManager.AdsManager.Instance.ShowRewardedAds(() =>
                {
                    //SecurePlayerPrefs.SetInt("Level", SecurePlayerPrefs.GetInt("Level", 1) + 1);
                    UpdateLevelText();
                });
#endif
            });

            RewardedAdbtn_MM.onClick.RemoveAllListeners();
            RewardedAdbtn_MM.onClick.AddListener(() =>
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
        PlayBtn.onClick.RemoveAllListeners();
        PlayBtn.onClick.AddListener(() =>
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

