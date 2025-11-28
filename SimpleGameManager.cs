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
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject wall;
    [SerializeField] private Image nextItemImage; // 次のアイテムを表示するImage

    [Header("エフェクト")]
    [SerializeField] private GameObject matchEffectPrefab;    // 出荷（消滅）時のエフェクト
    [SerializeField] private GameObject comboEffectPrefab;    // コンボ時のエフェクト
    [SerializeField] private GameObject fillingNEffectPrefab; // FillingN生成時のエフェクト

    [Header("スコア設定")]
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

    public bool IsGameOver => isGameOver;
    public int ComboCount => comboCount;

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
        PlaySound(gameoverSound);
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

        Debug.Log("ゲームオーバー！");

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

        gameOverTimer += Time.deltaTime;
        if (gameOverTimer >= gameOverCheckInterval)
        {
            gameOverTimer = 0f;
            CheckHeightGameOver();
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
