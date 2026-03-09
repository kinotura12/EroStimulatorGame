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

    static readonly (SimState state, string label)[] States =
    {
        (SimState.Guarded,           "① Guarded"),
        (SimState.Defensive,         "② Defensive"),
        (SimState.Overridden,        "③ Overridden"),
        (SimState.FrustratedCraving, "④ Frustrated"),
        (SimState.Acclimating,       "⑤ Acclimating"),
        (SimState.Surrendered,       "⑥ Surrendered"),
        (SimState.BrokenDown,        "⑦ BrokenDown"),
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
            var (state, label) = States[i];
            float x = PanelX + i * (BtnWidth + BtnSpacing);
            var rect = new Rect(x, PanelY, BtnWidth, BtnHeight);

            bool isActive = (state == current);
            GUIStyle style = isActive ? _activeStyle : _normalStyle;

            if (GUI.Button(rect, label, style))
            {
                sim.ForceStateForDebug(state);
            }
        }
    }
}
#else
// ビルド時は空クラスとして残す（参照エラー回避）
public class DebugPanel : UnityEngine.MonoBehaviour { }
#endif
