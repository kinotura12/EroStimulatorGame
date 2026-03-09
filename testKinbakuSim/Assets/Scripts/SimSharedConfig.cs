// SimSharedConfig.cs
// 全状態で共有したい基礎係数

using UnityEngine;

[CreateAssetMenu(fileName = "SimSharedConfig", menuName = "Sim/SharedConfig")]
public class SimSharedConfig : ScriptableObject
{
    [Header("=== Arousal変化係数（＋増加 / －減少）===")]
    public float ArousalChangeStop   = -0.03f;  // 負=減少
    public float ArousalChangeBelow  =  0.02f;
    public float ArousalChangeWithin =  0.08f;
    public float ArousalChangeAbove  =  0.15f;

    [Header("=== Resistance変化係数（＋増加 / －減少）===")]
    public float ResistanceChangeStop   = -0.01f;  // 負=低下
    public float ResistanceChangeBelow  = -0.01f;  // 負=低下
    public float ResistanceChangeWithin = -0.03f;  // 負=低下
    public float ResistanceChangeAbove  =  0.02f;  // 正=上昇

    [Header("=== Fatigue変化係数（＋増加 / －減少）===")]
    public float FatigueChangeStop   = -0.03f;  // 負=回復
    public float FatigueChangeBelow  = -0.005f; // 負=微回復
    public float FatigueChangeWithin =  0.02f;
    public float FatigueChangeAbove  =  0.05f;
    public float FatigueMultiplier   =  1.0f;

    [Header("=== Drive変化係数（＋増加 / －減少）===")]
    public float DriveChangeStop     = -0.005f; // 負=減少
    public float DriveChangeBelow    =  0.02f;
    public float DriveChangeWithin   =  0.00f;  // 0=保持、負=減少
    public float DriveChangeAbove    =  0.02f;
    public float DriveChangeDelay    =  5.0f;   // 継続何秒後にDrive上昇開始
    public float DriveArousalBoostFactor = 0.5f; // DriveがArousal上昇率を増幅する係数（0=無効, 1=Drive満タン時に2倍）

    [Header("=== DriveBias変化係数 ===")]
    public float DriveBiasShiftBelow  = -0.02f;
    public float DriveBiasShiftAbove  =  0.02f;
    public float DriveBiasDecayWithin =  0.01f;
    public float DriveBiasDecayStop   =  0.005f;

    [Header("=== 遷移判定閾値 ===")]
    public float TransitionAboveDuration               = 5.0f;
    public float TransitionBelowDuration               = 5.0f;
    public float TransitionWithinDuration              = 8.0f;
    public float TransitionDriveThreshold              = 0.7f;
    public float TransitionFatigueThreshold            = 0.9f;
    public float TransitionBrokenDownArousalThreshold  = 0.6f;  // ⑦BrokenDown遷移に必要なArousal最低値
    public float TransitionSurrenderedArousalThreshold = 0.65f; // ⑤→⑥Surrendered突入に必要なArousal最低値
    public float TransitionOverriddenExitArousal       = 0.35f; // ③でArousalがこの値を下回ると②へ退場
    public float TransitionAcclimatingArousalThreshold = 0.4f;  // ①→⑤Acclimating突入に必要なArousal最低値
    public float TransitionDefensiveResistanceThreshold = 0.8f; // Resistanceがこの値を超えると②へ退場

    [Header("=== FrustratedCraving専用 ===")]
    public float FrustrationStackGain      = 0.05f;
    public float FrustrationStackThreshold = 0.6f;
    public float FrustrationDriveThreshold = 0.5f;

    [Header("=== 射精管理：エッジモード突入条件（OverrideOrgasm で状態別上書き可）===")]
    [Range(0f, 1f)]
    public float OrgasmThreshold           = 1.0f;  // Arousal がこの値以上でエッジモード突入
    public float OrgasmThresholdMultiplier = 1.0f;  // 閾値の乗数（BrokenDown では 0.4 倍など）

    [Header("=== 射精管理：EdgeTension 増加・判定（OverrideEdge で状態別上書き可）===")]
    public float EdgeNeutralIntensity  = 0.2f;  // この入力強度を境に増加↑ / 減衰↓
    public float EdgeFillRate          = 1.0f;  // 増加速度の乗数
    public float WithholdDuration      = 3.0f;  // EdgeTension が 1.0 になるまでの基準秒数（短いほど早く満タン）
    public float EdgeDecayRate         = 0.5f;  // エッジモード外での自然減衰（/秒）★状態上書き不可
    public float EdgePeakHoldDuration  = 2.0f;  // EdgeTension=1.0 を何秒維持したら射精するか
    public float EdgeResistanceFactor  = 0.8f;  // Resistanceが射精欲蓄積を妨げる強さ（0=妨げなし、1=最大抑制）
    public float EdgeDriveBoostFactor = 0.5f;  // Driveが射精欲蓄積を促進する強さ（0=促進なし、1=Drive分だけ倍増）

    [Header("=== 射精管理：射精時効果（OverrideOrgasm で状態別上書き可）===")]
    public float OrgasmArousalResetTo    = 0.25f;  // 射精後 Arousal をこの値以下にクランプ（高いと連続イキしやすい）
    public float OrgasmFatigueGain       = 0.15f;  // 射精時 Fatigue 即時増加量
    public float OrgasmResistanceBaseDrop = 0.3f;  // 射精時 Resistance 基礎低下量（状態別係数で乗算される）
    // --- 実際の低下量: BaseDrop × OrgasmResistanceDropCoefficient (各StateConfigに設定) ---
    // ① Guarded      coeff=1.0  → 0.30
    // ② Defensive    coeff=1.0  → 0.30  ※Above刺激でResiが上がりやすいため係数大
    // ③ Overridden   coeff=0.8  → 0.24
    // ④ Frustrated   coeff=1.2  → 0.36
    // ⑤ Acclimating  coeff=1.2  → 0.36
    // ⑥ Surrendered  coeff=1.3  → 0.39
    // ⑦ BrokenDown   coeff=1.3  → 0.39
    // -----------------------------------------------------------------------

    [HideInInspector] public float EdgeFillCurve         = 0.5f;
    [HideInInspector] public float EdgeDrainRate         = 0.5f;
    [HideInInspector] public float EdgeDwellScaleMax     = 10.0f;
    [HideInInspector] public float OrgasmCumulativeGain       = 0.4f;
    [HideInInspector] public float OrgasmCumulativeDecayRate  = 0.03f;
    [HideInInspector] public float OrgasmCumulativeBonusScale = 0.5f;

    [Header("=== エンド条件 射精カウント ===")]
    public int EndAOrgasmCount = 2;   // ③ Overridden で何回射精でEnd_A
    public int EndBOrgasmCount = 2;   // ⑥ Surrendered で何回射精でEnd_B
    public int EndCOrgasmCount = 3;   // ⑦ BrokenDown で何回射精でEnd_C

    [Header("=== NeedMotion係数（＋増加 / －減少）===")]
    public float NeedMotionStopCalmChange     = -0.05f;  // Stop時（それ以外）の変化量（負=減少）
    public float NeedMotionStopArousedChange  =  0.03f;  // Stop時（Arousal高い or Sub有り）の変化量
    public float NeedMotionBelowChange        =  0.06f;  // Below帯の変化量
    public float NeedMotionWithinChange       = -0.04f;  // Within帯の変化量（正=増加、負=減少）
    public float NeedMotionAboveChange        = -0.05f;  // Above帯の変化量（負=減少、全状態共通）
    public float NeedMotionArousalThreshold   =  0.5f;   // Stop時にNeedMotion増加するArousal閾値

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

#if UNITY_EDITOR
    // Inspector で値を変更したとき、Override=false の全 SimStateConfig に自動同期
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            var guids = UnityEditor.AssetDatabase.FindAssets("t:SimStateConfig");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var cfg  = UnityEditor.AssetDatabase.LoadAssetAtPath<SimStateConfig>(path);
                if (cfg == null) continue;
                SyncToStateConfig(cfg);
                UnityEditor.EditorUtility.SetDirty(cfg);
            }
            UnityEditor.AssetDatabase.SaveAssets();
        };
    }

    void SyncToStateConfig(SimStateConfig dst)
    {
        if (!dst.OverrideArousal)
        {
            dst.ArousalChangeStop   = ArousalChangeStop;
            dst.ArousalChangeBelow  = ArousalChangeBelow;
            dst.ArousalChangeWithin = ArousalChangeWithin;
            dst.ArousalChangeAbove  = ArousalChangeAbove;
        }
        if (!dst.OverrideResistance)
        {
            dst.ResistanceChangeStop   = ResistanceChangeStop;
            dst.ResistanceChangeBelow  = ResistanceChangeBelow;
            dst.ResistanceChangeWithin = ResistanceChangeWithin;
            dst.ResistanceChangeAbove  = ResistanceChangeAbove;
        }
        if (!dst.OverrideFatigue)
        {
            dst.FatigueChangeStop   = FatigueChangeStop;
            dst.FatigueChangeBelow  = FatigueChangeBelow;
            dst.FatigueChangeWithin = FatigueChangeWithin;
            dst.FatigueChangeAbove  = FatigueChangeAbove;
            dst.FatigueMultiplier   = FatigueMultiplier;
        }
        if (!dst.OverrideDrive)
        {
            dst.DriveChangeStop          = DriveChangeStop;
            dst.DriveChangeBelow         = DriveChangeBelow;
            dst.DriveChangeWithin        = DriveChangeWithin;
            dst.DriveChangeAbove         = DriveChangeAbove;
            dst.DriveChangeDelay         = DriveChangeDelay;
            dst.DriveArousalBoostFactor  = DriveArousalBoostFactor;
        }
        if (!dst.OverrideDriveBias)
        {
            dst.DriveBiasShiftBelow  = DriveBiasShiftBelow;
            dst.DriveBiasShiftAbove  = DriveBiasShiftAbove;
            dst.DriveBiasDecayWithin = DriveBiasDecayWithin;
            dst.DriveBiasDecayStop   = DriveBiasDecayStop;
        }
        if (!dst.OverrideOrgasm)
        {
            dst.OrgasmThreshold           = OrgasmThreshold;
            dst.OrgasmThresholdMultiplier = OrgasmThresholdMultiplier;
            dst.OrgasmArousalResetTo      = OrgasmArousalResetTo;
            dst.WithholdDuration          = WithholdDuration;
        }
        if (!dst.OverrideEdge)
        {
            dst.EdgeNeutralIntensity = EdgeNeutralIntensity;
            dst.EdgeFillRate         = EdgeFillRate;
            dst.EdgeFillCurve        = EdgeFillCurve;
            dst.EdgeDrainRate        = EdgeDrainRate;
            dst.EdgePeakHoldDuration = EdgePeakHoldDuration;
            dst.EdgeResistanceFactor = EdgeResistanceFactor;
            dst.EdgeDriveBoostFactor = EdgeDriveBoostFactor;
        }
        if (!dst.OverrideTransition)
        {
            dst.TransitionAboveDuration              = TransitionAboveDuration;
            dst.TransitionBelowDuration              = TransitionBelowDuration;
            dst.TransitionWithinDuration             = TransitionWithinDuration;
            dst.TransitionDriveThreshold             = TransitionDriveThreshold;
            dst.TransitionFatigueThreshold           = TransitionFatigueThreshold;
            dst.TransitionBrokenDownArousalThreshold  = TransitionBrokenDownArousalThreshold;
            dst.TransitionSurrenderedArousalThreshold = TransitionSurrenderedArousalThreshold;
            dst.TransitionOverriddenExitArousal       = TransitionOverriddenExitArousal;
            dst.TransitionAcclimatingArousalThreshold  = TransitionAcclimatingArousalThreshold;
            dst.TransitionDefensiveResistanceThreshold = TransitionDefensiveResistanceThreshold;
        }
        if (!dst.OverrideFrustration)
        {
            dst.FrustrationStackGain      = FrustrationStackGain;
            dst.FrustrationStackThreshold = FrustrationStackThreshold;
            dst.FrustrationDriveThreshold = FrustrationDriveThreshold;
        }
        if (!dst.OverrideNeedMotion)
        {
            dst.NeedMotionStopCalmChange    = NeedMotionStopCalmChange;
            dst.NeedMotionStopArousedChange = NeedMotionStopArousedChange;
            dst.NeedMotionBelowChange       = NeedMotionBelowChange;
            dst.NeedMotionWithinChange      = NeedMotionWithinChange;
            dst.NeedMotionAboveChange       = NeedMotionAboveChange;
            dst.NeedMotionArousalThreshold  = NeedMotionArousalThreshold;
        }
        if (!dst.OverrideSub)
        {
            dst.SubADriveChange       = SubADriveChange;
            dst.SubAArousalChange     = SubAArousalChange;
            dst.SubADriveBiasGain     = SubADriveBiasGain;
            dst.SubAResistanceChange  = SubAResistanceChange;
            dst.SubBDriveChange       = SubBDriveChange;
            dst.SubBArousalChange     = SubBArousalChange;
            dst.SubBDriveBiasGain     = SubBDriveBiasGain;
            dst.SubBResistanceChange  = SubBResistanceChange;
            dst.SubBothArousalChange  = SubBothArousalChange;
        }
    }
#endif
}
