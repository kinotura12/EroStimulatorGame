// SimParameters.cs
// 内部パラメータのデータクラス（すべて 0.0～1.0、DriveBiasのみ -1.0～1.0）

using UnityEngine;

[System.Serializable]
public class SimParameters
{
    [Header("=== 主要パラメータ ===")]
    [Range(0f, 1f)]  public float Arousal;         // 快感の反応量
    [Range(0f, 1f)]  public float Resistance;      // 抵抗感（高=拒絶、低=受容）
    [Range(0f, 1f)]  public float Fatigue;         // 消耗・疲弊
    [Range(0f, 1f)]  public float Drive;           // 理性崩壊の総量

    [Header("=== 崩壊の質 ===")]
    [Range(-1f, 1f)] public float DriveBias;       // -=トロトロ寄り、+=アヘ顔寄り

    [Header("=== 動き欲求 ===")]
    [Range(0f, 1f)]  public float NeedMotion;      // 腰の反応・求める動き（蓄積）

    [Header("=== FrustratedCraving専用 ===")]
    [Range(0f, 1f)]  public float FrustrationStack; // お預け度合い

    [Header("=== BrokenDown ===")]
    public BrokenDownMode BrokenDownMode;            // 突入時にロックされるモード

    public void Reset()
    {
        Arousal          = 0f;
        Resistance       = 0.5f;
        Fatigue          = 0f;
        Drive            = 0f;
        DriveBias        = 0f;
        NeedMotion       = 0f;
        FrustrationStack = 0f;
        BrokenDownMode   = BrokenDownMode.None;
    }
}
