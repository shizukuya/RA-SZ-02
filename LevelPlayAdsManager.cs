using UnityEngine;
using Unity.Services.Core;
using Unity.Services.LevelPlay;
using System.Threading.Tasks;

public class LevelPlayAdsManager : MonoBehaviour
{
    public static LevelPlayAdsManager Instance { get; private set; }

    [Header("App Keys")]
    [SerializeField] private string androidAppKey = "your_android_app_key";
    [SerializeField] private string iosAppKey = "your_ios_app_key";

    [Header("Ad Unit IDs (Android)")]
    [SerializeField] private string androidBannerId = "your_android_banner_id";
    [SerializeField] private string androidInterstitialId = "your_android_interstitial_id";
    [SerializeField] private string androidRewardedId = "your_android_rewarded_id";

    [Header("Ad Unit IDs (iOS)")]
    [SerializeField] private string iosBannerId = "your_ios_banner_id";
    [SerializeField] private string iosInterstitialId = "your_ios_interstitial_id";
    [SerializeField] private string iosRewardedId = "your_ios_rewarded_id";

    [Header("Settings")]
    [SerializeField] private bool showBannerOnLoad = true;



    private bool isInitialized = false;
    private LevelPlayBannerAd bannerAd;
    private LevelPlayRewardedAd rewardedAd;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            InitializeLevelPlay();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Unity Services Initialization Failed: {e.Message}");
        }
    }

    private void InitializeLevelPlay()
    {
        // イベントリスナー登録
        LevelPlay.OnInitSuccess += OnSdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += OnSdkInitializationFailedEvent;

        string appKey = GetAppKey();
        Debug.Log($"Initializing LevelPlay with AppKey: {appKey}");
        
        // SDK初期化
        LevelPlay.Init(appKey);
    }

    private void OnSdkInitializationCompletedEvent(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay Initialization Completed!");
        isInitialized = true;

        // 初期化完了後に広告をロード
        if (showBannerOnLoad)
        {
            LoadBanner();
        }
        LoadInterstitial();
        LoadRewarded();
    }

    private void OnSdkInitializationFailedEvent(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay Initialization Failed: {error.ErrorMessage}");
    }

    private string GetAppKey()
    {
#if UNITY_ANDROID
        return androidAppKey;
#elif UNITY_IOS
        return iosAppKey;
#else
        return "dummy_key";
#endif
    }

    // ===== Banner =====

    public void LoadBanner()
    {
        if (!isInitialized) return;
        
        // すでに作成済みなら破棄（再ロードの場合）
        if (bannerAd != null)
        {
            bannerAd.DestroyAd();
        }

        string adUnitId = GetBannerId();
        Debug.Log($"Loading Banner with ID: {adUnitId}");

        // バナー作成 (サイズ: BANNER, 位置: BottomCenter)
        // コンストラクタ引数がバージョンにより異なる可能性があるため、必須のIDのみ指定してデフォルト設定を使用
        bannerAd = new LevelPlayBannerAd(adUnitId);

        // イベント登録
        bannerAd.OnAdLoaded += OnBannerLoaded;
        bannerAd.OnAdLoadFailed += OnBannerLoadFailed;

        // ロード
        bannerAd.LoadAd();
    }

    public void ShowBanner()
    {
        if (bannerAd != null)
        {
            bannerAd.ShowAd();
        }
    }

    public void HideBanner()
    {
        if (bannerAd != null)
        {
            bannerAd.HideAd();
        }
    }

    private void OnBannerLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner Loaded!");
        ShowBanner();
    }

    private void OnBannerLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError($"Banner Load Failed: {error.ErrorMessage}");
    }

    // ===== Interstitial =====

    public void LoadInterstitial()
    {
        if (!isInitialized) return;
        Debug.Log("LoadInterstitial Called");
        // LevelPlayInterstitialAd.Load(GetInterstitialId());
    }

    public void ShowInterstitial()
    {
        if (!isInitialized) return;
        Debug.Log("ShowInterstitial Called");
        // LevelPlayInterstitialAd.Show(GetInterstitialId());
    }

    // ===== Rewarded =====

    public void LoadRewarded()
    {
        if (!isInitialized) return;
        
        // すでに作成済みなら破棄
        if (rewardedAd != null)
        {
            rewardedAd.DestroyAd();
        }

        string adUnitId = GetRewardedId();
        Debug.Log($"Loading Rewarded Ad with ID: {adUnitId}");

        // リワード広告作成
        rewardedAd = new LevelPlayRewardedAd(adUnitId);

        // イベント登録
        rewardedAd.OnAdLoaded += OnRewardedAdLoaded;
        rewardedAd.OnAdLoadFailed += OnRewardedAdLoadFailed;
        rewardedAd.OnAdRewarded += OnRewardedAdReceivedReward;
        rewardedAd.OnAdClosed += OnRewardedAdClosed;
        rewardedAd.OnAdDisplayFailed += OnRewardedAdDisplayFailed;

        // ロード
        rewardedAd.LoadAd();
    }

    public void ShowRewarded(System.Action<bool> onComplete)
    {
        if (!isInitialized || rewardedAd == null) 
        {
            onComplete?.Invoke(false);
            return;
        }

        if (rewardedAd.IsAdReady())
        {
            this.onRewardedComplete = onComplete;
            // 広告表示中は音声を停止
            AudioListener.pause = true;
            rewardedAd.ShowAd();
        }
        else
        {
            Debug.Log("Rewarded Ad not ready");
            // ロードされていない場合はロードを試みる
            LoadRewarded();
            onComplete?.Invoke(false);
        }
    }

    private System.Action<bool> onRewardedComplete;

    private void OnRewardedAdLoaded(LevelPlayAdInfo info)
    {
        Debug.Log("Rewarded Ad Loaded");
    }

    private void OnRewardedAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError($"Rewarded Ad Load Failed: {error.ErrorMessage}");
    }

    private void OnRewardedAdReceivedReward(LevelPlayAdInfo info, LevelPlayReward reward)
    {
        Debug.Log("Rewarded Ad Received Reward");
        onRewardedComplete?.Invoke(true);
        onRewardedComplete = null;
    }

    private void OnRewardedAdClosed(LevelPlayAdInfo info)
    {
        Debug.Log("Rewarded Ad Closed");
        // 音声を再開
        AudioListener.pause = false;

        // 報酬イベントが遅れて来る可能性があるため、少し待ってから判定
        StartCoroutine(WaitAndCheckReward());
        
        // 次回のためにロード
        LoadRewarded();
    }

    private System.Collections.IEnumerator WaitAndCheckReward()
    {
        // 1秒待機 (Realtime)
        yield return new WaitForSecondsRealtime(1.0f);

        // まだコールバックが残っている＝報酬を受け取っていない
        if (onRewardedComplete != null)
        {
            Debug.Log("Reward not received within timeout. Treating as failure.");
            onRewardedComplete.Invoke(false);
            onRewardedComplete = null;
        }
    }

    private void OnRewardedAdDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
    {
        Debug.LogError($"Rewarded Ad Display Failed: {error.ErrorMessage}");
        // 音声を再開
        AudioListener.pause = false;

        if (onRewardedComplete != null)
        {
            onRewardedComplete.Invoke(false);
            onRewardedComplete = null;
        }
        // 次回のためにロード
        LoadRewarded();
    }

    // ===== ID Helper =====

    private string GetBannerId()
    {
#if UNITY_ANDROID
        return androidBannerId;
#elif UNITY_IOS
        return iosBannerId;
#else
        return "";
#endif
    }

    private string GetInterstitialId()
    {
#if UNITY_ANDROID
        return androidInterstitialId;
#elif UNITY_IOS
        return iosInterstitialId;
#else
        return "";
#endif
    }

    private string GetRewardedId()
    {
#if UNITY_ANDROID
        return androidRewardedId;
#elif UNITY_IOS
        return iosRewardedId;
#else
        return "";
#endif
    }
}
