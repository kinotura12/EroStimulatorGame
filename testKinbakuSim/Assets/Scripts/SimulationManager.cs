// SimulationManager.cs
// MonoBehaviour：全クラスを統括するエントリーポイント
// このコンポーネントをGameObjectにアタッチして使う

using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    [Header("=== 参照 ===")]
    [SerializeField] InputHandler inputHandler;
    [SerializeField] SimSharedConfig sharedConfig;          // 全状態共通の基礎係数
    [SerializeField] SimStateConfig[] stateConfigs;         // enum順に7つ設定
    [SerializeField] StateTransitionConfig transitionConfig; // 遷移ルール定義

    [Header("=== OutputDriver係数 ===")]
    [SerializeField] OutputDriver.OutputWeights outputWeights = new OutputDriver.OutputWeights();

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
    readonly SimResolvedConfig runtimeConfig = new SimResolvedConfig();

    // タイマー類
    float aboveDuration;
    float belowDuration;
    float withinDuration;
    float stopDuration;
    float driveRampTimer;

    InputBand currentBand;
    bool justOrgasmed;
    bool missingInputLogged;

    // 外部から出力を取得するプロパティ
    public SimulationOutput Output        => output;
    public SimState         State         => currentState;
    public SimParameters    Param         => param;
    public float            CurrentTolLow  => runtimeConfig.TolLow;
    public float            CurrentTolHigh => runtimeConfig.TolHigh;

    // イベント
    public System.Action<SimState>       OnStateChanged;
    public System.Action                 OnOrgasm;
    public System.Action<SimState>       OnEnding;

    void Awake()
    {
        EnsureRuntimeInitialized();
        param.Reset();
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
        if (endJudge.CheckOrgasm(param, runtimeConfig))
        {
            justOrgasmed = true;
            if (!isEnd) stateOrgasmCount++;
            endJudge.OnOrgasm(param, runtimeConfig);
            OnOrgasm?.Invoke();
        }

        // 4. BrokenDown突入時: DriveBias変換 + モードロック（エンド状態はスキップ）
        if (!isEnd)
        {
            SimState previousState = currentState;
            SimState nextState = stateResolver.Resolve(
                currentState, param, currentBand, runtimeConfig,
                aboveDuration, belowDuration, withinDuration, stopDuration,
                stateOrgasmCount, transitionConfig);

            if (nextState == SimState.BrokenDown && previousState != SimState.BrokenDown)
            {
                // Below継続で突入 → DriveBiasを－方向（トロトロ）に変換
                // Above継続で突入 → DriveBiasを＋方向（アヘ顔）に変換
                if (currentBand == InputBand.Below)
                    param.DriveBias = -Mathf.Abs(param.DriveBias);
                else if (currentBand == InputBand.Above)
                    param.DriveBias = Mathf.Abs(param.DriveBias);
                // それ以外（⑦直行等）は現在のDriveBiasをそのまま使用

                endJudge.LockBrokenDownMode(param);
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

        // 6. 出力計算（エンド状態でも継続）
        output = outputDriver.Compute(param, justOrgasmed);

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

        dst.DriveChangeStop   = src.DriveChangeStop;
        dst.DriveChangeBelow  = src.DriveChangeBelow;
        dst.DriveChangeWithin = src.DriveChangeWithin;
        dst.DriveChangeAbove  = src.DriveChangeAbove;
        dst.DriveChangeDelay  = src.DriveChangeDelay;

        dst.DriveBiasShiftBelow = src.DriveBiasShiftBelow;
        dst.DriveBiasShiftAbove = src.DriveBiasShiftAbove;
        dst.DriveBiasDecayWithin = src.DriveBiasDecayWithin;
        dst.DriveBiasDecayStop = src.DriveBiasDecayStop;

        dst.OrgasmThreshold           = src.OrgasmThreshold;
        dst.OrgasmThresholdMultiplier = src.OrgasmThresholdMultiplier;
        dst.OrgasmArousalResetTo      = src.OrgasmArousalResetTo;

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

        // 射精FatigueとエンドカウントはSharedConfigから固定（状態上書き不可）
        dst.OrgasmFatigueGain = sharedConfig.OrgasmFatigueGain;
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
            dst.DriveChangeStop   = sharedConfig.DriveChangeStop;
            dst.DriveChangeBelow  = sharedConfig.DriveChangeBelow;
            dst.DriveChangeWithin = sharedConfig.DriveChangeWithin;
            dst.DriveChangeAbove  = sharedConfig.DriveChangeAbove;
            dst.DriveChangeDelay  = sharedConfig.DriveChangeDelay;
        }

        if (!src.OverrideDriveBias)
        {
            dst.DriveBiasShiftBelow = sharedConfig.DriveBiasShiftBelow;
            dst.DriveBiasShiftAbove = sharedConfig.DriveBiasShiftAbove;
            dst.DriveBiasDecayWithin = sharedConfig.DriveBiasDecayWithin;
            dst.DriveBiasDecayStop = sharedConfig.DriveBiasDecayStop;
        }

        if (!src.OverrideOrgasm)
        {
            dst.OrgasmThreshold           = sharedConfig.OrgasmThreshold;
            dst.OrgasmThresholdMultiplier = sharedConfig.OrgasmThresholdMultiplier;
            dst.OrgasmArousalResetTo      = sharedConfig.OrgasmArousalResetTo;
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
    }

    bool IsEndState(SimState s) =>
        s == SimState.End_A ||
        s == SimState.End_B ||
        s == SimState.End_C_White ||
        s == SimState.End_C_Overload;

    public void RestartSimulation()
    {
        param.Reset();
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

    // --- Unity側で手動接続が必要な作業（コメント） ---
    // 1. InputHandler を Inspector でアタッチ
    // 2. stateConfigs に SimStateConfig アセットを7つ（Guarded〜BrokenDown）設定
    // 3. OutputDriver.Output の各値を Animator Parameter / Cubism Parameter に接続
    //    → AnimationBlender.cs（別途作成）でOutputを受け取りLive2Dに流す
    // 4. スライダーの OnValueChanged → InputHandler.SetMainIntensity
    // 5. SubA/SubBトグルの OnClick → InputHandler.SetSubA / SetSubB
}
