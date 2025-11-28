// ファイル名: SimpleTouchController.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// シンプルなタッチ操作（タップした位置に即座に落下）
/// </summary>
public class SimpleTouchController : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private float minX = -2.5f;
    [SerializeField] private float maxX = 2.5f;
    [SerializeField] private float spawnY = 3.0f;  // アイテム生成時のY座標

    [Header("参照")]
    [SerializeField] private Camera mainCamera;

    private SimpleItem currentItem;

    // ゲームオーバーかどうか
    private bool isGameOver = false;

    public SimpleItem CurrentItem => currentItem;

    private void Awake()
    {
        // Inspectorの設定を上書きして強制的に3.5にする
        spawnY = 3.5f;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        if (isGameOver) return;

        HandleInput();
    }

    private void HandleInput()
    {
        // ゲーム停止中は操作を受け付けない
        if (Time.timeScale == 0f) return;

        // タッチ入力
        if (Touch.activeTouches.Count > 0)
        {
            Touch touch = Touch.activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                bool isUI = IsPointerOverUI(touch.screenPosition);
                
                // UIの上なら無視
                if (isUI) return;

                OnTap(touch.screenPosition);
            }
        }
        // マウス入力（エディタ用）
        else if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                bool isUI = IsPointerOverUI(Mouse.current.position.ReadValue());
                
                // UIの上なら無視
                if (isUI) return;

                OnTap(Mouse.current.position.ReadValue());
            }
        }
    }

    /// <summary>
    /// 指定したスクリーン座標にUIがあるか判定（手動レイキャスト）
    /// </summary>
    private bool IsPointerOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }

    /// <summary>
    /// タップ時の処理：タップ位置に移動して即座に落下
    /// </summary>
    private void OnTap(Vector2 screenPos)
    {
        if (currentItem == null)
        {
            // ゲームオーバー時などに発生する可能性があるため、ログは出さないかWarningにする
            // Debug.LogWarning("OnTap: currentItem が null です（操作無効）");
            return;
        }

        // スクリーン座標をワールド座標に変換
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        float clampedX = Mathf.Clamp(worldPos.x, minX, maxX);

        // タップ位置に移動
        currentItem.transform.position = new Vector3(clampedX, spawnY, 0);

        // 落下開始
        currentItem.StartFalling();

        // 次の生成のためにタイプを保持
        SimpleItemType droppedItemType = currentItem.ItemType;

        // 操作中のアイテムをクリア（先にクリア）
        currentItem = null;

        // 次のアイテム生成を予約
        SimpleSpawner spawner = FindFirstObjectByType<SimpleSpawner>();
        if (spawner != null)
        {
            // 落下させたアイテムのタイプを渡す（遅延調整用）
            spawner.SpawnNextDelayed(droppedItemType);
            Debug.Log("SpawnNextDelayed 呼び出し完了");
        }
        else
        {
            Debug.LogError("Spawner が見つかりません");
        }
    }

    public void SetItem(SimpleItem item)
    {
        if (item == null)
        {
            Debug.LogError("SetItem: item が null です");
            return;
        }

        currentItem = item;
        currentItem.transform.position = new Vector3(0, spawnY, 0);
        currentItem.StopFalling();
        Debug.Log($"SetItem 完了: {item.name}, currentItem = {currentItem}");
    }

    /// <summary>
    /// ゲームオーバー時に呼ばれる
    /// </summary>
    public void OnGameOver()
    {
        isGameOver = true;
        enabled = false;
    }
}
