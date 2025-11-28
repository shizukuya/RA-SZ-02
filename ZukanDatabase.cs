using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ZukanDatabase", menuName = "Onigiri/Zukan Database")]
public class ZukanDatabase : ScriptableObject
{
    [SerializeField] private List<ZukanItemData> allItems;
    public List<ZukanItemData> AllItems => allItems;
}
