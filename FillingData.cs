// ファイル名: FillingData.cs
using UnityEngine;

/// <summary>
/// 具材データ（拡張可能な ScriptableObject）
/// </summary>
[CreateAssetMenu(fileName = "New Filling", menuName = "Onigiri/Filling Data")]
public class FillingData : ScriptableObject
{
    [Header("具材情報")]
    [SerializeField] private string fillingName;  // 具材名（梅、鮭など）

    [Header("スプライト")]
    [SerializeField] private Sprite sprite;           // 具材の画像（単体）
    [SerializeField] private Sprite fillingOSprite;   // 具材-o（白+具材）の画像
    [SerializeField] private Sprite fillingNSprite;   // 具材-n（具材-o+のり / 白+のり+具材）の画像

    [Header("サウンド")]
    [SerializeField] private AudioClip fillingSound;  // 具材合体時の音
    [SerializeField] private AudioClip wrappingSound; // 巻く時の音

    [Header("スコア")]
    [SerializeField] private int score;               // 基本スコア

    public string FillingName => fillingName;
    public Sprite Sprite => sprite;
    public Sprite FillingOSprite => fillingOSprite;
    public Sprite FillingNSprite => fillingNSprite;
    public AudioClip FillingSound => fillingSound;
    public AudioClip WrappingSound => wrappingSound;
    public int Score => score;
}

public enum Rarity
{
    Common,
    Rare,
    SuperRare,
    UltraRare
}
