// DebugPanel.cs
// デバッグ用：状態ジャンプボタン（DEVELOPMENT_BUILD または UNITY_EDITOR 時のみ有効）
//
// 【セットアップ手順】
// 1. SimulationRoot（SimulationManager があるオブジェクト）にアタッチ
// 2. Inspector で sim に SimulationManager を設定

using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
public class DebugPanel : MonoBehaviour
{
    [SerializeField] SimulationManager sim;

    // ボタンのレイアウト定数
    const float PanelX      = 10f;
    const float PanelY      = 10f;
    const float BtnWidth    = 130f;
    const float BtnHeight   = 32f;
    const float BtnSpacing  = 6f;

    // (state, label, arousal, resistance, fatigue, drive, driveBias)
    // 各状態の典型的な突入値 — 遷移条件の閾値 + 余裕値を参考に設定
    static readonly (SimState state, string label, float arousal, float resistance, float fatigue, float drive, float driveBias)[] States =
    {
        // ① Guarded   : 初期。Resistance高め、他低め
        (SimState.Guarded,           "① Guarded",     0.20f, 0.75f, 0.10f, 0.10f, 0.0f),
        // ② Defensive : Resistance≥0.75 で突入
        (SimState.Defensive,         "② Defensive",   0.20f, 0.85f, 0.30f, 0.30f, 0.0f),
        // ③ Overridden: Fatigue≥0.7 + Drive≥0.5 で突入
        (SimState.Overridden,        "③ Overridden",  0.50f, 0.55f, 0.75f, 0.60f, 0.0f),
        // ④ Frustrated: Drive≥0.5（お預け欲求）で突入
        (SimState.FrustratedCraving, "④ Frustrated",  0.40f, 0.50f, 0.45f, 0.65f, 0.2f),
        // ⑤ Acclimating: Resistance≤0.35 + Arousal≥0.5 で突入
        (SimState.Acclimating,       "⑤ Acclimating", 0.55f, 0.25f, 0.40f, 0.35f, 0.0f),
        // ⑥ Surrendered: Arousal≥0.65 + Drive≥0.5 で突入
        (SimState.Surrendered,       "⑥ Surrendered", 0.70f, 0.20f, 0.50f, 0.60f, 0.1f),
        // ⑦ BrokenDown: Fatigue≥0.8 + Drive≥0.7 + Arousal≥0.6 で突入
        (SimState.BrokenDown,        "⑦ BrokenDown",  0.72f, 0.10f, 0.85f, 0.75f, 0.2f),
    };

    GUIStyle _activeStyle;
    GUIStyle _normalStyle;

    void OnGUI()
    {
        if (sim == null) return;

        // スタイル初期化（初回のみ）
        if (_activeStyle == null)
        {
            _normalStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Normal,
            };
            _activeStyle = new GUIStyle(_normalStyle)
            {
                fontStyle = FontStyle.Bold,
            };
            _activeStyle.normal.textColor  = Color.yellow;
            _activeStyle.hover.textColor   = Color.yellow;
        }

        SimState current = sim.State;

        for (int i = 0; i < States.Length; i++)
        {
            var (state, label, arousal, resistance, fatigue, drive, driveBias) = States[i];
            float y = PanelY + i * (BtnHeight + BtnSpacing);
            var rect = new Rect(PanelX, y, BtnWidth, BtnHeight);

            bool isActive = (state == current);
            GUIStyle style = isActive ? _activeStyle : _normalStyle;

            if (GUI.Button(rect, label, style))
            {
                sim.ForceStateWithPreset(state, arousal, resistance, fatigue, drive, driveBias);
            }
        }
    }
}
#else
// ビルド時は空クラスとして残す（参照エラー回避）
public class DebugPanel : UnityEngine.MonoBehaviour { }
#endif
