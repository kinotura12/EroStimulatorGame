// SimStateConfig.cs
// 状態ごとの閾値・係数設定（ScriptableObject）
// Inspectorで状態ごとにアセットを作って割り当てる

using UnityEngine;

[CreateAssetMenu(fileName = "SimStateConfig", menuName = "Sim/StateConfig")]
public class SimStateConfig : ScriptableObject
{
    [Header("=== 状態名（確認用） ===")]
    public SimState State;

    [Header("=== SharedBase からの上書き設定 ===")]
    public bool OverrideArousal;
    public bool OverrideResistance;
    public bool OverrideFatigue;
    public bool OverrideDrive;
    public bool OverrideDriveBias;
    public bool OverrideOrgasm;
    public bool OverrideTransition;
    public bool OverrideFrustration;
    public bool OverrideNeedMotion;
    public bool OverrideSub;

    [Header("=== 入力帯閾値 ===")]
    [Range(0f, 1f)] public float TolLow  = 0.3f;
    [Range(0f, 1f)] public float TolHigh = 0.7f;

    [Header("=== Arousal変化係数（＋増加 / －減少）===")]
    public float ArousalChangeStop   = -0.03f;  // 負=減少
    public float ArousalChangeBelow  =  0.02f;
    public float ArousalChangeWithin =  0.08f;
    public float ArousalChangeAbove  =  0.15f;

    [Header("=== Resistance変化係数（＋増加 / －減少）===")]
    public float ResistanceChangeStop   = -0.01f;  // 負=低下
    public float ResistanceChangeBelow  = -0.01f;  // 負=低下
    public float ResistanceChangeWithin = -0.03f;  // 負=低下
    public float ResistanceChangeAbove  =  0.02f;  // 正=上昇（後半状態では低値推奨）

    [Header("=== Fatigue変化係数（＋増加 / －減少）===")]
    public float FatigueChangeStop   = -0.03f;  // 負=回復
    public float FatigueChangeBelow  = -0.005f; // 負=微回復
    public float FatigueChangeWithin =  0.02f;
    public float FatigueChangeAbove  =  0.05f;
    public float FatigueMultiplier   =  1.0f;   // BrokenDownで2倍

    [Header("=== Drive変化係数（＋増加 / －減少）===")]
    public float DriveChangeStop     = -0.005f; // 負=減少
    public float DriveChangeBelow    =  0.02f;  // 一定時間継続後に上昇
    public float DriveChangeWithin   =  0.00f;  // 0=保持、負=減少
    public float DriveChangeAbove    =  0.02f;
    public float DriveChangeDelay    =  5.0f;   // 継続何秒後にDrive上昇開始

    [Header("=== DriveBias変化係数 ===")]
    public float DriveBiasShiftBelow  = -0.02f; // マイナス方向
    public float DriveBiasShiftAbove  =  0.02f; // プラス方向
    public float DriveBiasDecayWithin =  0.01f; // 0方向に収束
    public float DriveBiasDecayStop   =  0.005f;

    [Header("=== 射精閾値 ===")]
    [Range(0f, 1f)] public float OrgasmThreshold     = 1.0f;  // 通常は1.0
    public float OrgasmThresholdMultiplier            = 1.0f;  // BrokenDownで0.4倍
    public float OrgasmArousalResetTo                 = 0.25f; // 射精後ArousalをこのMax値にクランプ（高興奮状態では高めに設定で連続イキ）

    [Header("=== 遷移判定閾値 ===")]
    public float TransitionAboveDuration              = 5.0f;  // Above継続秒数
    public float TransitionBelowDuration              = 5.0f;  // Below継続秒数
    public float TransitionWithinDuration             = 8.0f;  // Within安定継続秒数
    public float TransitionDriveThreshold             = 0.7f;  // Drive閾値
    public float TransitionFatigueThreshold           = 0.9f;  // Fatigue閾値（エンド条件）
    public float TransitionBrokenDownArousalThreshold  = 0.6f;  // ⑦BrokenDown遷移に必要なArousal最低値
    public float TransitionSurrenderedArousalThreshold = 0.65f; // ⑤→⑥Surrendered突入に必要なArousal最低値
    public float TransitionOverriddenExitArousal       = 0.35f; // ③でArousalがこの値を下回ると②へ退場
    public float TransitionAcclimatingArousalThreshold  = 0.4f;  // ①→⑤Acclimating突入に必要なArousal最低値
    public float TransitionDefensiveResistanceThreshold = 0.8f;  // Resistanceがこの値を超えると②へ退場

    [Header("=== FrustratedCraving専用 ===")]
    public float FrustrationStackGain     = 0.05f;
    public float FrustrationStackThreshold= 0.6f;  // ⑦直行トリガー閾値
    public float FrustrationDriveThreshold= 0.5f;

    [Header("=== NeedMotion係数（＋増加 / －減少）===")]
    public float NeedMotionStopCalmChange     = -0.05f;  // Stop時（それ以外）の変化量（負=減少）
    public float NeedMotionStopArousedChange  =  0.03f;  // Stop時（Arousal高い or Sub有り）の変化量
    public float NeedMotionBelowChange        =  0.06f;
    public float NeedMotionWithinChange       = -0.04f;  // 正=増加、負=減少
    public float NeedMotionAboveChange        = -0.05f;  // 負=減少（全状態共通）
    public float NeedMotionArousalThreshold   =  0.5f;

    [Header("=== Sub効果係数（＋増加 / －減少）===")]
    public float SubADriveChange        =  0.02f;
    public float SubAArousalChange      =  0.04f;
    public float SubADriveBiasGain      =  0.03f;  // +方向
    public float SubAResistanceChange   = -0.01f;  // 負値=低下
    public float SubBDriveChange        =  0.02f;
    public float SubBArousalChange      =  0.02f;
    public float SubBDriveBiasGain      = -0.03f;  // -方向（負値で設定）
    public float SubBResistanceChange   =  0.01f;  // 正値=増加
    public float SubBothArousalChange   =  0.0f;   // 同時ON時のArousal変化量（0=無効）
}
