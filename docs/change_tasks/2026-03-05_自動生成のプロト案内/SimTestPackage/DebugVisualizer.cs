// DebugVisualizer.cs
// テスト用シーン：四角がキャラの代わりに動く・色が変わる
// SimulationManager と同じ GameObject にアタッチ、またはシーン上の任意の場所に置く
//
// 【セットアップ手順】
// 1. 空のシーンに空のGameObjectを作り "SimulationRoot" と名付ける
// 2. SimulationRoot に以下をアタッチ：
//      - SimulationManager.cs
//      - InputHandler.cs
//      - DebugVisualizer.cs
// 3. Hierarchy に UI > Canvas を作り、その下に：
//      - Image（名前: CharaRect）     ← キャラの代わりの四角
//      - Slider（名前: PistonSlider） ← ピストン強度
//      - Toggle（名前: SubAToggle）
//      - Toggle（名前: SubBToggle）
//      - Text（名前: StateLabel）     ← 現在の状態表示
// 4. Inspector でそれぞれの参照をアタッチする

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DebugVisualizer : MonoBehaviour
{
    [Header("=== 参照：SimulationManager ===")]
    [SerializeField] SimulationManager sim;
    [SerializeField] InputHandler inputHandler;

    [Header("=== 参照：UI ===")]
    [SerializeField] RectTransform charaRect;   // キャラ代わりの四角
    [SerializeField] Image          charaImage; // 四角の色制御用
    [SerializeField] Slider         pistonSlider;
    [SerializeField] Toggle         subAToggle;
    [SerializeField] Toggle         subBToggle;
    [SerializeField] Text           stateLabel;
    [SerializeField] Text           paramLabel; // パラメータ数値表示

    [Header("=== 四角の動き設定 ===")]
    [SerializeField] float baseY          = 0f;    // 基本Y座標
    [SerializeField] float bodyYieldScale = 80f;   // BodyYield に応じた上下幅
    [SerializeField] float shakeScale     = 20f;   // Aftershock の震え幅

    [Header("=== 色設定 ===")]
    [SerializeField] Color colorCalm    = new Color(0.5f, 0.6f, 0.8f); // 青系（冷静）
    [SerializeField] Color colorAroused = new Color(0.9f, 0.4f, 0.4f); // 赤系（興奮）
    [SerializeField] Color colorBroken  = new Color(1.0f, 0.2f, 0.6f); // ピンク（崩壊）

    // 内部
    Vector2 basePosition;
    float   shakeTimer;

    void Start()
    {
        if (charaRect != null)
            basePosition = charaRect.anchoredPosition;

        // UI イベント接続
        if (pistonSlider != null)
            pistonSlider.onValueChanged.AddListener(inputHandler.SetMainIntensity);

        if (subAToggle != null)
            subAToggle.onValueChanged.AddListener(inputHandler.SetSubA);

        if (subBToggle != null)
            subBToggle.onValueChanged.AddListener(inputHandler.SetSubB);

        // 停止ボタン代わり：スライダーが0になったら Stop
        if (pistonSlider != null)
            pistonSlider.onValueChanged.AddListener(v => { if (v <= 0f) inputHandler.SetStop(); });
    }

    void Update()
    {
        if (sim == null) return;

        SimulationOutput o = sim.Output;
        SimState state      = sim.State;

        UpdateCharaRect(o);
        UpdateColor(o, state);
        UpdateLabels(o, state);
    }

    // --- 四角の位置・大きさ ---
    void UpdateCharaRect(SimulationOutput o)
    {
        if (charaRect == null) return;

        // 上下移動：BodyYield が高いほど下に沈む（腰が落ちる表現）
        float yOffset = -o.BodyYield * bodyYieldScale;

        // 震え：Aftershock が高いほどランダムに震える
        float shakeX = 0f;
        float shakeY = 0f;
        if (o.Aftershock > 0.05f)
        {
            shakeTimer += Time.deltaTime * 30f;
            shakeX = Mathf.Sin(shakeTimer * 1.3f) * o.Aftershock * shakeScale;
            shakeY = Mathf.Sin(shakeTimer * 1.7f) * o.Aftershock * shakeScale;
        }
        else
        {
            shakeTimer = 0f;
        }

        charaRect.anchoredPosition = basePosition + new Vector2(shakeX, yOffset + shakeY);

        // 大きさ：BodyTension が高いほど縮む（緊張で体が固まる表現）
        float tension = o.BodyTension;
        float scaleX = Mathf.Lerp(1.0f, 0.85f, tension);
        float scaleY = Mathf.Lerp(1.0f, 1.10f, tension); // 縦に少し伸びる
        charaRect.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    // --- 色変化 ---
    void UpdateColor(SimulationOutput o, SimState state)
    {
        if (charaImage == null) return;

        Color targetColor;

        if (state == SimState.BrokenDown ||
            state == SimState.End_C_White ||
            state == SimState.End_C_Overload)
        {
            targetColor = colorBroken;
        }
        else
        {
            // Arousal に応じて calm → aroused をブレンド
            float t = o.FaceHeat;
            targetColor = Color.Lerp(colorCalm, colorAroused, t);
        }

        // なめらかに色を変える
        charaImage.color = Color.Lerp(charaImage.color, targetColor, Time.deltaTime * 3f);
    }

    // --- テキスト表示 ---
    void UpdateLabels(SimulationOutput o, SimState state)
    {
        if (stateLabel != null)
            stateLabel.text = $"State: {state}";

        if (paramLabel != null)
        {
            // SimulationOutput の値を全部表示
            paramLabel.text =
                $"BodyTension  {Bar(o.BodyTension)}  {o.BodyTension:F2}\n" +
                $"BodyYield    {Bar(o.BodyYield)}  {o.BodyYield:F2}\n" +
                $"BreathDepth  {Bar(o.BreathDepth)}  {o.BreathDepth:F2}\n" +
                $"FaceHeat     {Bar(o.FaceHeat)}  {o.FaceHeat:F2}\n" +
                $"EyeFocus     {Bar(o.EyeFocus)}  {o.EyeFocus:F2}\n" +
                $"ControlMask  {Bar(o.ControlMask)}  {o.ControlMask:F2}\n" +
                $"NeedMotion   {Bar(o.NeedMotion)}  {o.NeedMotion:F2}\n" +
                $"PeakDrive    {Bar(o.PeakDrive)}  {o.PeakDrive:F2}\n" +
                $"Aftershock   {Bar(o.Aftershock)}  {o.Aftershock:F2}";
        }
    }

    // テキストのバー表示（0.0～1.0 を10段階で表示）
    string Bar(float value)
    {
        int filled = Mathf.RoundToInt(Mathf.Clamp01(value) * 10f);
        return "[" + new string('█', filled) + new string('░', 10 - filled) + "]";
    }
}
