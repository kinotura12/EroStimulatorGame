// SimResolvedConfig.cs
// 実行時に SharedBase と StateConfig をマージした有効設定

public class SimResolvedConfig
{
    public float TolLow;
    public float TolHigh;

    public float ArousalChangeStop;
    public float ArousalChangeBelow;
    public float ArousalChangeWithin;
    public float ArousalChangeAbove;

    public float ResistanceChangeStop;
    public float ResistanceChangeBelow;
    public float ResistanceChangeWithin;
    public float ResistanceChangeAbove;

    public float FatigueChangeStop;
    public float FatigueChangeBelow;
    public float FatigueChangeWithin;
    public float FatigueChangeAbove;
    public float FatigueMultiplier;

    public float DriveChangeStop;
    public float DriveChangeBelow;
    public float DriveChangeWithin;
    public float DriveChangeAbove;
    public float DriveChangeDelay;
    public float DriveArousalBoostFactor;

    public float DriveBiasShiftBelow;
    public float DriveBiasShiftAbove;
    public float DriveBiasDecayWithin;
    public float DriveBiasDecayStop;

    public float OrgasmThreshold;
    public float OrgasmThresholdMultiplier;
    public float WithholdDuration;  // Arousal閾値到達後、射精まで何秒我慢できるか
    public float EdgeDecayRate;     // エッジモード外でのEdgeTension自然減衰速度（/秒）

    public float EdgeNeutralIntensity;  // この強度を境に増加↑/減衰↓
    public float EdgeFillRate;          // 増加速度乗数
    public float EdgeFillCurve;         // 増加カーブ指数（1=線形、0.5=√曲線）
    public float EdgeDrainRate;         // エッジモード中・中立以下での減衰速度（/秒）
    public float EdgeDwellScaleMax;     // 何秒滞在でOrgasmScale=1.0になるか（SharedConfig固定）
    public float EdgePeakHoldDuration;  // EdgeTension=1.0 を何秒維持したら射精するか
    public float EdgeResistanceFactor;  // Resistanceが射精欲蓄積を妨げる強さ（OverrideEdgeで状態別上書き可）
    public float EdgeDriveBoostFactor; // Driveが射精欲蓄積を促進する強さ（OverrideEdgeで状態別上書き可）

    public float TransitionAboveDuration;
    public float TransitionBelowDuration;
    public float TransitionWithinDuration;
    public float TransitionDriveThreshold;
    public float TransitionFatigueThreshold;
    public float TransitionBrokenDownArousalThreshold;
    public float TransitionSurrenderedArousalThreshold;
    public float TransitionOverriddenExitArousal;
    public float TransitionAcclimatingArousalThreshold;
    public float TransitionDefensiveResistanceThreshold;

    public float FrustrationStackGain;
    public float FrustrationStackThreshold;
    public float FrustrationDriveThreshold;

    // === 射精効果（SharedConfig固定、状態上書き不可） ===
    public float OrgasmFatigueGain;          // 射精時Fatigue即時増加量
    public float OrgasmArousalResetTo;       // 射精後ArousalをこのMax値にクランプ
    public float OrgasmResistanceBaseDrop;   // 射精時Resistance基礎低下量（SharedConfig固定）
    public float OrgasmResistanceDropCoefficient; // 射精時Resistance低下の状態別係数（StateConfig固定）

    // === 累積オーガズム（SharedConfig固定） ===
    public float OrgasmCumulativeGain;         // 射精1回の加算量
    public float OrgasmCumulativeDecayRate;   // 毎秒の減衰量
    public float OrgasmCumulativeBonusScale;  // CumulativeOrgasmがOrgasmScaleに加算するボーナス乗数

    // === エンド条件 射精カウント（SharedConfig固定） ===
    public int EndAOrgasmCount;   // ③ End_A に必要な射精回数
    public int EndBOrgasmCount;   // ⑥ End_B に必要な射精回数
    public int EndCOrgasmCount;   // ⑦ End_C に必要な射精回数

    // === NeedMotion係数（＋増加 / －減少）===
    public float NeedMotionStopCalmChange;      // Stop時（それ以外）の変化量（負=減少）
    public float NeedMotionStopArousedChange;   // Stop時（Arousal高い or Sub有り）の変化量
    public float NeedMotionBelowChange;         // Below帯の変化量
    public float NeedMotionWithinChange;        // Within帯の変化量（正=増加、負=減少）
    public float NeedMotionAboveChange;         // Above帯の変化量（負=減少、全状態共通）
    public float NeedMotionArousalThreshold;    // Stop時にNeedMotion増加するArousal閾値

    // === Sub効果係数（＋増加 / －減少）===
    public float SubADriveChange;
    public float SubAArousalChange;
    public float SubADriveBiasGain;
    public float SubAResistanceChange;
    public float SubBDriveChange;
    public float SubBArousalChange;
    public float SubBDriveBiasGain;
    public float SubBResistanceChange;
    public float SubBothArousalChange;
}
