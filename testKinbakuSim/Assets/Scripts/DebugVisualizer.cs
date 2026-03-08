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
    [SerializeField] RectTransform charaRect;        // キャラ代わりの四角
    [SerializeField] Image          charaImage;      // 四角の色制御用
    [SerializeField] Slider         pistonSlider;
    [SerializeField] Toggle         subAToggle;
    [SerializeField] Toggle         subBToggle;
    [SerializeField] Text           stateLabel;
    [SerializeField] Text           endLabel;
    [SerializeField] Text           charaParamLabel; // キャラパラメータ表示（メイン）
    [SerializeField] Text           paramLabel;      // デバッグ用出力パラメータ表示
    [SerializeField] Button         restartButton;
    [SerializeField] Image          zoneLow;         // 閾値ゾーン：Below（赤）
    [SerializeField] Image          zoneWithin;      // 閾値ゾーン：Within（緑）
    [SerializeField] Image          zoneHigh;        // 閾値ゾーン：Above（オレンジ）
    [SerializeField] Image          subAIndicator;   // SubA：ペニス位置インジケーター
    [SerializeField] Image          subBIndicator;   // SubB：乳首位置インジケーター

    [Header("=== 四角の動き設定 ===")]
    [SerializeField] float bodyYieldScale = 80f;   // BodyYield に応じた上下幅
    [SerializeField] float shakeScale     = 20f;   // Aftershock の震え幅
    [SerializeField] float needMotionSwayScale = 26f; // NeedMotion に応じた左右揺れ幅
    [SerializeField] float needMotionSwayMinHz = 0.8f;
    [SerializeField] float needMotionSwayMaxHz = 3.5f;
    [SerializeField] float rejectSwayScale = 36f; // RejectOffsetX に応じた左右イヤイヤ幅
    [SerializeField] float pistonAmplitude = 45f;  // ピストン上下の振幅
    [SerializeField] float minPistonHz     = 0.8f; // intensity=0 のときの速度
    [SerializeField] float maxPistonHz     = 5.0f; // intensity=1 のときの速度
    [SerializeField] float idleBreathMinScaleAmount = 0.008f; // BreathDepth=0でも残す呼吸振幅
    [SerializeField] float idleBreathScaleAmount = 0.03f; // アイドリング時の呼吸伸縮量
    [SerializeField] float idleBreathMinHz       = 0.4f;  // BreathDepth=0 の呼吸速度
    [SerializeField] float idleBreathMaxHz       = 1.5f;  // BreathDepth=1 の呼吸速度

    [Header("=== 色設定 ===")]
    [SerializeField] Color colorCalm    = new Color(0.5f, 0.6f, 0.8f); // 青系（冷静）
    [SerializeField] Color colorAroused = new Color(0.9f, 0.4f, 0.4f); // 赤系（興奮）
    [SerializeField] Color colorBroken  = new Color(1.0f, 0.2f, 0.6f); // ピンク（崩壊）

    // 内部
    Vector2 basePosition;
    float   shakeTimer;
    float   needMotionSwayPhase;
    float   pistonPhase;
    float   idleBreathPhase;
    Vector2 subABasePos;
    Vector2 subBBasePos;
    float   subAVibPhase;
    float   subBVibPhase;

    void OnEnable()
    {
        if (sim != null)
            sim.OnEnding += HandleEnding;
    }

    void OnDisable()
    {
        if (sim != null)
            sim.OnEnding -= HandleEnding;
    }

    void Start()
    {
        if (charaRect != null)
            basePosition = charaRect.anchoredPosition;

        if (subAIndicator != null) subABasePos = subAIndicator.rectTransform.anchoredPosition;
        if (subBIndicator != null) subBBasePos = subBIndicator.rectTransform.anchoredPosition;

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

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(HandleRestartButton);
            restartButton.gameObject.SetActive(false);
        }

        if (endLabel != null)
            endLabel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (sim == null) return;

        SimulationOutput o = sim.Output;
        SimState state      = sim.State;

        UpdateCharaRect(o);
        UpdateColor(o, state);
        UpdateLabels(o, state);
        UpdateThresholdBar();
        UpdateSubIndicators();
    }

    // --- 四角の位置・大きさ ---
    void UpdateCharaRect(SimulationOutput o)
    {
        if (charaRect == null) return;

        // 上下移動：BodyYield が高いほど下に沈む（腰が落ちる表現）
        float yOffset = -o.BodyYield * bodyYieldScale;

        // ピストン上下：メインスライダーの値を速度(Hz)に変換して Sin 波で動かす
        float intensity = 0f;
        bool isActive = false;
        if (inputHandler != null)
        {
            intensity = inputHandler.MainIntensity;
            isActive = inputHandler.IsActive;
        }

        float pistonY = 0f;
        if (isActive && intensity > 0f)
        {
            float hz = Mathf.Lerp(minPistonHz, maxPistonHz, intensity);
            pistonPhase += Time.deltaTime * hz * Mathf.PI * 2f;

            // 速度が上がると動き幅も少し大きく見えるようにする
            float amp = pistonAmplitude * Mathf.Lerp(0.45f, 1.0f, intensity);
            pistonY = Mathf.Sin(pistonPhase) * amp;
        }
        else
        {
            pistonPhase = 0f;
        }

        // 震え：Aftershock が高いほどランダムに震える。OrgasmScaleで振幅を増幅
        float shakeX = 0f;
        float shakeY = 0f;
        if (o.Aftershock > 0.05f)
        {
            float shakeAmp = shakeScale * Mathf.Lerp(1f, 4f, o.OrgasmScale);
            shakeTimer += Time.deltaTime * Mathf.Lerp(30f, 60f, o.OrgasmScale);
            shakeX = Mathf.Sin(shakeTimer * 1.3f) * o.Aftershock * shakeAmp;
            shakeY = Mathf.Sin(shakeTimer * 1.7f) * o.Aftershock * shakeAmp;
        }
        else
        {
            shakeTimer = 0f;
        }

        // 左右スウェイ：NeedMotion が高いほど腰振りの幅・速度が上がる
        float swayX = 0f;
        if (o.NeedMotion > 0.02f)
        {
            float swayHz = Mathf.Lerp(needMotionSwayMinHz, needMotionSwayMaxHz, o.NeedMotion);
            needMotionSwayPhase += Time.deltaTime * swayHz * Mathf.PI * 2f;
            swayX = Mathf.Sin(needMotionSwayPhase) * (needMotionSwayScale * o.NeedMotion);
        }

        // 拒否モーション（Resistance/入力変化率ベース）
        float rejectX = 0f;
        if (sim != null)
            rejectX = sim.RejectOffsetX * rejectSwayScale;

        charaRect.anchoredPosition = basePosition + new Vector2(swayX + rejectX + shakeX, yOffset + pistonY + shakeY);

        // 大きさ：BodyTension が高いほど縮む（緊張で体が固まる表現）
        float tension = o.BodyTension;
        float scaleX = Mathf.Lerp(1.0f, 0.85f, tension);
        float scaleY = Mathf.Lerp(1.0f, 1.10f, tension); // 縦に少し伸びる

        // 呼吸：常時。BreathDepth が高いほど呼吸が大きくなる
        {
            float breathHz = Mathf.Lerp(idleBreathMinHz, idleBreathMaxHz, Mathf.Clamp01(o.BreathDepth));
            idleBreathPhase += Time.deltaTime * breathHz * Mathf.PI * 2f;
            float breathAmp = Mathf.Lerp(
                Mathf.Max(0f, idleBreathMinScaleAmount),
                Mathf.Max(0f, idleBreathScaleAmount),
                Mathf.Clamp01(o.BreathDepth));
            float breathWave = Mathf.Sin(idleBreathPhase) * breathAmp;
            scaleY *= (1f + breathWave);
            scaleX *= (1f - breathWave * 0.35f);
        }

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

        // 射精フラッシュ：OrgasmScaleが高いほど白く大きく光る
        if (o.Aftershock > 0.05f)
        {
            float flash = o.Aftershock * o.OrgasmScale;
            charaImage.color = Color.Lerp(charaImage.color, Color.white, flash * 0.75f);
        }
    }

    // --- テキスト表示 ---
    void UpdateLabels(SimulationOutput o, SimState state)
    {
        if (stateLabel != null)
            stateLabel.text = $"State: {state}";

        // キャラパラメータ（メイン表示）
        if (charaParamLabel != null && sim != null)
        {
            SimParameters p = sim.Param;
            // EdgePeakTimerのプログレス表示（維持中は [▶▶▶__] で進行を見せる）
            string peakProgress = "";
            if (p.EdgeTension >= 1f)
            {
                int filled = Mathf.Clamp(Mathf.RoundToInt(p.EdgePeakTimer / Mathf.Max(0.01f, sim.CurrentEdgePeakHoldDuration) * 2f), 0, 2);
                peakProgress = $" [{new string('>', filled)}{new string('_', 2 - filled)}]";
            }
            charaParamLabel.text =
                $"Arousal    {Bar(p.Arousal)}  {p.Arousal:F2}\n" +
                $"  射精欲   {SmallBar(p.EdgeTension)} {p.EdgeTension:F2}{peakProgress}\n" +
                $"Resistance {Bar(p.Resistance)}  {p.Resistance:F2}\n" +
                $"Fatigue    {Bar(p.Fatigue)}  {p.Fatigue:F2}\n" +
                $"Drive      {Bar(p.Drive)}  {p.Drive:F2}\n" +
                $"DriveBias  {BiasBar(p.DriveBias)}  {p.DriveBias:F2}";
        }

        // デバッグ用出力パラメータ（サブ表示）
        if (paramLabel != null)
        {
            float bandSec = sim.StopDuration;
            if (sim.CurrentBand == InputBand.Above) bandSec = sim.AboveDuration;
            else if (sim.CurrentBand == InputBand.Below) bandSec = sim.BelowDuration;
            else if (sim.CurrentBand == InputBand.Within) bandSec = sim.WithinDuration;
            paramLabel.text =
                $"[debug]\n" +
                $"BodyTension {Bar(o.BodyTension)} {o.BodyTension:F2}\n" +
                $"BodyYield   {Bar(o.BodyYield)} {o.BodyYield:F2}\n" +
                $"BreathDepth {Bar(o.BreathDepth)} {o.BreathDepth:F2}\n" +
                $"FaceHeat    {Bar(o.FaceHeat)} {o.FaceHeat:F2}\n" +
                $"EyeFocus    {Bar(o.EyeFocus)} {o.EyeFocus:F2}\n" +
                $"ControlMask {Bar(o.ControlMask)} {o.ControlMask:F2}\n" +
                $"NeedMotion  {Bar(o.NeedMotion)} {o.NeedMotion:F2}\n" +
                $"PeakDrive   {Bar(o.PeakDrive)} {o.PeakDrive:F2}\n" +
                $"Aftershock  {Bar(o.Aftershock)} {o.Aftershock:F2}\n" +
                $"RejectMove  {Bar(o.RejectMotion)} {o.RejectMotion:F2}\n" +
                $"RejectHab   {Bar(o.RejectHabituation)} {o.RejectHabituation:F2}\n" +
                $"RejectRate  {sim.RejectTriggerRate:F2}/s\n" +
                $"EdgePeak    {sim.Param.EdgePeakTimer:F1}s  DwellTime {sim.Param.EdgeDwellTime:F1}s\n" +
                $"Cumulative  {Bar(o.CumulativeOrgasm)} {o.CumulativeOrgasm:F2}\n" +
                $"mainActive  {(inputHandler != null && inputHandler.IsActive ? 1 : 0)}\n" +
                $"Band        {sim.CurrentBand} ({bandSec:F1}s)";
        }
    }

    // テキストのバー表示（0.0～1.0 を10段階で表示）
    string Bar(float value)
    {
        int filled = Mathf.RoundToInt(Mathf.Clamp01(value) * 10f);
        return "[" + new string('#', filled) + new string('-', 10 - filled) + "]";
    }

    // 小さいバー表示（0.0～1.0 を5段階で表示）
    string SmallBar(float value)
    {
        int filled = Mathf.RoundToInt(Mathf.Clamp01(value) * 5f);
        return "[" + new string('#', filled) + new string('-', 5 - filled) + "]";
    }

    // --- SubA/B インジケーター振動 ---
    void UpdateSubIndicators()
    {
        if (inputHandler == null) return;
        // SubA：上下振動（挿入刺激）
        UpdateSubVib(subAIndicator, inputHandler.SubA, ref subAVibPhase, ref subABasePos, vertical: true);
        // SubB：左右振動（乳首刺激）
        UpdateSubVib(subBIndicator, inputHandler.SubB, ref subBVibPhase, ref subBBasePos, vertical: false);
    }

    void UpdateSubVib(Image indicator, bool active, ref float phase, ref Vector2 basePos, bool vertical)
    {
        if (indicator == null) return;
        if (active)
        {
            phase += Time.deltaTime * 60f;
            float vib = Mathf.Sin(phase) * 3.5f;
            indicator.rectTransform.anchoredPosition = basePos + (vertical ? new Vector2(0f, vib) : new Vector2(vib, 0f));
            indicator.color = new Color(1f, 0.55f, 0.1f, 1f); // アクティブ：オレンジ
        }
        else
        {
            phase = 0f;
            indicator.rectTransform.anchoredPosition = basePos;
            indicator.color = new Color(0.35f, 0.35f, 0.35f, 0.9f); // 非アクティブ：グレー
        }
    }

    // --- 閾値ゾーンバー更新 ---
    void UpdateThresholdBar()
    {
        if (sim == null) return;
        float lo = sim.CurrentTolLow;
        float hi = sim.CurrentTolHigh;

        if (zoneLow    != null) SetZoneAnchors(zoneLow.rectTransform,    0f, lo);
        if (zoneWithin != null) SetZoneAnchors(zoneWithin.rectTransform, lo, hi);
        if (zoneHigh   != null) SetZoneAnchors(zoneHigh.rectTransform,   hi, 1f);
    }

    void SetZoneAnchors(RectTransform rt, float xMin, float xMax)
    {
        rt.anchorMin = new Vector2(xMin, 0f);
        rt.anchorMax = new Vector2(xMax, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // DriveBias 用バー表示（-1.0～1.0 を中央基準で表示）
    string BiasBar(float value)
    {
        value = Mathf.Clamp(value, -1f, 1f);
        int center = 5;
        int pos = Mathf.RoundToInt((value + 1f) * 5f); // 0〜10
        char[] bar = new string('-', 11).ToCharArray();
        bar[center] = '|'; // 中央マーク
        if (pos != center)
        {
            int lo = Mathf.Min(pos, center);
            int hi = Mathf.Max(pos, center);
            for (int i = lo; i <= hi; i++) if (bar[i] != '|') bar[i] = '=';
        }
        return "[" + new string(bar) + "]";
    }

    void HandleEnding(SimState endState)
    {
        if (endLabel != null)
        {
            endLabel.text = $"END: {GetEndingName(endState)}";
            endLabel.gameObject.SetActive(true);
        }

        if (restartButton != null)
            restartButton.gameObject.SetActive(true);
    }

    void HandleRestartButton()
    {
        if (sim != null)
            sim.RestartSimulation();

        if (pistonSlider != null)
            pistonSlider.SetValueWithoutNotify(0f);

        if (subAToggle != null)
            subAToggle.SetIsOnWithoutNotify(false);

        if (subBToggle != null)
            subBToggle.SetIsOnWithoutNotify(false);

        if (endLabel != null)
            endLabel.gameObject.SetActive(false);

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);

        if (stateLabel != null && sim != null)
            stateLabel.text = $"State: {sim.State}";
    }

    string GetEndingName(SimState state)
    {
        switch (state)
        {
            case SimState.End_A:          return "グッタリエンド";
            case SimState.End_B:          return "快楽落ちエンド";
            case SimState.End_C_White:    return "とろけ落ちエンド";
            case SimState.End_C_Overload: return "アヘ顔崩壊エンド";
            default:                      return state.ToString();
        }
    }
}
