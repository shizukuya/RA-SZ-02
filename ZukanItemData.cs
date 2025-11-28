using UnityEngine;

[CreateAssetMenu(fileName = "ZukanItemData", menuName = "Onigiri/Zukan Item Data")]
public class ZukanItemData : ScriptableObject
{
    [SerializeField] private int id;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private Rarity rarity;
    [TextArea]
    [SerializeField] private string description;

    public int ID => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public Rarity Rarity => rarity;
    public string Description => description;
}
