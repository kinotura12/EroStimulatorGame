// SimulationManager.cs
// MonoBehaviour：全クラスを統括するエントリーポイント
// このコンポーネントをGameObjectにアタッチして使う

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SimulationManager : MonoBehaviour
{
    struct DebugStatePreset
    {
        public float arousal;
        public float resistance;
        public float fatigue;
        public float drive;
        public float driveBias;
        public float needMotion;
        public float frustrationStack;
        public float edgeTension;
        public float edgeDwellTime;
        public float edgePeakTimer;
        public float orgasmScale;
        public float cumulativeOrgasm;
        public InputBand band;
        public float bandDuration;
        public bool subA;
        public bool subB;
        public string note;
    }

    struct DebugRouteSpec
    {
        public SimState fromState;
        public SimState toState;
        public string routeLabel;
        public float driveBiasOverride;
    }

    [Header("=== 参照 ===")]
    [SerializeField] InputHandler inputHandler;
    [SerializeField] SimSharedConfig sharedConfig;          // 全状態共通の基礎係数
    [SerializeField] SimStateConfig[] stateConfigs;         // enum順に7つ設定
    [SerializeField] StateTransitionConfig transitionConfig; // 遷移ルール定義

    [Header("=== OutputDriver係数 ===")]
    [SerializeField] OutputDriver.OutputWeights outputWeights = new OutputDriver.OutputWeights();
    [Header("=== 拒否モーション（Reject） ===")]
    [SerializeField] RejectMotionSettings rejectSettings = new RejectMotionSettings();

    [Header("=== 現在の状態（確認用・読み取り専用） ===")]
    [SerializeField] SimState      currentState;
    [SerializeField] SimParameters param = new SimParameters();
    [SerializeField] SimulationOutput output;

    [Header("=== デバッグ ===")]
    [SerializeField] bool debugLogging = false;
    [SerializeField] int stateOrgasmCount;  // 現在の状態での射精回数（確認用）
    // --- 内部クラス ---
    BandEvaluator     bandEvaluator     = new BandEvaluator();
    ParameterUpdater  parameterUpdater  = new ParameterUpdater();
    StateResolver     stateResolver     = new StateResolver();
    EndJudge          endJudge          = new EndJudge();
    OutputDriver      outputDriver;
    readonly RejectMotionController rejectMotion = new RejectMotionController();
    readonly SimResolvedConfig runtimeConfig = new SimResolvedConfig();

    // タイマー類
    float aboveDuration;
    float belowDuration;
    float withinDuration;
    float stopDuration;
    float driveRampTimer;
    float forceStateLockTimer;  // ForceState後の遷移評価ロック（秒）

    InputBand currentBand;
    bool justOrgasmed;
    bool missingInputLogged;
    bool missingTransitionConfigLogged;

    // 外部から出力を取得するプロパティ
    public SimulationOutput Output        => output;
    public SimState         State         => currentState;
    public SimParameters    Param         => param;
    public float            CurrentTolLow           => runtimeConfig.TolLow;
    public float            CurrentTolHigh          => runtimeConfig.TolHigh;
    public float            CurrentEdgePeakHoldDuration => runtimeConfig.EdgePeakHoldDuration;
    public InputBand        CurrentBand             => currentBand;
    public float            AboveDuration           => aboveDuration;
    public float            BelowDuration           => belowDuration;
    public float            WithinDuration          => withinDuration;
    public float            StopDuration            => stopDuration;
    public float            RejectHabituation => rejectMotion.Habituation;
    public float            RejectOffsetX => rejectMotion.OffsetX;
    public float            RejectTriggerRate => rejectMotion.TriggerRate;


    // イベント
    public System.Action<SimState>       OnStateChanged;
    public System.Action                 OnOrgasm;
    public System.Action                 OnFemaleOrgasm;   // DriveBias < 0 時（メスイキ）
    public System.Action                 OnMaleOrgasm;     // DriveBias >= 0 時（オスイキ）
    public System.Action<SimState>       OnEnding;

    void Awake()
    {
        EnsureRuntimeInitialized();
        param.Reset();
        rejectMotion.Reset(GetInitialRejectHab01());
        output = default;
        currentState = SimState.Guarded;
        currentBand = InputBand.Stop;
        SimStateConfig rawConfig = GetConfig(currentState);
        if (rawConfig != null)
            ResolveRuntimeConfig(rawConfig, runtimeConfig);
    }

    void OnEnable()
    {
        EnsureRuntimeInitialized();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (transitionConfig != null) return;
        var guids = AssetDatabase.FindAssets("t:StateTransitionConfig");
        if (guids == null || guids.Length == 0) return;
        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        transitionConfig = AssetDatabase.LoadAssetAtPath<StateTransitionConfig>(path);
        if (transitionConfig != null)
            EditorUtility.SetDirty(this);
    }
#endif

    void Update()
    {
        EnsureRuntimeInitialized();

        if (inputHandler == null)
        {
            if (!missingInputLogged)
            {
                Debug.LogError("InputHandler が未設定です。SimulationManager の inputHandler を割り当ててください。");
                missingInputLogged = true;
            }
            return;
        }

        if (transitionConfig == null)
        {
            if (!missingTransitionConfigLogged)
            {
                Debug.LogError("StateTransitionConfig が未設定です。SimulationManager の transitionConfig に Assets/Sim/StateTransitionConfig.asset を割り当ててください。");
                missingTransitionConfigLogged = true;
            }
            return;
        }
        missingTransitionConfigLogged = false;

        bool isEnd = IsEndState(currentState);

        // エンド状態でない場合のみconfig更新
        if (!isEnd)
        {
            SimStateConfig rawConfig = GetConfig(currentState);
            if (rawConfig == null)
            {
                Debug.LogError($"SimStateConfig が未設定です: {currentState}");
                return;
            }
            ResolveRuntimeConfig(rawConfig, runtimeConfig);
        }

        // 1. 入力帯判定（エンド状態でも継続）
        currentBand  = bandEvaluator.Evaluate(
            inputHandler.MainIntensity,
            inputHandler.IsActive,
            runtimeConfig.TolLow,
            runtimeConfig.TolHigh);

        // 2. パラメータ更新（エンド状態でも継続 → キャラのアニメが自然に変化し続ける）
        justOrgasmed = false;
        parameterUpdater.Update(
            param,
            currentState,
            currentBand,
            inputHandler.SubA,
            inputHandler.SubB,
            runtimeConfig,
            Time.deltaTime,
            ref aboveDuration,
            ref belowDuration,
            ref withinDuration,
            ref stopDuration,
            ref driveRampTimer);

        // 3. 射精判定（エンド状態でも継続）
        if (endJudge.UpdateOrgasm(param, runtimeConfig, inputHandler.MainIntensity, Time.deltaTime))
        {
            justOrgasmed = true;
            if (!isEnd) stateOrgasmCount++;
            bool isFemaleOrgasm = endJudge.OnOrgasm(param, runtimeConfig);
            OnOrgasm?.Invoke();
            if (isFemaleOrgasm)
                OnFemaleOrgasm?.Invoke();
            else
                OnMaleOrgasm?.Invoke();
        }

        // 4. BrokenDown突入時: DriveBias変換 + モードロック（エンド状態はスキップ）
        if (!isEnd)
        {
            // ForceState後のロック期間中は遷移評価をスキップ
            if (forceStateLockTimer > 0f)
            {
                forceStateLockTimer -= Time.deltaTime;
            }
            else
            {
                SimState previousState = currentState;
                SimState nextState = stateResolver.Resolve(
                    currentState, param, currentBand, runtimeConfig,
                    aboveDuration, belowDuration, withinDuration, stopDuration,
                    stateOrgasmCount, transitionConfig);

                if (nextState == SimState.BrokenDown && previousState != SimState.BrokenDown)
                {
                    // プラスモード突入時はFatigueをリセット（アヘ顔のサービスタイム確保・仕様2-3）
                    // マイナスモード（トロトロ）はFatigueが残るため短命になりやすい（意図通り）
                    if (param.DriveBias >= 0f)
                        param.Fatigue *= 0.3f;
                }

                // 5. 状態遷移
                if (nextState != currentState)
                {
                    if (debugLogging)
                        Debug.Log($"[State] {currentState} → {nextState} | Band:{currentBand} | Arousal:{param.Arousal:F2} | Drive:{param.Drive:F2}");

                    currentState = nextState;
                    stateOrgasmCount = 0;  // 状態が変わったら射精カウントをリセット
                    OnStateChanged?.Invoke(currentState);

                    if (IsEndState(currentState))
                        OnEnding?.Invoke(currentState);

                    // タイマーリセット
                    aboveDuration  = 0f;
                    belowDuration  = 0f;
                    withinDuration = 0f;
                }
            }
        }

        // 6. 出力計算（エンド状態でも継続）
        rejectMotion.Update(
            Time.deltaTime,
            param.Resistance,
            inputHandler.IsActive,
            inputHandler.MainIntensity,
            inputHandler.SubA,
            inputHandler.SubB,
            rejectSettings);

        output = outputDriver.Compute(param, justOrgasmed);
        output.RejectMotion = rejectMotion.MotionIntensity;
        output.RejectHabituation = rejectMotion.Habituation;
        output.EdgeTension     = param.EdgeTension;
        output.OrgasmScale     = param.OrgasmScale;
        output.CumulativeOrgasm = param.CumulativeOrgasm;

        // 7. デバッグログ
        if (debugLogging)
            Debug.Log($"[{currentState}] Band:{currentBand} | Arousal:{param.Arousal:F2} Resistance:{param.Resistance:F2} Fatigue:{param.Fatigue:F2} Drive:{param.Drive:F2} DriveBias:{param.DriveBias:F2}");
    }

    SimStateConfig GetConfig(SimState state)
    {
        if (stateConfigs == null || stateConfigs.Length == 0) return null;
        int index = (int)state;
        if (index < 0 || index >= stateConfigs.Length) return null;
        return stateConfigs[index];
    }

    void ResolveRuntimeConfig(SimStateConfig src, SimResolvedConfig dst)
    {
        // まず state 固有値をコピー
        dst.TolLow = src.TolLow;
        dst.TolHigh = src.TolHigh;

        dst.ArousalChangeStop   = src.ArousalChangeStop;
        dst.ArousalChangeBelow  = src.ArousalChangeBelow;
        dst.ArousalChangeWithin = src.ArousalChangeWithin;
        dst.ArousalChangeAbove  = src.ArousalChangeAbove;

        dst.ResistanceChangeStop   = src.ResistanceChangeStop;
        dst.ResistanceChangeBelow  = src.ResistanceChangeBelow;
        dst.ResistanceChangeWithin = src.ResistanceChangeWithin;
        dst.ResistanceChangeAbove  = src.ResistanceChangeAbove;

        dst.FatigueChangeStop   = src.FatigueChangeStop;
        dst.FatigueChangeBelow  = src.FatigueChangeBelow;
        dst.FatigueChangeWithin = src.FatigueChangeWithin;
        dst.FatigueChangeAbove  = src.FatigueChangeAbove;
        dst.FatigueMultiplier   = src.FatigueMultiplier;

        dst.DriveChangeStop          = src.DriveChangeStop;
        dst.DriveChangeBelow         = src.DriveChangeBelow;
        dst.DriveChangeWithin        = src.DriveChangeWithin;
        dst.DriveChangeAbove         = src.DriveChangeAbove;
        dst.DriveChangeDelay         = src.DriveChangeDelay;
        dst.DriveArousalBoostFactor  = src.DriveArousalBoostFactor;

        // 射精時Resistance低下：BaseDropはSharedConfig固定、CoefficientはStateConfig固定
        dst.OrgasmResistanceDropCoefficient = src.OrgasmResistanceDropCoefficient;

        dst.DriveBiasShiftBelow = src.DriveBiasShiftBelow;
        dst.DriveBiasShiftAbove = src.DriveBiasShiftAbove;
        dst.DriveBiasDecayWithin = src.DriveBiasDecayWithin;
        dst.DriveBiasDecayStop = src.DriveBiasDecayStop;

        dst.OrgasmThreshold           = src.OrgasmThreshold;
        dst.OrgasmThresholdMultiplier = src.OrgasmThresholdMultiplier;
        dst.OrgasmArousalResetTo      = src.OrgasmArousalResetTo;
        dst.WithholdDuration          = src.WithholdDuration;

        dst.EdgeNeutralIntensity = src.EdgeNeutralIntensity;
        dst.EdgeFillRate         = src.EdgeFillRate;
        dst.EdgeFillCurve        = src.EdgeFillCurve;
        dst.EdgeDrainRate        = src.EdgeDrainRate;
        dst.EdgePeakHoldDuration = src.EdgePeakHoldDuration;

        dst.TransitionAboveDuration               = src.TransitionAboveDuration;
        dst.TransitionBelowDuration               = src.TransitionBelowDuration;
        dst.TransitionWithinDuration              = src.TransitionWithinDuration;
        dst.TransitionDriveThreshold              = src.TransitionDriveThreshold;
        dst.TransitionFatigueThreshold            = src.TransitionFatigueThreshold;
        dst.TransitionBrokenDownArousalThreshold  = src.TransitionBrokenDownArousalThreshold;
        dst.TransitionSurrenderedArousalThreshold = src.TransitionSurrenderedArousalThreshold;
        dst.TransitionOverriddenExitArousal       = src.TransitionOverriddenExitArousal;
        dst.TransitionAcclimatingArousalThreshold  = src.TransitionAcclimatingArousalThreshold;
        dst.TransitionDefensiveResistanceThreshold = src.TransitionDefensiveResistanceThreshold;

        dst.FrustrationStackGain = src.FrustrationStackGain;
        dst.FrustrationStackThreshold = src.FrustrationStackThreshold;
        dst.FrustrationDriveThreshold = src.FrustrationDriveThreshold;

        // NeedMotion係数（StateConfigのデフォルト値をまず設定）
        dst.NeedMotionStopCalmChange    = src.NeedMotionStopCalmChange;
        dst.NeedMotionStopArousedChange = src.NeedMotionStopArousedChange;
        dst.NeedMotionBelowChange       = src.NeedMotionBelowChange;
        dst.NeedMotionWithinChange      = src.NeedMotionWithinChange;
        dst.NeedMotionAboveChange       = src.NeedMotionAboveChange;
        dst.NeedMotionArousalThreshold  = src.NeedMotionArousalThreshold;

        // Sub効果係数
        dst.SubADriveChange       = src.SubADriveChange;
        dst.SubAArousalChange     = src.SubAArousalChange;
        dst.SubADriveBiasGain     = src.SubADriveBiasGain;
        dst.SubAResistanceChange  = src.SubAResistanceChange;
        dst.SubBDriveChange       = src.SubBDriveChange;
        dst.SubBArousalChange     = src.SubBArousalChange;
        dst.SubBDriveBiasGain     = src.SubBDriveBiasGain;
        dst.SubBResistanceChange  = src.SubBResistanceChange;
        dst.SubBothArousalChange  = src.SubBothArousalChange;

        // shared 未設定時は state 値をそのまま使う
        if (sharedConfig == null) return;

        // SharedConfig固定値（状態上書き不可）
        dst.OrgasmFatigueGain            = sharedConfig.OrgasmFatigueGain;
        dst.OrgasmResistanceBaseDrop     = sharedConfig.OrgasmResistanceBaseDrop;
        dst.EdgeDwellScaleMax           = sharedConfig.EdgeDwellScaleMax;
        dst.OrgasmCumulativeGain        = sharedConfig.OrgasmCumulativeGain;
        dst.OrgasmCumulativeDecayRate   = sharedConfig.OrgasmCumulativeDecayRate;
        dst.OrgasmCumulativeBonusScale  = sharedConfig.OrgasmCumulativeBonusScale;
        dst.EndAOrgasmCount   = sharedConfig.EndAOrgasmCount;
        dst.EndBOrgasmCount   = sharedConfig.EndBOrgasmCount;
        dst.EndCOrgasmCount   = sharedConfig.EndCOrgasmCount;

        if (!src.OverrideArousal)
        {
            dst.ArousalChangeStop   = sharedConfig.ArousalChangeStop;
            dst.ArousalChangeBelow  = sharedConfig.ArousalChangeBelow;
            dst.ArousalChangeWithin = sharedConfig.ArousalChangeWithin;
            dst.ArousalChangeAbove  = sharedConfig.ArousalChangeAbove;
        }

        if (!src.OverrideResistance)
        {
            dst.ResistanceChangeStop   = sharedConfig.ResistanceChangeStop;
            dst.ResistanceChangeBelow  = sharedConfig.ResistanceChangeBelow;
            dst.ResistanceChangeWithin = sharedConfig.ResistanceChangeWithin;
            dst.ResistanceChangeAbove  = sharedConfig.ResistanceChangeAbove;
        }

        if (!src.OverrideFatigue)
        {
            dst.FatigueChangeStop   = sharedConfig.FatigueChangeStop;
            dst.FatigueChangeBelow  = sharedConfig.FatigueChangeBelow;
            dst.FatigueChangeWithin = sharedConfig.FatigueChangeWithin;
            dst.FatigueChangeAbove  = sharedConfig.FatigueChangeAbove;
            dst.FatigueMultiplier   = sharedConfig.FatigueMultiplier;
        }

        if (!src.OverrideDrive)
        {
            dst.DriveChangeStop         = sharedConfig.DriveChangeStop;
            dst.DriveChangeBelow        = sharedConfig.DriveChangeBelow;
            dst.DriveChangeWithin       = sharedConfig.DriveChangeWithin;
            dst.DriveChangeAbove        = sharedConfig.DriveChangeAbove;
            dst.DriveChangeDelay        = sharedConfig.DriveChangeDelay;
            dst.DriveArousalBoostFactor = sharedConfig.DriveArousalBoostFactor;
        }

        if (!src.OverrideDriveBias)
        {
            dst.DriveBiasShiftBelow = sharedConfig.DriveBiasShiftBelow;
            dst.DriveBiasShiftAbove = sharedConfig.DriveBiasShiftAbove;
            dst.DriveBiasDecayWithin = sharedConfig.DriveBiasDecayWithin;
            dst.DriveBiasDecayStop = sharedConfig.DriveBiasDecayStop;
        }

        // EdgeDecayRate は常に SharedConfig から（状態上書き不可）
        dst.EdgeDecayRate = sharedConfig.EdgeDecayRate;

        if (!src.OverrideEdge)
        {
            dst.EdgeNeutralIntensity = sharedConfig.EdgeNeutralIntensity;
            dst.EdgeFillRate         = sharedConfig.EdgeFillRate;
            dst.EdgeFillCurve        = sharedConfig.EdgeFillCurve;
            dst.EdgeDrainRate        = sharedConfig.EdgeDrainRate;
            dst.EdgePeakHoldDuration = sharedConfig.EdgePeakHoldDuration;
            dst.EdgeResistanceFactor = sharedConfig.EdgeResistanceFactor;
            dst.EdgeDriveBoostFactor = sharedConfig.EdgeDriveBoostFactor;
        }

        if (!src.OverrideOrgasm)
        {
            dst.OrgasmThreshold           = sharedConfig.OrgasmThreshold;
            dst.OrgasmThresholdMultiplier = sharedConfig.OrgasmThresholdMultiplier;
            dst.OrgasmArousalResetTo      = sharedConfig.OrgasmArousalResetTo;
            dst.WithholdDuration          = sharedConfig.WithholdDuration;
        }

        if (!src.OverrideTransition)
        {
            dst.TransitionAboveDuration              = sharedConfig.TransitionAboveDuration;
            dst.TransitionBelowDuration              = sharedConfig.TransitionBelowDuration;
            dst.TransitionWithinDuration             = sharedConfig.TransitionWithinDuration;
            dst.TransitionDriveThreshold             = sharedConfig.TransitionDriveThreshold;
            dst.TransitionFatigueThreshold           = sharedConfig.TransitionFatigueThreshold;
            dst.TransitionBrokenDownArousalThreshold  = sharedConfig.TransitionBrokenDownArousalThreshold;
            dst.TransitionSurrenderedArousalThreshold = sharedConfig.TransitionSurrenderedArousalThreshold;
            dst.TransitionOverriddenExitArousal       = sharedConfig.TransitionOverriddenExitArousal;
            dst.TransitionAcclimatingArousalThreshold  = sharedConfig.TransitionAcclimatingArousalThreshold;
            dst.TransitionDefensiveResistanceThreshold = sharedConfig.TransitionDefensiveResistanceThreshold;
        }

        if (!src.OverrideFrustration)
        {
            dst.FrustrationStackGain = sharedConfig.FrustrationStackGain;
            dst.FrustrationStackThreshold = sharedConfig.FrustrationStackThreshold;
            dst.FrustrationDriveThreshold = sharedConfig.FrustrationDriveThreshold;
        }

        if (!src.OverrideNeedMotion)
        {
            dst.NeedMotionStopCalmChange    = sharedConfig.NeedMotionStopCalmChange;
            dst.NeedMotionStopArousedChange = sharedConfig.NeedMotionStopArousedChange;
            dst.NeedMotionBelowChange       = sharedConfig.NeedMotionBelowChange;
            dst.NeedMotionWithinChange      = sharedConfig.NeedMotionWithinChange;
            dst.NeedMotionAboveChange       = sharedConfig.NeedMotionAboveChange;
            dst.NeedMotionArousalThreshold  = sharedConfig.NeedMotionArousalThreshold;
        }

        if (!src.OverrideSub)
        {
            dst.SubADriveChange       = sharedConfig.SubADriveChange;
            dst.SubAArousalChange     = sharedConfig.SubAArousalChange;
            dst.SubADriveBiasGain     = sharedConfig.SubADriveBiasGain;
            dst.SubAResistanceChange  = sharedConfig.SubAResistanceChange;
            dst.SubBDriveChange       = sharedConfig.SubBDriveChange;
            dst.SubBArousalChange     = sharedConfig.SubBArousalChange;
            dst.SubBDriveBiasGain     = sharedConfig.SubBDriveBiasGain;
            dst.SubBResistanceChange  = sharedConfig.SubBResistanceChange;
            dst.SubBothArousalChange  = sharedConfig.SubBothArousalChange;
        }
    }

    void EnsureRuntimeInitialized()
    {
        if (param == null)
            param = new SimParameters();

        if (outputDriver == null)
            outputDriver = new OutputDriver(outputWeights);

#if UNITY_EDITOR
        if (transitionConfig == null)
        {
            var guids = AssetDatabase.FindAssets("t:StateTransitionConfig");
            if (guids != null && guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                transitionConfig = AssetDatabase.LoadAssetAtPath<StateTransitionConfig>(path);
            }
        }
#endif
    }

    float GetInitialRejectHab01()
    {
        if (rejectSettings == null) return 0f;
        return Mathf.Clamp01(rejectSettings.InitialHab);
    }

    bool IsEndState(SimState s) =>
        s == SimState.End_A ||
        s == SimState.End_B ||
        s == SimState.End_C_White ||
        s == SimState.End_C_Overload;

    public string GetDebugPresetNote(SimState state)
    {
        return BuildDebugStatePreset(state).note;
    }

    public void RestartSimulation()
    {
        param.Reset();
        rejectMotion.Reset(GetInitialRejectHab01());
        output = default;
        currentState = SimState.Guarded;
        SimStateConfig rawConfig = GetConfig(currentState);
        if (rawConfig != null)
            ResolveRuntimeConfig(rawConfig, runtimeConfig);

        aboveDuration    = 0f;
        belowDuration    = 0f;
        withinDuration   = 0f;
        stopDuration     = 0f;
        driveRampTimer   = 0f;
        justOrgasmed     = false;
        stateOrgasmCount = 0;
        currentBand      = InputBand.Stop;

        if (inputHandler != null)
        {
            inputHandler.SetSubA(false);
            inputHandler.SetSubB(false);
            inputHandler.SetMainIntensity(0f);
            inputHandler.SetStop();
        }

        OnStateChanged?.Invoke(currentState);
    }

    // デバッグ用：強制状態遷移（パラメータ維持）
    public void ForceState(SimState state)
    {
        if (IsEndState(state)) return;
        currentState = state;
        SimStateConfig rawConfig = GetConfig(currentState);
        if (rawConfig != null)
            ResolveRuntimeConfig(rawConfig, runtimeConfig);
        aboveDuration  = 0f;
        belowDuration  = 0f;
        withinDuration = 0f;
        stopDuration   = 0f;
        stateOrgasmCount = 0;
        forceStateLockTimer = 1.5f;  // 1.5秒間、遷移評価をロック
        OnStateChanged?.Invoke(currentState);
    }

    public void ForceStateForDebug(SimState state)
    {
        if (IsEndState(state)) return;

        var preset = BuildDebugStatePreset(state);
        ApplyDebugStatePreset(state, preset);
    }

    // デバッグ用：強制状態遷移 + パラメータプリセット適用
    public void ForceStateWithPreset(SimState state,
        float arousal, float resistance, float fatigue, float drive, float driveBias)
    {
        param.Arousal          = Mathf.Clamp01(arousal);
        param.Resistance       = Mathf.Clamp01(resistance);
        param.Fatigue          = Mathf.Clamp01(fatigue);
        param.Drive            = Mathf.Clamp01(drive);
        param.DriveBias        = Mathf.Clamp(driveBias, -1f, 1f);
        param.NeedMotion       = 0f;
        param.FrustrationStack = 0f;
        param.EdgeTension      = 0f;
        param.EdgeDwellTime    = 0f;
        param.EdgePeakTimer    = 0f;
        ForceState(state);
    }

    DebugStatePreset BuildDebugStatePreset(SimState state)
    {
        var preset = GetBaselineDebugPreset(state);
        if (state == SimState.Guarded)
            return preset;

        if (!TryGetRepresentativeDebugRoute(state, out var route, out var rule))
            return preset;

        ApplyRuleConditionsToPreset(ref preset, rule);
        ApplyRouteFlavorToPreset(ref preset, route, rule);
        preset.note = $"{route.routeLabel} | {rule.note}";
        return preset;
    }

    DebugStatePreset GetBaselineDebugPreset(SimState state)
    {
        return state switch
        {
            SimState.Guarded => new DebugStatePreset
            {
                arousal = 0.12f,
                resistance = 0.52f,
                fatigue = 0.08f,
                drive = 0.08f,
                driveBias = -0.05f,
                needMotion = 0.08f,
                frustrationStack = 0.02f,
                edgeTension = 0f,
                edgeDwellTime = 0f,
                edgePeakTimer = 0f,
                orgasmScale = 0f,
                cumulativeOrgasm = 0.05f,
                band = InputBand.Stop,
                bandDuration = 0.6f,
                subA = false,
                subB = false,
                note = "初期寄りのベース値"
            },
            SimState.Defensive => new DebugStatePreset
            {
                arousal = 0.24f,
                resistance = 0.78f,
                fatigue = 0.22f,
                drive = 0.22f,
                driveBias = 0.06f,
                needMotion = 0.18f,
                frustrationStack = 0.08f,
                edgeTension = 0f,
                edgeDwellTime = 0f,
                edgePeakTimer = 0f,
                orgasmScale = 0f,
                cumulativeOrgasm = 0.06f,
                band = InputBand.Above,
                bandDuration = 0.8f,
                subA = false,
                subB = true,
                note = "Defensive baseline"
            },
            SimState.Overridden => new DebugStatePreset
            {
                arousal = 0.58f,
                resistance = 0.42f,
                fatigue = 0.70f,
                drive = 0.56f,
                driveBias = 0.10f,
                needMotion = 0.10f,
                frustrationStack = 0.10f,
                edgeTension = 0.24f,
                edgeDwellTime = 0.6f,
                edgePeakTimer = 0f,
                orgasmScale = 0.20f,
                cumulativeOrgasm = 0.16f,
                band = InputBand.Above,
                bandDuration = 1.1f,
                subA = true,
                subB = false,
                note = "Overridden baseline"
            },
            SimState.FrustratedCraving => new DebugStatePreset
            {
                arousal = 0.44f,
                resistance = 0.54f,
                fatigue = 0.34f,
                drive = 0.58f,
                driveBias = -0.16f,
                needMotion = 0.42f,
                frustrationStack = 0.40f,
                edgeTension = 0.18f,
                edgeDwellTime = 0.3f,
                edgePeakTimer = 0f,
                orgasmScale = 0f,
                cumulativeOrgasm = 0.12f,
                band = InputBand.Below,
                bandDuration = 5.1f,
                subA = false,
                subB = false,
                note = "Frustrated baseline"
            },
            SimState.Acclimating => new DebugStatePreset
            {
                arousal = 0.54f,
                resistance = 0.24f,
                fatigue = 0.32f,
                drive = 0.38f,
                driveBias = -0.04f,
                needMotion = 0.16f,
                frustrationStack = 0.05f,
                edgeTension = 0.10f,
                edgeDwellTime = 0.2f,
                edgePeakTimer = 0f,
                orgasmScale = 0f,
                cumulativeOrgasm = 0.08f,
                band = InputBand.Within,
                bandDuration = 0.9f,
                subA = true,
                subB = false,
                note = "Acclimating baseline"
            },
            SimState.Surrendered => new DebugStatePreset
            {
                arousal = 0.70f,
                resistance = 0.14f,
                fatigue = 0.46f,
                drive = 0.60f,
                driveBias = -0.08f,
                needMotion = 0.12f,
                frustrationStack = 0.02f,
                edgeTension = 0.20f,
                edgeDwellTime = 0.5f,
                edgePeakTimer = 0f,
                orgasmScale = 0.16f,
                cumulativeOrgasm = 0.22f,
                band = InputBand.Within,
                bandDuration = 1.2f,
                subA = true,
                subB = false,
                note = "Surrendered baseline"
            },
            SimState.BrokenDown => new DebugStatePreset
            {
                arousal = 0.78f,
                resistance = 0.06f,
                fatigue = 0.82f,
                drive = 0.80f,
                driveBias = 0.20f,
                needMotion = 0.06f,
                frustrationStack = 0f,
                edgeTension = 0.34f,
                edgeDwellTime = 0.9f,
                edgePeakTimer = 0f,
                orgasmScale = 0.26f,
                cumulativeOrgasm = 0.30f,
                band = InputBand.Above,
                bandDuration = 0.8f,
                subA = true,
                subB = false,
                note = "BrokenDown baseline"
            },
            _ => default
        };
    }

    bool TryGetRepresentativeDebugRoute(SimState state, out DebugRouteSpec route, out TransitionRule rule)
    {
        route = state switch
        {
            SimState.Defensive => new DebugRouteSpec { fromState = SimState.Guarded, toState = SimState.Defensive, routeLabel = "代表流入: Guarded -> Defensive", driveBiasOverride = 0.08f },
            SimState.Overridden => new DebugRouteSpec { fromState = SimState.Defensive, toState = SimState.Overridden, routeLabel = "代表流入: Defensive -> Overridden", driveBiasOverride = 0.12f },
            SimState.FrustratedCraving => new DebugRouteSpec { fromState = SimState.Defensive, toState = SimState.FrustratedCraving, routeLabel = "代表流入: Defensive -> FrustratedCraving", driveBiasOverride = -0.18f },
            SimState.Acclimating => new DebugRouteSpec { fromState = SimState.Guarded, toState = SimState.Acclimating, routeLabel = "代表流入: Guarded -> Acclimating", driveBiasOverride = -0.04f },
            SimState.Surrendered => new DebugRouteSpec { fromState = SimState.Acclimating, toState = SimState.Surrendered, routeLabel = "代表流入: Acclimating -> Surrendered", driveBiasOverride = -0.10f },
            SimState.BrokenDown => new DebugRouteSpec { fromState = SimState.Surrendered, toState = SimState.BrokenDown, routeLabel = "代表流入: Surrendered -> BrokenDown", driveBiasOverride = 0.24f },
            _ => default
        };

        rule = null;
        if (transitionConfig == null || transitionConfig.rules == null)
            return false;

        foreach (var candidate in transitionConfig.rules)
        {
            if (!candidate.enabled) continue;
            if (candidate.fromState != route.fromState) continue;
            if (candidate.toState != route.toState) continue;
            rule = candidate;
            return true;
        }

        return false;
    }

    void ApplyRuleConditionsToPreset(ref DebugStatePreset preset, TransitionRule rule)
    {
        if (rule == null || rule.conditions == null) return;

        foreach (var condition in rule.conditions)
        {
            float threshold = condition.threshold;
            switch (condition.param)
            {
                case ConditionParam.Arousal:
                    preset.arousal = OffsetFromThreshold(condition.op, threshold, 0.05f);
                    break;
                case ConditionParam.Resistance:
                    preset.resistance = OffsetFromThreshold(condition.op, threshold, 0.05f);
                    break;
                case ConditionParam.Fatigue:
                    preset.fatigue = OffsetFromThreshold(condition.op, threshold, 0.05f);
                    break;
                case ConditionParam.Drive:
                    preset.drive = OffsetFromThreshold(condition.op, threshold, 0.05f);
                    break;
                case ConditionParam.DriveBias:
                    preset.driveBias = OffsetSignedFromThreshold(condition.op, threshold, 0.08f);
                    break;
                case ConditionParam.EdgeDwellTime:
                    preset.edgeDwellTime = Mathf.Max(0f, threshold + 0.2f);
                    break;
            }
        }
    }

    void ApplyRouteFlavorToPreset(ref DebugStatePreset preset, DebugRouteSpec route, TransitionRule rule)
    {
        preset.driveBias = route.driveBiasOverride;

        if (rule.requiredBand != BandRequirement.Any)
        {
            preset.band = ToInputBand(rule.requiredBand);
            preset.bandDuration = Mathf.Max(rule.bandDuration + 0.2f, 0.4f);
        }
        else
        {
            preset.bandDuration = Mathf.Max(preset.bandDuration, 0.6f);
        }

        switch (route.toState)
        {
            case SimState.Defensive:
                preset.subA = false;
                preset.subB = true;
                break;
            case SimState.Overridden:
                preset.band = InputBand.Above;
                preset.bandDuration = Mathf.Max(preset.bandDuration, 1.0f);
                preset.subA = true;
                preset.subB = false;
                preset.edgeTension = Mathf.Max(preset.edgeTension, 0.24f);
                break;
            case SimState.FrustratedCraving:
                preset.band = InputBand.Below;
                preset.bandDuration = Mathf.Max(preset.bandDuration, 5.2f);
                preset.subA = false;
                preset.subB = false;
                preset.needMotion = Mathf.Max(preset.needMotion, 0.40f);
                preset.frustrationStack = Mathf.Max(preset.frustrationStack, 0.40f);
                break;
            case SimState.Acclimating:
                preset.band = InputBand.Within;
                preset.bandDuration = Mathf.Max(preset.bandDuration, 0.8f);
                preset.subA = true;
                preset.subB = false;
                break;
            case SimState.Surrendered:
                preset.band = InputBand.Within;
                preset.bandDuration = Mathf.Max(preset.bandDuration, 1.0f);
                preset.subA = true;
                preset.subB = false;
                preset.orgasmScale = Mathf.Max(preset.orgasmScale, 0.16f);
                preset.cumulativeOrgasm = Mathf.Max(preset.cumulativeOrgasm, 0.20f);
                break;
            case SimState.BrokenDown:
                preset.band = InputBand.Above;
                preset.bandDuration = Mathf.Max(preset.bandDuration, 0.8f);
                preset.subA = true;
                preset.subB = false;
                preset.edgeTension = Mathf.Max(preset.edgeTension, 0.30f);
                preset.cumulativeOrgasm = Mathf.Max(preset.cumulativeOrgasm, 0.28f);
                break;
        }
    }

    float OffsetFromThreshold(CompareOp op, float threshold, float margin)
    {
        return op switch
        {
            CompareOp.GreaterEqual => Mathf.Clamp01(threshold + margin),
            CompareOp.GreaterThan => Mathf.Clamp01(threshold + margin),
            CompareOp.LessEqual => Mathf.Clamp01(threshold - margin),
            CompareOp.LessThan => Mathf.Clamp01(threshold - margin),
            _ => Mathf.Clamp01(threshold)
        };
    }

    float OffsetSignedFromThreshold(CompareOp op, float threshold, float margin)
    {
        float value = op switch
        {
            CompareOp.GreaterEqual => threshold + margin,
            CompareOp.GreaterThan => threshold + margin,
            CompareOp.LessEqual => threshold - margin,
            CompareOp.LessThan => threshold - margin,
            _ => threshold
        };
        return Mathf.Clamp(value, -1f, 1f);
    }

    InputBand ToInputBand(BandRequirement requirement)
    {
        return requirement switch
        {
            BandRequirement.Stop => InputBand.Stop,
            BandRequirement.Below => InputBand.Below,
            BandRequirement.Within => InputBand.Within,
            BandRequirement.Above => InputBand.Above,
            _ => InputBand.Stop
        };
    }

    void ApplyDebugStatePreset(SimState state, DebugStatePreset preset)
    {
        ForceState(state);

        param.Arousal          = Mathf.Clamp01(preset.arousal);
        param.Resistance       = Mathf.Clamp01(preset.resistance);
        param.Fatigue          = Mathf.Clamp01(preset.fatigue);
        param.Drive            = Mathf.Clamp01(preset.drive);
        param.DriveBias        = Mathf.Clamp(preset.driveBias, -1f, 1f);
        param.NeedMotion       = Mathf.Clamp01(preset.needMotion);
        param.FrustrationStack = Mathf.Clamp01(preset.frustrationStack);
        param.EdgeTension      = Mathf.Clamp01(preset.edgeTension);
        param.EdgeDwellTime    = Mathf.Max(0f, preset.edgeDwellTime);
        param.EdgePeakTimer    = Mathf.Max(0f, preset.edgePeakTimer);
        param.OrgasmScale      = Mathf.Clamp01(preset.orgasmScale);
        param.CumulativeOrgasm = Mathf.Clamp01(preset.cumulativeOrgasm);

        // MainIntensity / SubA / SubB はユーザー入力値を保持（変更しない）
        // band継続タイマーは 0 リセット。次のUpdate()で実入力から再積算される
        aboveDuration    = 0f;
        belowDuration    = 0f;
        withinDuration   = 0f;
        stopDuration     = 0f;
        driveRampTimer   = 0f;
        stateOrgasmCount = 0;
        justOrgasmed     = false;
        forceStateLockTimer = 1.5f;
    }

    // --- Unity側で手動接続が必要な作業（コメント） ---
    // 1. InputHandler を Inspector でアタッチ
    // 2. stateConfigs に SimStateConfig アセットを7つ（Guarded〜BrokenDown）設定
    // 3. OutputDriver.Output の各値を Animator Parameter / Cubism Parameter に接続
    //    → AnimationBlender.cs（別途作成）でOutputを受け取りLive2Dに流す
    // 4. スライダーの OnValueChanged → InputHandler.SetMainIntensity
    // 5. SubA/SubBトグルの OnClick → InputHandler.SetSubA / SetSubB
}
