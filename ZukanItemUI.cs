using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ZukanItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image rarityBackground; // レア度に応じた背景色などを変える場合
    [SerializeField] private GameObject lockedOverlay; // 未獲得時のマスク

    public void Setup(ZukanItemData data, bool isUnlocked)
    {
        if (data == null) return;

        // 名前設定
        if (nameText != null)
        {
            nameText.text = isUnlocked ? data.DisplayName : "???";
        }

        // アイコン設定
        if (iconImage != null)
        {
            if (isUnlocked)
            {
                iconImage.sprite = data.Icon;
                iconImage.color = Color.white;
            }
            else
            {
                // 未獲得時はシルエットにするか、?画像にする
                // ここでは黒く塗りつぶす例
                iconImage.sprite = data.Icon; 
                iconImage.color = Color.black; 
            }
        }

        // ロック状態の表示
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked);
        }

        // レア度表示（背景色など）
        if (rarityBackground != null)
        {
            switch (data.Rarity)
            {
                case Rarity.Common:
                    rarityBackground.color = new Color(0.9f, 0.9f, 0.9f); // 白/グレー
                    break;
                case Rarity.Rare:
                    rarityBackground.color = new Color(0.8f, 0.9f, 1.0f); // 青白
                    break;
                case Rarity.SuperRare:
                    rarityBackground.color = new Color(1.0f, 0.9f, 0.5f); // 金
                    break;
                case Rarity.UltraRare:
                    rarityBackground.color = new Color(1.0f, 0.8f, 1.0f); // ピンク/虹
                    break;
            }
        }
    }
}
