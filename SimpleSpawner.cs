// ファイル名: SimpleSpawner.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// アイテム生成器（同じアイテムは最大2回まで連続）
/// </summary>
public class SimpleSpawner : MonoBehaviour
{
    [Header("プレハブ")]
    [SerializeField] private GameObject whiteRicePrefab;  // 白いおにぎり
    [SerializeField] private GameObject noriPrefab;       // のり
    [SerializeField] private GameObject fillingPrefab;    // 具材（ベース）

    [Header("具材データ")]
    [SerializeField] private FillingData[] availableFillings;  // 使用可能な具材リスト

    [Header("出現確率")]
    [SerializeField] private float whiteRiceChance = 40f;
    [SerializeField] private float noriChance = 30f;
    [SerializeField] private float fillingChance = 30f;

    [Header("参照")]
    [SerializeField] private SimpleTouchController touchController;

    [Header("生成設定")]
    [SerializeField] private float spawnDelay = 1.5f;
    [SerializeField] private float noriSpawnDelay = 2.0f; // のり用の遅延
    [SerializeField] private int maxConsecutive = 2;  // 同じアイテムの最大連続回数

    // 連続生成トラッキング
    private SimpleItemType lastItemType;
    private int consecutiveCount = 0;

    // 次のアイテム情報
    private SimpleItemType nextItemType;
    private FillingData nextFillingData; // Fillingの場合の具材データ

    private void Start()
    {
        // 初回の「次」を決定
        PrepareNextItem();
        SpawnNext();
    }

    public void SpawnNextDelayed(SimpleItemType? lastDroppedItemType = null)
    {
        if (!enabled || !gameObject.activeInHierarchy) return;
        StartCoroutine(SpawnNextCoroutine(lastDroppedItemType));
    }

    private IEnumerator SpawnNextCoroutine(SimpleItemType? lastDroppedItemType)
    {
        float delay = spawnDelay;
        if (lastDroppedItemType.HasValue && lastDroppedItemType.Value == SimpleItemType.Nori)
        {
            delay = noriSpawnDelay;
            Debug.Log($"のりが落ちたため、生成を {delay}秒 待ちます");
        }

        yield return new WaitForSeconds(delay);

        if (SimpleGameManager.Instance != null)
        {
            SimpleGameManager.Instance.ResetCombo();
        }

        SpawnNext();
    }

    public void SpawnNext()
    {
        Debug.Log("[SimpleSpawner] SpawnNext called");

        // ゲームオーバーなら生成しない
        if (SimpleGameManager.Instance != null && SimpleGameManager.Instance.IsGameOver)
        {
            Debug.Log("[SimpleSpawner] SpawnNext aborted: Game Over is true");
            return;
        }
        if (touchController == null)
        {
            Debug.LogError("[SimpleSpawner] SpawnNext aborted: touchController is null");
            return;
        }
        if (touchController.CurrentItem != null)
        {
            Debug.LogWarning($"[SimpleSpawner] SpawnNext aborted: touchController.CurrentItem is not null ({touchController.CurrentItem.name})");
            return;
        }

        // 準備しておいた「次」を使用
        SimpleItemType selectedType = nextItemType;
        GameObject selectedPrefab = GetPrefabByType(selectedType);

        if (selectedPrefab == null)
        {
            Debug.LogError($"[SimpleSpawner] SpawnNext aborted: Prefab is null for type {selectedType}");
            return;
        }

        GameObject obj = Instantiate(selectedPrefab, transform.position, Quaternion.identity);
        SimpleItem item = obj.GetComponent<SimpleItem>();

        if (item != null)
        {
            // 具材タイプの場合、準備しておいた具材データを設定
            if (item.ItemType == SimpleItemType.Filling && nextFillingData != null)
            {
                item.SetFillingData(nextFillingData);
            }

            // 連続カウント更新
            UpdateConsecutiveCount(selectedType);

            // ゲームオーバー判定はGameManagerの高さチェックに移行したため削除
            // StartCoroutine(CheckGameOverCollision(item));

            touchController.SetItem(item);
        }

        // 次のアイテムを準備（UI更新含む）
        PrepareNextItem();
    }

    /// <summary>
    /// 次のアイテムを決定し、UIを更新する
    /// </summary>
    private void PrepareNextItem()
    {
        // 次のタイプを決定
        nextItemType = SelectItemType();
        nextFillingData = null;

        // 具材の場合は具材データも決定
        if (nextItemType == SimpleItemType.Filling && availableFillings.Length > 0)
        {
            nextFillingData = availableFillings[Random.Range(0, availableFillings.Length)];
        }

        // UI更新
        UpdateNextItemUI();
    }

    /// <summary>
    /// 次のアイテム画像をGameManagerに通知
    /// </summary>
    private void UpdateNextItemUI()
    {
        if (SimpleGameManager.Instance == null) return;

        Sprite displaySprite = null;

        switch (nextItemType)
        {
            case SimpleItemType.WhiteRice:
                if (whiteRicePrefab != null)
                {
                    var renderer = whiteRicePrefab.GetComponent<SpriteRenderer>();
                    if (renderer != null) displaySprite = renderer.sprite;
                }
                break;

            case SimpleItemType.Nori:
                if (noriPrefab != null)
                {
                    var renderer = noriPrefab.GetComponent<SpriteRenderer>();
                    if (renderer != null) displaySprite = renderer.sprite;
                }
                break;

            case SimpleItemType.Filling:
                if (nextFillingData != null)
                {
                    displaySprite = nextFillingData.Sprite;
                }
                break;
        }

        SimpleGameManager.Instance.UpdateNextItemUI(displaySprite);
    }

    /// <summary>
    /// アイテムタイプを選択（同じタイプはmaxConsecutive回まで）
    /// </summary>
    private SimpleItemType SelectItemType()
    {
        // 連続回数が上限に達している場合、別のタイプを強制選択
        if (consecutiveCount >= maxConsecutive)
        {
            return SelectDifferentType(lastItemType);
        }

        // 通常のランダム選択
        float total = whiteRiceChance + noriChance + fillingChance;
        float random = Random.Range(0f, total);

        if (random < whiteRiceChance)
        {
            return SimpleItemType.WhiteRice;
        }
        else if (random < whiteRiceChance + noriChance)
        {
            return SimpleItemType.Nori;
        }
        else
        {
            return SimpleItemType.Filling;
        }
    }

    /// <summary>
    /// 指定タイプ以外をランダムに選択
    /// </summary>
    private SimpleItemType SelectDifferentType(SimpleItemType excludeType)
    {
        // 除外タイプ以外の確率を計算
        float total = 0f;

        if (excludeType != SimpleItemType.WhiteRice) total += whiteRiceChance;
        if (excludeType != SimpleItemType.Nori) total += noriChance;
        if (excludeType != SimpleItemType.Filling) total += fillingChance;

        float random = Random.Range(0f, total);
        float current = 0f;

        if (excludeType != SimpleItemType.WhiteRice)
        {
            current += whiteRiceChance;
            if (random < current) return SimpleItemType.WhiteRice;
        }

        if (excludeType != SimpleItemType.Nori)
        {
            current += noriChance;
            if (random < current) return SimpleItemType.Nori;
        }

        return SimpleItemType.Filling;
    }

    /// <summary>
    /// 連続カウントを更新
    /// </summary>
    private void UpdateConsecutiveCount(SimpleItemType newType)
    {
        if (newType == lastItemType)
        {
            consecutiveCount++;
        }
        else
        {
            consecutiveCount = 1;
            lastItemType = newType;
        }
    }

    /// <summary>
    /// タイプからプレハブを取得
    /// </summary>
    private GameObject GetPrefabByType(SimpleItemType type)
    {
        switch (type)
        {
            case SimpleItemType.WhiteRice:
                return whiteRicePrefab;
            case SimpleItemType.Nori:
                return noriPrefab;
            case SimpleItemType.Filling:
                return fillingPrefab;
            default:
                return null;
        }
    }


}
