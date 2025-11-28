using UnityEngine;
using System.Collections.Generic;

public class ZukanPanelController : MonoBehaviour
{
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform contentParent; // ScrollViewのContent

    private void OnEnable()
    {
        RefreshZukan();
    }

    public void RefreshZukan()
    {
        // 既存のリストをクリア
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        /*
        if (SimpleGameManager.Instance == null) return;

        ZukanDatabase db = SimpleGameManager.Instance.GetZukanDatabase();
        if (db == null || db.AllItems == null) return;

        foreach (var item in db.AllItems)
        {
            if (item == null) continue;

            GameObject obj = Instantiate(itemPrefab, contentParent);
            ZukanItemUI ui = obj.GetComponent<ZukanItemUI>();
            if (ui != null)
            {
                bool isUnlocked = SimpleGameManager.Instance.IsItemUnlocked(item.ID);
                ui.Setup(item, isUnlocked);
            }
        }
        */
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}
