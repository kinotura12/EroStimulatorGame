// SimulationManager.cs
// MonoBehaviour：全クラスを統括するエントリーポイント
// このコンポーネントをGameObjectにアタッチして使う

using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    [Header("=== 参照 ===")]
    [SerializeField] InputHandler inputHandler;
    [SerializeField] SimStateConfig[] stateConfigs; // enum順に7つ設定

    [Header("=== OutputDriver係数 ===")]
    [SerializeField] OutputDriver.OutputWeights outputWeights = new OutputDriver.OutputWeights();

    [Header("=== 現在の状態（確認用・読み取り専用） ===")]
    [SerializeField] SimState      currentState;
    [SerializeField] SimParameters param = new SimParameters();
    [SerializeField] SimulationOutput output;

    [Header("=== デバッグ ===")]
    [SerializeField] bool debugLogging = false;

    // --- 内部クラス ---
    BandEvaluator     bandEvaluator     = new BandEvaluator();
    ParameterUpdater  parameterUpdater  = new ParameterUpdater();
    StateResolver     stateResolver     = new StateResolver();
    EndJudge          endJudge          = new EndJudge();
    OutputDriver      outputDriver;

    // タイマー類
    float aboveDuration;
    float belowDuration;
    float withinDuration;
    float driveRampTimer;
    float bandFlipTimer;

    InputBand previousBand;
    InputBand currentBand;
    bool justOrgasmed;

    // 外部から出力を取得するプロパティ
    public SimulationOutput Output  => output;
    public SimState         State   => currentState;

    // イベント
    public System.Action<SimState>       OnStateChanged;
    public System.Action                 OnOrgasm;
    public System.Action<SimState>       OnEnding;

    void Awake()
    {
        outputDriver = new OutputDriver(outputWeights);
        param.Reset();
        currentState = SimState.Guarded;
    }

    void Update()
    {
        if (IsEndState(currentState)) return;

        SimStateConfig config = GetConfig(currentState);
        if (config == null)
        {
            Debug.LogError($"SimStateConfig が未設定です: {currentState}");
            return;
        }

        // 1. 入力帯判定
        previousBand = currentBand;
        currentBand  = bandEvaluator.Evaluate(
            inputHandler.MainIntensity,
            inputHandler.IsActive,
            config.TolLow,
            config.TolHigh);

        // 2. パラメータ更新
        justOrgasmed = false;
        parameterUpdater.Update(
            param,
            currentBand,
            inputHandler.SubA,
            inputHandler.SubB,
            config,
            Time.deltaTime,
            ref aboveDuration,
            ref belowDuration,
            ref withinDuration,
            ref driveRampTimer);

        // 3. 射精判定
        if (endJudge.CheckOrgasm(param, config))
        {
            justOrgasmed = true;
            endJudge.OnOrgasm(param);
            OnOrgasm?.Invoke();
        }

        // 4. BrokenDown突入時のモードロック
        SimState previousState = currentState;
        SimState nextState = stateResolver.Resolve(
            currentState, param, currentBand, config,
            aboveDuration, belowDuration, withinDuration,
            ref bandFlipTimer, previousBand);

        if (nextState == SimState.BrokenDown && previousState != SimState.BrokenDown)
            endJudge.LockBrokenDownMode(param);

        // 5. 状態遷移
        if (nextState != currentState)
        {
            if (debugLogging)
                Debug.Log($"[State] {currentState} → {nextState} | Band:{currentBand} | Arousal:{param.Arousal:F2} | Drive:{param.Drive:F2}");

            currentState = nextState;
            OnStateChanged?.Invoke(currentState);

            if (IsEndState(currentState))
                OnEnding?.Invoke(currentState);

            // タイマーリセット
            aboveDuration  = 0f;
            belowDuration  = 0f;
            withinDuration = 0f;
        }

        // 6. 出力計算
        output = outputDriver.Compute(param, justOrgasmed);

        // 7. デバッグログ
        if (debugLogging)
            Debug.Log($"[{currentState}] Band:{currentBand} | Arousal:{param.Arousal:F2} Resistance:{param.Resistance:F2} Fatigue:{param.Fatigue:F2} Drive:{param.Drive:F2} DriveBias:{param.DriveBias:F2}");
    }

    SimStateConfig GetConfig(SimState state)
    {
        int index = (int)state;
        if (index < 0 || index >= stateConfigs.Length) return null;
        return stateConfigs[index];
    }

    bool IsEndState(SimState s) =>
        s == SimState.End_A ||
        s == SimState.End_B ||
        s == SimState.End_C_White ||
        s == SimState.End_C_Overload;

    // --- Unity側で手動接続が必要な作業（コメント） ---
    // 1. InputHandler を Inspector でアタッチ
    // 2. stateConfigs に SimStateConfig アセットを7つ（Guarded〜BrokenDown）設定
    // 3. OutputDriver.Output の各値を Animator Parameter / Cubism Parameter に接続
    //    → AnimationBlender.cs（別途作成）でOutputを受け取りLive2Dに流す
    // 4. スライダーの OnValueChanged → InputHandler.SetMainIntensity
    // 5. SubA/SubBトグルの OnClick → InputHandler.SetSubA / SetSubB
}
