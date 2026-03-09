// AnimationBridge.cs
// SimulationManager の出力を Animator Parameter / Cubism Parameter に毎フレーム流す
// このコンポーネントを Live2D モデルと同じ GameObject か子 GameObject にアタッチする

using UnityEngine;
using Live2D.Cubism.Core; // Cubism SDK が必要

public class AnimationBridge : MonoBehaviour
{
    [Header("=== 参照 ===")]
    [SerializeField] SimulationManager sim;
    [SerializeField] Animator animator;
    [SerializeField] CubismModel cubismModel;

    [Header("=== SmoothDamp 速度（値が大きいほど追従が速い） ===")]
    [SerializeField] float smoothTime = 0.15f;

    // SmoothDamp用の速度バッファ
    float velBodyTension, velBodyYield, velBreathDepth;
    float velFaceHeat, velEyeFocus, velControlMask;
    float velNeedMotion, velPeakDrive, velAftershock;

    // 現在の補間済み値
    float curBodyTension, curBodyYield, curBreathDepth;
    float curFaceHeat, curEyeFocus, curControlMask;
    float curNeedMotion, curPeakDrive, curAftershock;

    // Cubism パラメータのキャッシュ
    // ※ Cubism Editor 側でこれらの ID を設定しておくこと
    CubismParameter paramFaceHeat;
    CubismParameter paramEyeFocus;
    CubismParameter paramControlMask;
    CubismParameter paramAftershock;

    // Cubism パラメータ ID（Cubism Editor 側の ID と合わせること）
    const string ID_FACE_HEAT     = "ParamFaceHeat";
    const string ID_EYE_FOCUS     = "ParamEyeFocus";
    const string ID_CONTROL_MASK  = "ParamControlMask";
    const string ID_AFTERSHOCK    = "ParamAftershock";

    void Start()
    {
        // Cubism パラメータをキャッシュ
        if (cubismModel != null)
        {
            paramFaceHeat    = cubismModel.Parameters.FindById(ID_FACE_HEAT);
            paramEyeFocus    = cubismModel.Parameters.FindById(ID_EYE_FOCUS);
            paramControlMask = cubismModel.Parameters.FindById(ID_CONTROL_MASK);
            paramAftershock  = cubismModel.Parameters.FindById(ID_AFTERSHOCK);
        }
    }

    void Update()
    {
        if (sim == null) return;

        SimulationOutput o = sim.Output;

        // --- SmoothDamp で補間 ---
        curBodyTension = Mathf.SmoothDamp(curBodyTension, o.BodyTension,  ref velBodyTension,  smoothTime);
        curBodyYield   = Mathf.SmoothDamp(curBodyYield,   o.BodyYield,    ref velBodyYield,    smoothTime);
        curBreathDepth = Mathf.SmoothDamp(curBreathDepth, o.BreathDepth,  ref velBreathDepth,  smoothTime);
        curFaceHeat    = Mathf.SmoothDamp(curFaceHeat,    o.FaceHeat,     ref velFaceHeat,     smoothTime);
        curEyeFocus    = Mathf.SmoothDamp(curEyeFocus,    o.EyeFocus,     ref velEyeFocus,     smoothTime);
        curControlMask = Mathf.SmoothDamp(curControlMask, o.ControlMask,  ref velControlMask,  smoothTime);
        curNeedMotion  = Mathf.SmoothDamp(curNeedMotion,  o.NeedMotion,   ref velNeedMotion,   smoothTime);
        curPeakDrive   = Mathf.SmoothDamp(curPeakDrive,   o.PeakDrive,    ref velPeakDrive,    smoothTime);
        curAftershock  = Mathf.SmoothDamp(curAftershock,  o.Aftershock,   ref velAftershock,   smoothTime * 0.3f); // 余韻は速く

        // --- Animator Parameter に流す（BlendTree / PlaybackSpeed 用） ---
        if (animator != null)
        {
            animator.SetFloat("BodyTension",  curBodyTension);
            animator.SetFloat("BodyYield",    curBodyYield);
            animator.SetFloat("BreathDepth",  curBreathDepth);
            animator.SetFloat("NeedMotion",   curNeedMotion);
            animator.SetFloat("PeakDrive",    curPeakDrive);
            animator.SetFloat("PistonSpeed",  GetPistonSpeed()); // PlaybackSpeed用
        }

        // --- Cubism Parameter に直接流す（表情・目線系） ---
        SetCubismParam(paramFaceHeat,    curFaceHeat);
        SetCubismParam(paramEyeFocus,    curEyeFocus);
        SetCubismParam(paramControlMask, curControlMask);
        SetCubismParam(paramAftershock,  curAftershock);

        // --- 状態遷移トリガー ---
        HandleStateTriggers();
    }

    // 状態に応じた Animator トリガーを叩く
    // SimulationManager.OnStateChanged イベントで受け取る方式に変えてもOK
    SimState previousState;
    void HandleStateTriggers()
    {
        SimState current = sim.State;
        if (current == previousState) return;

        // 状態遷移時にトリガーを叩く
        switch (current)
        {
            case SimState.BrokenDown:
                animator.SetTrigger("BrokenDown");
                break;
            case SimState.End_A:
                animator.SetTrigger("End_A");
                break;
            case SimState.End_B:
                animator.SetTrigger("End_B");
                break;
            case SimState.End_C_White:
                animator.SetTrigger("End_C_White");
                break;
            case SimState.End_C_Overload:
                animator.SetTrigger("End_C_Overload");
                break;
        }

        // BodyBase のステートを切り替える
        animator.SetInteger("StateIndex", (int)current);

        previousState = current;
    }

    // InputHandler から PistonSpeed を取得（PlaybackSpeed に使う）
    // SimulationManager 経由で取得できるよう後で調整してもOK
    float GetPistonSpeed()
    {
        // TODO: InputHandler への参照を SimulationManager 経由で取得する
        // 暫定で 1.0f を返す
        return 1.0f;
    }

    // null チェック付き Cubism パラメータセット
    void SetCubismParam(CubismParameter param, float value)
    {
        if (param != null)
            param.Value = value;
    }
}
