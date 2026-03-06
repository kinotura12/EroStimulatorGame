// StateTransitionConfigEditor.cs
// StateTransitionConfig の Inspector 表示：プレビュー一覧 + 通常エディタ

using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StateTransitionConfig))]
public class StateTransitionConfigEditor : Editor
{
    static readonly string[] StateLabels =
    {
        "① Guarded", "② Defensive", "③ Overridden",
        "④ FrustratedCraving", "⑤ Acclimating", "⑥ Surrendered", "⑦ BrokenDown",
        "End_A", "End_B", "End_C_White", "End_C_Overload"
    };
    static readonly string[] ParamLabels = { "Arousal", "Resistance", "Fatigue", "Drive" };
    static readonly string[] OpLabels    = { ">=", "<=", ">", "<" };
    static readonly string[] BandLabels  = { "Any", "Stop", "Below", "Within", "Above" };

    bool showPreview = true;

    public override void OnInspectorGUI()
    {
        var config = (StateTransitionConfig)target;

        // === プレビュー ===
        showPreview = EditorGUILayout.Foldout(showPreview, "=== 遷移ルール一覧（プレビュー）", true, EditorStyles.foldoutHeader);
        if (showPreview)
            DrawPreview(config);

        EditorGUILayout.Space(10);

        // === 編集リスト ===
        EditorGUILayout.LabelField("=== ルール編集 ===", EditorStyles.boldLabel);
        DrawDefaultInspector();
    }

    void DrawPreview(StateTransitionConfig config)
    {
        if (config.rules == null || config.rules.Count == 0)
        {
            EditorGUILayout.HelpBox("ルールがありません", MessageType.Info);
            return;
        }

        SimState? prevFrom = null;
        foreach (var rule in config.rules)
        {
            // 状態の区切りヘッダー
            if (rule.fromState != prevFrom)
            {
                EditorGUILayout.Space(4);
                prevFrom = rule.fromState;
                int idx = (int)rule.fromState;
                string header = idx < StateLabels.Length ? StateLabels[idx] : rule.fromState.ToString();
                EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            }

            // ルールの1行サマリー
            int toIdx     = (int)rule.toState;
            string toLabel = toIdx < StateLabels.Length ? StateLabels[toIdx] : rule.toState.ToString();
            string summary = BuildSummary(rule);

            var style = new GUIStyle(EditorStyles.miniLabel);
            if (!rule.enabled)
            {
                style.normal.textColor = Color.gray;
                EditorGUILayout.LabelField($"  [OFF] → {toLabel}: {summary}", style);
            }
            else
            {
                EditorGUILayout.LabelField($"  → {toLabel}: {summary}", style);
            }
        }
    }

    string BuildSummary(TransitionRule rule)
    {
        var sb = new StringBuilder();

        // Band条件
        if (rule.requiredBand != BandRequirement.Any)
        {
            string band = BandLabels[(int)rule.requiredBand];
            sb.Append(rule.bandDuration > 0f
                ? $"Band={band} {rule.bandDuration}s"
                : $"Band={band}");
        }

        // パラメータ条件
        if (rule.conditions != null)
        {
            foreach (var c in rule.conditions)
            {
                if (sb.Length > 0) sb.Append(" & ");
                string p  = (int)c.param < ParamLabels.Length ? ParamLabels[(int)c.param] : c.param.ToString();
                string op = (int)c.op    < OpLabels.Length    ? OpLabels[(int)c.op]       : "?";
                sb.Append($"{p}{op}{c.threshold:F2}");
            }
        }

        // メモ
        if (!string.IsNullOrEmpty(rule.note))
        {
            if (sb.Length > 0) sb.Append("  // ");
            sb.Append(rule.note);
        }

        return sb.Length > 0 ? sb.ToString() : "(常時)";
    }
}
