// ファイル名: SimpleItem.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// アイテムの種類（生成時のタイプ）
/// </summary>
public enum SimpleItemType
{
    WhiteRice,  // 白いおにぎり
    Nori,       // のり
    Filling,    // 具材
    Rock        // 岩
}

/// <summary>
/// おにぎりの状態（State Machine）
/// </summary>
public enum OnigiriState
{
    White,          // 白いおにぎり
    Nori,           // のり（単体）
    Filling,        // 具材（単体）
    WithNori,       // 白 + のり
    FillingO,       // 白 + 具材（具材-o）
    FillingN,       // 具材-o + のり / WithNori + 具材（具材-n）
    DoubleNori,     // のり + のり
    Rock,           // DoubleNori + のり（岩）
    Pickles         // 具材 + 具材（お漬物）
}

/// <summary>
/// 落下アイテム（状態マシン実装）
/// Collider は Is Trigger を OFF にしてください
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SimpleItem : MonoBehaviour
{
    [Header("アイテム設定")]
    [SerializeField] private SimpleItemType itemType;
    [SerializeField] private FillingData fillingData;  // Filling タイプの場合のみ使用
    [SerializeField] private GameObject rockPrefab;    // 岩のプレハブ（Noriタイプの場合に設定）

    [Header("ビジュアル（白おにぎり用スプライト）")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite normalSprite;           // White 状態
    [SerializeField] private Sprite withNoriSprite;         // WithNori 状態
    [SerializeField] private Sprite rockSprite;             // Rock 状態 (iwa.png)
    [SerializeField] private Sprite picklesSprite;          // Pickles 状態 (otsukemono.png)

    // 現在の状態
    private OnigiriState currentState;

    // 具材情報（状態遷移で引き継ぐ）
    private FillingData currentFilling = null;

    // 合体済みフラグ（重複合体防止）
    public bool isMerged = false;

    // 着地判定用
    private bool isFalling = false;
    private bool _hasLanded = false; // 内部フィールド名を変更

    [Header("判定調整")]
    [SerializeField] private float mergeCheckRadius = 0.2f; // 判定半径
    [SerializeField] private float mergeCheckInterval = 0.2f; // 判定間隔
    private float mergeCheckTimer = 0f;



    private Rigidbody2D rb;

    // プロパティ
    public SimpleItemType ItemType => itemType;
    public OnigiriState CurrentState => currentState;
    public FillingData CurrentFilling => currentFilling;
    public bool IsFalling => isFalling;
    public bool HasLanded => _hasLanded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // 初期状態を設定
        InitializeState();
        UpdateVisual();
    }

    /// <summary>
    /// アイテムタイプから初期状態を設定
    /// </summary>
    private void InitializeState()
    {
        switch (itemType)
        {
            case SimpleItemType.WhiteRice:
                currentState = OnigiriState.White;
                break;
            case SimpleItemType.Nori:
                currentState = OnigiriState.Nori;
                break;
            case SimpleItemType.Filling:
                currentState = OnigiriState.Filling;
                currentFilling = fillingData;
                break;
            case SimpleItemType.Rock:
                currentState = OnigiriState.Rock;
                break;
        }
    }

    /// <summary>
    /// 重力を有効にして落下開始
    /// </summary>
    public void StartFalling()
    {
        if (rb == null) return;

        rb.bodyType = RigidbodyType2D.Dynamic;

        // のりはゆっくり落下
        if (currentState == OnigiriState.Nori)
        {
            rb.gravityScale = 0.3f;
        }
        else
        {
            rb.gravityScale = 1.0f;
        }

        // 落下開始サウンド
        if (SimpleGameManager.Instance != null)
        {
            SimpleGameManager.Instance.PlayDropSound();
        }

        isFalling = true;
        _hasLanded = false;
    }

    /// <summary>
    /// 重力を無効化
    /// </summary>
    public void StopFalling()
    {
        if (rb == null) return;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
    }

    /// <summary>
    /// 物理衝突時の処理
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 操作中（落下前）は衝突処理を行わない
        if (!isFalling && !_hasLanded) return;

        // 着地判定
        if (isFalling && !_hasLanded)
        {
            _hasLanded = true;
            isFalling = false;

            if (SimpleGameManager.Instance != null)
            {
                if (currentState == OnigiriState.Nori)
                {
                    SimpleGameManager.Instance.PlayNoriFwaSound();
                }
                else
                {
                    SimpleGameManager.Instance.PlayDownSound();
                }
            }
        }

        // 合体済みの場合は処理しない
        if (isMerged) return;

        SimpleItem other = collision.gameObject.GetComponent<SimpleItem>();
        if (other == null) return;
        if (other.isMerged) return;

        TryMerge(other);
    }

    /// <summary>
    /// 合体処理（状態マシンに基づく）
    /// </summary>
    private void TryMerge(SimpleItem other)
    {
        // 相手が操作中（落下前）の場合は合体しない
        if (!other.IsFalling && !other.HasLanded) return;

        OnigiriState myState = currentState;
        OnigiriState otherState = other.CurrentState;

        // ===== White =====
        if (myState == OnigiriState.White)
        {
            // White + Filling → FillingO
            if (otherState == OnigiriState.Filling)
            {
                TransitionTo(OnigiriState.FillingO, other.CurrentFilling);
                PlayFillingSound(other.CurrentFilling);
                DestroyOther(other);
                return;
            }
            // White + Nori → WithNori
            if (otherState == OnigiriState.Nori)
            {
                TransitionTo(OnigiriState.WithNori, null);
                PlayNoriSound();
                DestroyOther(other);
                return;
            }
        }

        // ===== Filling（単体具材）=====
        if (myState == OnigiriState.Filling)
        {
            // Filling + White → FillingO（相手に吸収される）
            if (otherState == OnigiriState.White)
            {
                other.TransitionTo(OnigiriState.FillingO, currentFilling);
                PlayFillingSound(currentFilling);
                DestroySelf();
                return;
            }
            // Filling + WithNori → FillingN（相手に吸収される）
            if (otherState == OnigiriState.WithNori)
            {
                other.TransitionTo(OnigiriState.FillingN, currentFilling);
                PlayWrappingSound(currentFilling);
                CheckCompletion(other);
                DestroySelf();
                return;
            }
            // Filling + Filling → Pickles（お漬物）
            if (otherState == OnigiriState.Filling)
            {
                TransitionTo(OnigiriState.Pickles, null);
                PlayPicklesSound();
                DestroyOther(other);
                return;
            }
        }

        // ===== Nori（単体のり）=====
        if (myState == OnigiriState.Nori)
        {
            // Nori + White → WithNori（相手に吸収される）
            if (otherState == OnigiriState.White)
            {
                other.TransitionTo(OnigiriState.WithNori, null);
                PlayNoriSound();
                DestroySelf();
                return;
            }
            // Nori + FillingO → FillingN（相手に吸収される）
            if (otherState == OnigiriState.FillingO)
            {
                other.TransitionTo(OnigiriState.FillingN, other.CurrentFilling);
                PlayWrappingSound(other.CurrentFilling);
                CheckCompletion(other);
                DestroySelf();
                return;
            }
            // Nori + Nori → Rock Prefab
            if (otherState == OnigiriState.Nori)
            {
                if (rockPrefab != null)
                {
                    // 中間地点に生成
                    Vector3 spawnPos = (transform.position + other.transform.position) / 2f;
                    Instantiate(rockPrefab, spawnPos, Quaternion.identity);
                    
                    PlayRockSound();
                }
                
                DestroyOther(other);
                DestroySelf();
                return;
            }
        }

        // ===== Rock（岩）=====
        if (myState == OnigiriState.Rock)
        {
            // 岩は合体しない
            return;
        }

        // ===== Pickles（お漬物）=====
        if (myState == OnigiriState.Pickles)
        {
            // お漬物は合体しない（出荷でのみ消える）
            return;
        }

        // ===== WithNori =====
        if (myState == OnigiriState.WithNori)
        {
            // WithNori + Filling → FillingN
            if (otherState == OnigiriState.Filling)
            {
                TransitionTo(OnigiriState.FillingN, other.CurrentFilling);
                PlayWrappingSound(other.CurrentFilling);
                CheckCompletion(this);
                DestroyOther(other);
                return;
            }
        }

        // ===== FillingO =====
        if (myState == OnigiriState.FillingO)
        {
            // FillingO + Nori → FillingN
            if (otherState == OnigiriState.Nori)
            {
                TransitionTo(OnigiriState.FillingN, currentFilling);
                PlayWrappingSound(currentFilling);
                CheckCompletion(this);
                DestroyOther(other);
                return;
            }
        }

        // ===== FillingN =====
        if (myState == OnigiriState.FillingN)
        {
            // FillingN + FillingN → 消滅（同じ具材のみ）
            if (otherState == OnigiriState.FillingN)
            {
                // 同じ具材かチェック
                if (!IsSameFilling(currentFilling, other.CurrentFilling))
                {
                    // 違う具材なので合体しない
                    return;
                }

                // 連鎖判定：つながっている同じFillingNをすべて探す
                List<SimpleItem> connectedItems = new List<SimpleItem>();
                HashSet<SimpleItem> visited = new HashSet<SimpleItem>();
                
                // 自分と相手を起点に探索
                FindConnectedItems(this, connectedItems, visited);
                // 相手側も探索（念のため、すでにつながっているはずだが）
                FindConnectedItems(other, connectedItems, visited);

                // 最低でも自分と相手で2つはあるはず
                int matchCount = connectedItems.Count;
                if (matchCount < 2) matchCount = 2; // 安全策

                // 消滅サウンド（出荷音）
                PlayFinalCompletedSound();

                // スコア加算とコンボ
                if (SimpleGameManager.Instance != null)
                {
                    // 中間地点（全アイテムの平均位置）でエフェクト再生
                    Vector3 centerPos = Vector3.zero;
                    foreach(var item in connectedItems)
                    {
                        centerPos += item.transform.position;
                    }
                    centerPos /= matchCount;

                    SimpleGameManager.Instance.OnOnigiriMatched(currentFilling, centerPos, matchCount);
                }

                // 全て削除（0.5秒遅延）
                foreach(var item in connectedItems)
                {
                    if (item != null)
                    {
                        // コルーチンは自分自身で回すか、GameManagerで回すか...
                        // ここでは各アイテムでStartCoroutineを呼ぶと、Destroyされた瞬間に止まる可能性があるが、
                        // DestroyWithDelayの中でDestroy(gameObject)するまで待つので大丈夫なはず。
                        // ただし、thisがDestroyされるとコルーチンも止まる？
                        // いや、Destroy(gameObject)が呼ばれるまでは動く。
                        // しかし、他人のコルーチンを呼ぶのは変。
                        // 自分が代表して全員を消すか？
                        // DestroyWithDelayをstatic的につかうか、GameManagerに任せるのが安全だが、
                        // 既存のDestroyWithDelayを使うなら、各アイテムのメソッドを呼ぶ。
                        
                        // 注意: StartCoroutineはMonoBehaviourのメソッド。
                        // item.StartCoroutine(...) で呼べる。
                        item.StartCoroutine(item.DestroyWithDelay(null)); 
                        // 引数nullなのは、個別に消すため（相方はいない扱い、あるいはリストで処理済み）
                    }
                }
                return;
            }
        }
    }

    /// <summary>
    /// 再帰的に同じ種類のFillingNを探す
    /// </summary>
    private void FindConnectedItems(SimpleItem startItem, List<SimpleItem> results, HashSet<SimpleItem> visited)
    {
        if (startItem == null || visited.Contains(startItem)) return;
        
        // 状態チェック
        if (startItem.CurrentState != OnigiriState.FillingN) return;
        if (!IsSameFilling(startItem.CurrentFilling, currentFilling)) return; // currentFillingはTryMergeの呼び出し元（this）のもの

        visited.Add(startItem);
        results.Add(startItem);
        startItem.isMerged = true; // 重複合体防止のためフラグを立てておく

        // 周囲を探索
        // 判定半径は mergeCheckRadius を使用
        float radius = mergeCheckRadius;
        var col = startItem.GetComponent<CircleCollider2D>();
        if (col != null)
        {
            radius += col.radius * startItem.transform.localScale.x;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(startItem.transform.position, radius);
        foreach (var hit in hits)
        {
            SimpleItem neighbor = hit.GetComponent<SimpleItem>();
            if (neighbor != null && !visited.Contains(neighbor))
            {
                FindConnectedItems(neighbor, results, visited);
            }
        }
    }

    public System.Collections.IEnumerator DestroyWithDelay(SimpleItem other)
    {
        // 重複処理防止
        isMerged = true;
        if (other != null) other.isMerged = true;

        // 0.5秒待機
        yield return new WaitForSeconds(0.5f);

        if (other != null) Destroy(other.gameObject);
        Destroy(gameObject);
    }

    /// <summary>
    /// 状態遷移
    /// </summary>
    public void TransitionTo(OnigiriState newState, FillingData filling)
    {
        currentState = newState;
        if (filling != null)
        {
            currentFilling = filling;
        }
        UpdateVisual();
    }

    /// <summary>
    /// 同じ具材かどうか判定
    /// </summary>
    private bool IsSameFilling(FillingData a, FillingData b)
    {
        if (a == null || b == null) return false;
        return a == b || a.FillingName == b.FillingName;
    }

    /// <summary>
    /// より高いスコアの具材を返す
    /// </summary>
    private FillingData GetBetterFilling(FillingData a, FillingData b)
    {
        if (a == null) return b;
        if (b == null) return a;
        return a.Score >= b.Score ? a : b;
    }

    /// <summary>
    /// FillingN生成時の完成チェック（コンボ用）
    /// </summary>
    private void CheckCompletion(SimpleItem item)
    {
        // コンボ判定用にGameManagerに通知
        if (SimpleGameManager.Instance != null)
        {
            SimpleGameManager.Instance.OnFillingNCreated();
            SimpleGameManager.Instance.PlayFillingNEffect(item.transform.position);
        }
    }

    /// <summary>
    /// 相手を削除
    /// </summary>
    private void DestroyOther(SimpleItem other)
    {
        other.isMerged = true;
        Destroy(other.gameObject);
    }

    /// <summary>
    /// 自分を削除
    /// </summary>
    private void DestroySelf()
    {
        isMerged = true;
        Destroy(gameObject);
    }

    // ===== サウンド関連 =====
    private void PlayNoriSound()
    {
        if (SimpleGameManager.Instance != null)
        {
            SimpleGameManager.Instance.PlayNoriSound();
        }
    }

    private void PlayFillingSound(FillingData filling)
    {
        if (SimpleGameManager.Instance != null && filling != null)
        {
            SimpleGameManager.Instance.PlayFillingSound(filling.FillingSound);
        }
    }

    private void PlayWrappingSound(FillingData filling)
    {
        if (SimpleGameManager.Instance != null && filling != null)
        {
            SimpleGameManager.Instance.PlayWrappingSound(filling.WrappingSound);
        }
    }

    private void PlayCompletedSound()
    {
        if (SimpleGameManager.Instance != null)
        {
            SimpleGameManager.Instance.PlayCompleteSound();
        }
    }

    private void PlayPicklesSound()
    {
        if (SimpleGameManager.Instance != null)
        {
            SimpleGameManager.Instance.PlayPicklesSound();
        }
    }

    private void PlayFinalCompletedSound()
    {
        if (SimpleGameManager.Instance != null)
        {
            SimpleGameManager.Instance.PlayConsumeSound();
        }
    }

    private void Update()
    {
        if (isMerged) return;

        // 定期的に周囲のアイテムをチェックして、当たり判定漏れを防ぐ
        mergeCheckTimer += Time.deltaTime;
        if (mergeCheckTimer >= mergeCheckInterval)
        {
            mergeCheckTimer = 0f;
            CheckProximityMerge();
        }
    }



    /// <summary>
    /// 物理演算に頼らず、近くのアイテムを検知して合体を試みる
    /// </summary>
    private void CheckProximityMerge()
    {
        // 落下中または着地後のみ判定
        if (!isFalling && !_hasLanded) return;

        // 自分のコライダーサイズを考慮
        float radius = mergeCheckRadius;
        var col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            radius += col.radius * transform.localScale.x;
        }

        // 周囲のコライダーを取得
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            SimpleItem other = hit.GetComponent<SimpleItem>();
            if (other != null && !other.isMerged)
            {
                // 相手も落下中か着地後なら合体を試みる
                if (other.IsFalling || other.HasLanded)
                {
                    TryMerge(other);
                    if (isMerged) return; // 合体したら終了
                }
            }
        }
    }

    /// <summary>
    /// ビジュアルを更新
    /// </summary>
    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.color = Color.white;
        // transform.localScale = Vector3.one;

        switch (currentState)
        {
            case OnigiriState.White:
                if (normalSprite != null)
                    spriteRenderer.sprite = normalSprite;
                break;

            case OnigiriState.Nori:
                // のりは FillingData の Sprite を使わず、プレハブのスプライトをそのまま使用
                break;

            case OnigiriState.Filling:
                // 具材単体
                if (currentFilling != null && currentFilling.Sprite != null)
                    spriteRenderer.sprite = currentFilling.Sprite;
                break;

            case OnigiriState.WithNori:
                if (withNoriSprite != null)
                    spriteRenderer.sprite = withNoriSprite;
                break;

            case OnigiriState.FillingO:
                // 具材-o
                if (currentFilling != null && currentFilling.FillingOSprite != null)
                    spriteRenderer.sprite = currentFilling.FillingOSprite;
                break;

            case OnigiriState.FillingN:
                // 具材-n
                if (currentFilling != null && currentFilling.FillingNSprite != null)
                    spriteRenderer.sprite = currentFilling.FillingNSprite;
                    transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                break;

            case OnigiriState.DoubleNori:
                // のり2枚（少し暗くする）
                spriteRenderer.color = new Color(0.7f, 0.7f, 0.7f);
                break;

            case OnigiriState.Rock:
                // 岩
                if (rockSprite != null)
                {
                    spriteRenderer.sprite = rockSprite;
                    transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                }
                else
                {
                    // 画像がない場合は黒っぽくする
                    spriteRenderer.color = Color.gray;
                }
                break;

            case OnigiriState.Pickles:
                // お漬物
                if (picklesSprite != null)
                {
                    spriteRenderer.sprite = picklesSprite;
                    transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                }
                else
                {
                    // 画像がない場合は緑っぽくする
                    spriteRenderer.color = Color.green;
                }
                break;
        }
    }

    /// <summary>
    /// 具材データを外部から設定（Spawner用）
    /// </summary>
    public void SetFillingData(FillingData filling)
    {
        fillingData = filling;
        currentFilling = filling;
        UpdateVisual();
    }

    /// <summary>
    /// 岩を破壊する（外部呼び出し用）
    /// </summary>
    public void DestroyRock()
    {
        Debug.Log($"[DestroyRock] Called on {name}. CurrentState: {currentState}");
        if (currentState == OnigiriState.Rock)
        {
            // エフェクトや音を入れるならここ
            Debug.Log($"[DestroyRock] Destroying {name}");
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"[DestroyRock] {name} is NOT in Rock state! (State: {currentState})");
        }
    }

    /// <summary>
    /// お漬物を破壊する（外部呼び出し用）
    /// </summary>
    public void DestroyPickles()
    {
        Debug.Log($"[DestroyPickles] Called on {name}. CurrentState: {currentState}");
        if (currentState == OnigiriState.Pickles)
        {
            Debug.Log($"[DestroyPickles] Destroying {name}");
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"[DestroyPickles] {name} is NOT in Pickles state! (State: {currentState})");
        }
    }

    public void PlayRockSound()
    {
        if (SimpleGameManager.Instance != null)
        {
            SimpleGameManager.Instance.PlayRockSound();
        }
    }
}
