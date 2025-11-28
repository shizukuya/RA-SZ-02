using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ShipmentPopup : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private GameObject panelRoot; // ポップアップ全体のルート（表示/非表示用）

    [Header("Settings")]
    [SerializeField] private float displayDuration = 1.5f; // 自動で消えるまでの時間

    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void Show(ZukanItemData data)
    {
        Debug.Log($"[ShipmentPopup] Show: {data?.DisplayName}");
        if (data == null) return;

        // UI更新
        if (iconImage != null) iconImage.sprite = data.Icon;
        if (nameText != null) nameText.text = data.DisplayName;
        
        if (rarityText != null)
        {
            rarityText.text = data.Rarity.ToString();
            // レア度ごとの色分けなどをここで行っても良い
        }

        // 表示
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        // 既存の非表示コルーチンがあれば止める
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        // 一定時間後に非表示
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        Close();
    }

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }
}
