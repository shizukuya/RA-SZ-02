// ファイル名: SimpleGameManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// ゲーム全体の管理（スコア、コンボ、サウンド）
/// </summary>
public class SimpleGameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text scoreText2;
    [SerializeField] private TMP_Text scoreText3;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private AdConfirmationPanel adConfirmationPanel; // 広告確認パネル
    [SerializeField] private GameObject wall;
    [SerializeField] private Image nextItemImage; // 次のアイテムを表示するImage

    [Header("エフェクト")]
    [SerializeField] private GameObject matchEffectPrefab;    // 出荷（消滅）時のエフェクト
    [SerializeField] private GameObject comboEffectPrefab;    // コンボ時のエフェクト
    [SerializeField] private GameObject fillingNEffectPrefab; // FillingN生成時のエフェクト

    [Header("スコア設定")]
    [SerializeField] private int matchBaseScore = 500;       // おにぎり完成（消滅）時の基本スコア
    [SerializeField] private float comboMultiplier = 0.5f;       // コンボ倍率（combo * multiplier）

    [Header("サウンド - 共通")]
    [SerializeField] private AudioClip dropSound;       // se_drop.mp3
    [SerializeField] private AudioClip downSound;       // down.mp3
    [SerializeField] private AudioClip noriFwaSound;    // nori-fwa.mp3
    [SerializeField] private AudioClip guzaiSound;      // guzai.mp3
    [SerializeField] private AudioClip noriSound;       // nori.mp3
    [SerializeField] private AudioClip fwaSound;        // fwa.mp3
    [SerializeField] private AudioClip kanseiSound;     // kansei.mp3
    [SerializeField] private AudioClip completeSound;   // complete.mp3
    [SerializeField] private AudioClip shuxtsukaSound;  // shuxtuka.mp3
    [SerializeField] private AudioClip getSound;        // get.mp3

    [SerializeField] private AudioClip gameoverSound;   // gameover.mp3
    [SerializeField] private AudioClip buttonSound;     // ボタン音

    [Header("サウンド - 具材別")]
    [SerializeField] private AudioClip rockSound;       // iwa.mp3
    [SerializeField] private AudioClip rockVoiceSound;  // iwa-voice.mp3
    [SerializeField] private AudioClip nigirimeshiSound; // nigirimeshi-1.mp3
    [SerializeField] private AudioClip picklesSound;      // otukemono.mp3
    [SerializeField] private AudioClip picklesVoiceSound; // otukemono-voice.mp3
    [SerializeField] private AudioClip fillingNCreatedSound; // FillingN生成時の固定音

    [Header("BGM設定")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip bgmClip;

    private AudioSource audioSource; // SE用
    private float bgmVolume = 0.5f;
    private float seVolume = 0.5f;
    private const string BGM_VOLUME_KEY = "BGM_Volume";
    private const string SE_VOLUME_KEY = "SE_Volume";
    private int currentScore = 0;
    private int comboCount = 0;
    private bool isGameOver = false;
    // PlayerPrefs Keys
    private const string GAME_OVER_COUNT_KEY = "GameOverCount";
    private const string LAST_RESET_TIME_KEY = "LastResetTime";
    private const int GAME_OVER_LIMIT = 2;

    public bool IsGameOver => isGameOver;
    public int ComboCount => comboCount;

    // ===== フィーバーモード設定 =====
    [Header("フィーバーモード")]
    // [SerializeField] private TMP_Text feverText; // "FEVER!!" 表示用（廃止：裏設定のため）
    [SerializeField] private float feverIntervalMin = 90f; // 次のフィーバーまでの間隔（最小）
    [SerializeField] private float feverIntervalMax = 150f; // 次のフィーバーまでの間隔（最大）
    [SerializeField] private float feverDurationMin = 30f; // フィーバー継続時間（最小）
    [SerializeField] private float feverDurationMax = 60f; // フィーバー継続時間（最大）
    [SerializeField] private int feverLimitScore = 15000; // このスコアを超えたら確変なし

    private bool isFeverTime = false;
    public bool IsFeverTime => isFeverTime;

    private float feverTimer = 0f;
    private float currentFeverTargetTime = 0f;

    // シングルトン
    public static SimpleGameManager Instance { get; private set; }

    private SimpleTouchController touchController;

    private void Awake()
    {
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // コントローラーの参照を取得
        touchController = FindFirstObjectByType<SimpleTouchController>();

        UpdateScoreUI();
        UpdateComboUI();

        wall.SetActive(true);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // 音量設定のロードと適用
        bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
        seVolume = PlayerPrefs.GetFloat(SE_VOLUME_KEY, 0.5f);

        // BGM再生開始
        if (bgmAudioSource != null && bgmClip != null)
        {
            bgmAudioSource.clip = bgmClip;
            bgmAudioSource.loop = true;
            bgmAudioSource.volume = bgmVolume;
            bgmAudioSource.Play();
        }

        // 初回のフィーバー待ち時間を設定
        SetNextFeverSchedule(false);

        // ゲームオーバー制限チェック
        CheckGameOverLimit();
        // if (feverText != null) feverText.gameObject.SetActive(false);
    }

    private void SetNextFeverSchedule(bool enterFever)
    {
        feverTimer = 0f;
        isFeverTime = enterFever;

        if (isFeverTime)
        {
            // フィーバー開始：継続時間を決定
            currentFeverTargetTime = Random.Range(feverDurationMin, feverDurationMax);
            Debug.Log($"[Fever] Start! Duration: {currentFeverTargetTime:F1}s");
            // if (feverText != null) feverText.gameObject.SetActive(true);
            
            // フィーバー開始音などを鳴らすならここ
            // PlaySound(feverStartSound); 
        }
        else
        {
            // フィーバー終了：次の開始までの時間を決定
            currentFeverTargetTime = Random.Range(feverIntervalMin, feverIntervalMax);
            Debug.Log($"[Fever] End. Next in: {currentFeverTargetTime:F1}s");
            // if (feverText != null) feverText.gameObject.SetActive(false);
        }
    }

    // ===== サウンド再生 =====

    public void PlayDropSound()
    {
        PlaySound(dropSound);
    }

    public void PlayDownSound()
    {
        PlaySound(downSound);
    }

    public void PlayNoriFwaSound()
    {
        PlaySound(noriFwaSound);
    }

    public void PlayNoriSound()
    {
        PlaySound(noriSound);
        PlaySound(fwaSound);
    }

    public void PlayFillingSound(AudioClip clip)
    {
        if (clip != null)
        {
            PlaySound(clip);
        }
        PlaySound(guzaiSound);
    }

    public void PlayWrappingSound(AudioClip clip)
    {
        // 指定されたクリップがあればそれを、なければデフォルトを再生
        if (clip != null)
        {
            PlaySound(clip);
        }
        else
        {
            PlaySound(nigirimeshiSound);
        }
    }

    public void PlayRockSound()
    {
        PlaySound(rockSound);
        PlaySound(rockVoiceSound);
    }

    public void PlayPicklesSound()
    {
        PlaySound(picklesSound);
        PlaySound(picklesVoiceSound);
    }

    public void PlayCompleteSound()
    {
        StopAllSounds();
        PlaySound(kanseiSound);
        PlaySound(completeSound);
    }

    public void PlayConsumeSound()
    {
        StopAllSounds();
        PlaySound(shuxtsukaSound);
        PlaySound(getSound);
    }

    public void PlayButtonSound()
    {
        PlaySound(buttonSound);
    }

    public void PlayGameOverSound()
    {
        // ゲームオーバー音は例外的に鳴らす
        if (gameoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(gameoverSound, seVolume);
        }
    }

    public void StopAllSounds()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        // ゲームオーバー時はSEを鳴らさない
        if (isGameOver) return;

        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, seVolume);
        }
    }

    // ===== スコア・コンボ処理 =====

    /// <summary>
    /// FillingN が生成された時（コンボカウント用）
    /// </summary>
    public void OnFillingNCreated()
    {
        // 現状は特に処理なし（将来的な連鎖判定用）
    }

    /// <summary>
    /// FillingN 同士が合体して消滅した時に呼ばれる
    /// </summary>
    public void OnOnigiriMatched(FillingData filling, Vector3 position, int count = 2)
    {
        comboCount++;

        // コンボ数に応じてピッチを上げる（最大3.0倍）
        if (audioSource != null)
        {
            float newPitch = 1.0f + (comboCount * 0.1f);
            audioSource.pitch = Mathf.Clamp(newPitch, 1.0f, 3.0f);
        }

        // エフェクト再生
        PlayEffect(matchEffectPrefab, position);
        if (comboCount > 1)
        {
            PlayEffect(comboEffectPrefab, position);
        }

        // サウンド再生（優先度高：他の音を止めて鳴らす）
        StopAllSounds();
        PlaySound(kanseiSound); // または専用の出荷音があればそれ
        PlaySound(completeSound);

        // 岩またはお漬物を一つ破壊
        SimpleItem[] allItems = FindObjectsByType<SimpleItem>(FindObjectsSortMode.None);
        Debug.Log($"[OnOnigiriMatched] 全アイテム数: {allItems.Length}");
        bool obstacleDestroyed = false;

        // まず岩を探す
        foreach (var item in allItems)
        {
            if (item.CurrentState == OnigiriState.Rock)
            {
                Debug.Log($"[OnOnigiriMatched] 岩を発見: {item.name}, 破壊を試みます");
                item.DestroyRock();
                obstacleDestroyed = true;
                break; // 1つだけ破壊
            }
        }

        // 岩がなければお漬物を探す
        if (!obstacleDestroyed)
        {
            foreach (var item in allItems)
            {
                if (item.CurrentState == OnigiriState.Pickles)
                {
                    Debug.Log($"[OnOnigiriMatched] お漬物を発見: {item.name}, 破壊を試みます");
                    item.DestroyPickles();
                    obstacleDestroyed = true;
                    break; // 1つだけ破壊
                }
            }
        }

        if (!obstacleDestroyed)
        {
            Debug.Log("[OnOnigiriMatched] 破壊可能な岩・お漬物が見つかりませんでした");
        }

        // スコア計算
        int baseScore = matchBaseScore;

        // 具材スコア加算
        if (filling != null)
        {
            baseScore += filling.Score;
        }

        // コンボボーナス + 連鎖ボーナス
        float comboBonus = 1f + (comboCount * comboMultiplier);
        // 個数分加算（2個ならx1, 3個ならx2... と増やすか、単純に個数倍するか）
        // ここでは (count - 1) 倍して、3つ消えたら2倍のスコアが入るようにしてみる
        // あるいは単純に baseScore * count * comboBonus でも良い
        
        // 仕様: 1+1で消滅(2個)。1+1+1で3個。
        // 多くのパズルゲームでは消した数が多いほど指数関数的に増えるが、
        // ここではシンプルに (count - 1) を乗算係数に追加する
        
        int earnedScore = Mathf.RoundToInt(baseScore * (count - 1) * comboBonus);

        currentScore += earnedScore;

        Debug.Log($"[Matched] スコア +{earnedScore}（コンボ x{comboCount}） 合計: {currentScore}");

        UpdateScoreUI();
        UpdateComboUI();
    }

    /// <summary>
    /// コンボをリセット（アイテム落下後など）
    /// </summary>
    public void ResetCombo()
    {
        if (comboCount > 0)
        {
            Debug.Log($"コンボリセット（{comboCount}コンボ終了）");
            comboCount = 0;
            UpdateComboUI();

            // ピッチをリセット
            if (audioSource != null)
            {
                audioSource.pitch = 1.0f;
            }
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{currentScore}";
        }
        if (scoreText2 != null)
        {
            scoreText2.text = $"Score: {currentScore}";
        }
        if (scoreText3 != null)
        {
            scoreText3.text = $"Score: {currentScore}";
        }
    }

    private void UpdateComboUI()
    {
        if (comboText != null)
        {
            if (comboCount > 1)
            {
                comboText.text = $"{comboCount} Combo!";
                comboText.gameObject.SetActive(true);
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 次のアイテム画像を更新
    /// </summary>
    public void UpdateNextItemUI(Sprite sprite)
    {
        if (nextItemImage != null)
        {
            if (sprite != null)
            {
                nextItemImage.sprite = sprite;
                nextItemImage.gameObject.SetActive(true);
                // アスペクト比を維持
                nextItemImage.preserveAspect = true;
            }
            else
            {
                nextItemImage.gameObject.SetActive(false);
            }
        }
    }

    // ===== ゲームオーバー =====

    public void TriggerGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        
        // カウントアップと保存
        int count = PlayerPrefs.GetInt(GAME_OVER_COUNT_KEY, 0);
        count++;
        PlayerPrefs.SetInt(GAME_OVER_COUNT_KEY, count);
        PlayerPrefs.Save();
        
        Debug.Log($"ゲームオーバー！ (累積回数: {count})");

        // SEをすべて停止
        StopAllSounds();

        // 制限に達したかチェック
        if (count >= GAME_OVER_LIMIT)
        {
            // 制限到達 -> 広告確認パネルを表示（強制）
            PlayGameOverSound();
            ShowAdConfirmationPanel(true);
        }
        else
        {
            // 通常のゲームオーバー処理
            ShowGameOverPanel();
        }
    }

    private void CheckGameOverLimit()
    {
        // 24時間経過チェック
        string lastResetStr = PlayerPrefs.GetString(LAST_RESET_TIME_KEY, "");
        if (!string.IsNullOrEmpty(lastResetStr))
        {
            System.DateTime lastResetTime = System.DateTime.Parse(lastResetStr);
            if ((System.DateTime.Now - lastResetTime).TotalHours >= 24)
            {
                // 24時間経過でリセット
                ResetGameOverCount();
            }
        }
        else
        {
            // 初回起動時などは現在時刻をセット
            PlayerPrefs.SetString(LAST_RESET_TIME_KEY, System.DateTime.Now.ToString());
            PlayerPrefs.Save();
        }

        int count = PlayerPrefs.GetInt(GAME_OVER_COUNT_KEY, 0);
        if (count >= GAME_OVER_LIMIT)
        {
            // ゲーム開始時に制限に達している場合 -> プレイ不可、広告パネル表示
            Debug.Log("ゲームオーバー制限に達しています。プレイ不可。");
            
            // スポナーを停止
            SimpleSpawner spawner = FindFirstObjectByType<SimpleSpawner>();
            if (spawner != null) spawner.enabled = false;

            // 広告パネル表示
            ShowAdConfirmationPanel(false);
        }
    }

    private void ShowAdConfirmationPanel(bool fromGameOver)
    {
        if (adConfirmationPanel != null && LevelPlayAdsManager.Instance != null)
        {
            adConfirmationPanel.Show(
                onConfirmed: () => 
                {
                    // 広告を見る -> リワード広告再生
                    LevelPlayAdsManager.Instance.ShowRewarded((success) => 
                    {
                        if (success)
                        {
                            // 広告視聴完了 -> カウントリセット
                            Debug.Log("Reward Granted! Resetting Count...");
                            ResetGameOverCount();
                            RestartGame();
                        }
                        else
                        {
                            // 失敗 -> TopSceneへ（プレイ不可のため）
                            GoToTopScene();
                        }
                    });
                },
                onCancelled: () => 
                {
                    // キャンセル -> TopSceneへ
                    GoToTopScene();
                }
            );
        }
        else
        {
            // パネルがない場合はTopSceneへ戻すしかない
            GoToTopScene();
        }
    }

    private void ResetGameOverCount()
    {
        PlayerPrefs.SetInt(GAME_OVER_COUNT_KEY, 0);
        PlayerPrefs.SetString(LAST_RESET_TIME_KEY, System.DateTime.Now.ToString());
        PlayerPrefs.Save();
    }

    private void GoToTopScene()
    {
        // TopSceneへ遷移 (シーン名が "TopScene" であると仮定)
        UnityEngine.SceneManagement.SceneManager.LoadScene("TopScene");
    }

    private void ShowGameOverPanel()
    {
        PlayGameOverSound();

        if (gameOverPanel != null)
        {
            Debug.Log("GameOverPanel を表示します");
            gameOverPanel.SetActive(true);
            wall.SetActive(false);
        }
        else
        {
            Debug.LogError("GameOverPanel が割り当てられていません！");
        }

        SimpleSpawner spawner = FindFirstObjectByType<SimpleSpawner>();
        if (spawner != null)
        {
            spawner.enabled = false;
        }

        // キャッシュしたコントローラーを使用
        if (touchController != null)
        {
            touchController.OnGameOver();
        }
        else
        {
            // キャッシュがない場合のフォールバック
            touchController = FindFirstObjectByType<SimpleTouchController>();
            if (touchController != null)
            {
                touchController.OnGameOver();
            }
        }
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    [Header("ゲームオーバー判定")]
    [SerializeField] private float gameOverLineY = 3.0f;
    [SerializeField] private float gameOverCheckInterval = 0.5f;
    private float gameOverTimer = 0f;

    private void Update()
    {
        if (isGameOver) return;

        // ゲームオーバー判定
        gameOverTimer += Time.deltaTime;
        if (gameOverTimer >= gameOverCheckInterval)
        {
            gameOverTimer = 0f;
            CheckHeightGameOver();
        }

        // フィーバータイマー
        feverTimer += Time.deltaTime;
        if (feverTimer >= currentFeverTargetTime)
        {
            // モード切り替え
            bool nextState = !isFeverTime;

            // スコアが上限を超えていたら、確変（Fever）には入らない
            if (currentScore >= feverLimitScore)
            {
                nextState = false;
            }

            SetNextFeverSchedule(nextState);
        }
    }

    private void CheckHeightGameOver()
    {
        SimpleItem[] allItems = FindObjectsByType<SimpleItem>(FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            if (item == null) continue;

            // 保持中のアイテムは除外（Kinematic）
            Rigidbody2D rb = item.GetComponent<Rigidbody2D>();
            if (rb == null || rb.bodyType == RigidbodyType2D.Kinematic) continue;

            // ラインを超えているか
            if (item.transform.position.y > gameOverLineY)
            {
                // 落下中は除外
                if (item.IsFalling) continue;

                // 静止しているか（速度がほぼ0）
                if (rb.linearVelocity.magnitude < 0.05f && !item.isMerged)
                {
                    Debug.Log($"[GameOver] Item settled above line: {item.name} at Y={item.transform.position.y}");
                    TriggerGameOver();
                    return;
                }
            }
        }
    }


    /// <summary>
    /// 指定したエフェクトを再生
    /// </summary>
    private void PlayEffect(GameObject prefab, Vector3 position)
    {
        if (prefab != null)
        {
            Instantiate(prefab, position, Quaternion.identity);
        }
    }

    /// <summary>
    /// FillingN生成時のエフェクト再生
    /// </summary>
    public void PlayFillingNEffect(Vector3 position)
    {
        PlayEffect(fillingNEffectPrefab, position);
        PlaySound(fillingNCreatedSound);
    }

    // ===== 音量設定 =====

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = bgmVolume;
        }
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
        PlayerPrefs.Save();
    }

    public void SetSEVolume(float volume)
    {
        seVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SE_VOLUME_KEY, seVolume);
        PlayerPrefs.Save();
    }

    public float GetBGMVolume()
    {
        return bgmVolume;
    }

    public float GetSEVolume()
    {
        return seVolume;
    }
}
