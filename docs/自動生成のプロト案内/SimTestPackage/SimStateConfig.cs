// SimStateConfig.cs
// 状態ごとの閾値・係数設定（ScriptableObject）
// Inspectorで状態ごとにアセットを作って割り当てる

using UnityEngine;

[CreateAssetMenu(fileName = "SimStateConfig", menuName = "Sim/StateConfig")]
public class SimStateConfig : ScriptableObject
{
    [Header("=== 状態名（確認用） ===")]
    public SimState State;

    [Header("=== 入力帯閾値 ===")]
    [Range(0f, 1f)] public float TolLow  = 0.3f;
    [Range(0f, 1f)] public float TolHigh = 0.7f;

    [Header("=== Arousal変化係数 ===")]
    public float ArousalGainBelow  = 0.02f;
    public float ArousalGainWithin = 0.08f;
    public float ArousalGainAbove  = 0.15f;
    public float ArousalDecayStop  = 0.03f;

    [Header("=== Resistance変化係数 ===")]
    public float ResistanceDecayBelow  = 0.01f;
    public float ResistanceDecayWithin = 0.03f;
    public float ResistanceRiseAbove   = 0.02f; // 後半状態では抑制
    public float ResistanceDecayStop   = 0.01f;

    [Header("=== Fatigue変化係数 ===")]
    public float FatigueGainWithin = 0.02f;
    public float FatigueGainAbove  = 0.05f;
    public float FatigueDecayStop  = 0.01f;
    public float FatigueMultiplier = 1.0f;  // BrokenDownで2倍

    [Header("=== Drive変化係数 ===")]
    public float DriveGainBelow       = 0.0f;  // 一定時間継続後に上昇
    public float DriveGainAbove       = 0.0f;
    public float DriveDecayWithin     = 0.01f;
    public float DriveDecayStop       = 0.005f;
    public float DriveTimeBeforeGain  = 5.0f;  // 継続何秒後にDrive上昇開始

    [Header("=== DriveBias変化係数 ===")]
    public float DriveBiasShiftBelow  = -0.02f; // マイナス方向
    public float DriveBiasShiftAbove  =  0.02f; // プラス方向
    public float DriveBiasDecayWithin =  0.01f; // 0方向に収束
    public float DriveBiasDecayStop   =  0.005f;

    [Header("=== 射精閾値 ===")]
    [Range(0f, 1f)] public float OrgasmThreshold     = 1.0f; // 通常は1.0
    public float OrgasmThresholdMultiplier            = 1.0f; // BrokenDownで0.4倍

    [Header("=== 遷移判定閾値 ===")]
    public float TransitionAboveDuration   = 5.0f;  // Above継続秒数
    public float TransitionBelowDuration   = 5.0f;  // Below継続秒数
    public float TransitionWithinDuration  = 8.0f;  // Within安定継続秒数
    public float TransitionDriveThreshold  = 0.7f;  // Drive閾値
    public float TransitionFatigueThreshold= 0.9f;  // Fatigue閾値（エンド条件）

    [Header("=== FrustratedCraving専用 ===")]
    public float FrustrationStackGain     = 0.05f;
    public float FrustrationStackThreshold= 0.6f;  // ⑦直行トリガー閾値
    public float FrustrationDriveThreshold= 0.5f;
    public float FrustrationBandFlipTime  = 2.0f;  // 反転継続秒数
}
