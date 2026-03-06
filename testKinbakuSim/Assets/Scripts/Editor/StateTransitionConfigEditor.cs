// StateTransitionConfigEditor.cs
// StateTransitionConfig のカスタムInspector（日本語UI・文章形式）

using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StateTransitionConfig))]
public class StateTransitionConfigEditor : Editor
{
    // --- 表示ラベル（日本語）---
    static readonly string[] StateLabels =
    {
        "① Guarded", "② Defensive", "③ Overridden",
        "④ FrustratedCraving", "⑤ Acclimating", "⑥ Surrendered", "⑦ BrokenDown",
        "End_A", "End_B", "End_C_White", "End_C_Overload"
    };
    static readonly string[] BandLabels  = { "問わない", "停止中(Stop)", "Below帯", "Within帯", "Above帯" };
    static readonly string[] ParamLabels = { "Arousal", "Resistance", "Fatigue", "Drive", "射精回数", "DriveBias", "崩壊モード" };
    static readonly string[] OpLabels    = { "以上", "以下", "超える", "未満" };

    // プレビュー用（短縮）
    static readonly string[] OpShort = { ">=", "<=", ">", "<" };

    bool showPreview = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var config = (StateTransitionConfig)target;

        // === プレビュー ===
        showPreview = EditorGUILayout.Foldout(showPreview, "=== 遷移一覧（読み取り専用）", true, EditorStyles.foldoutHeader);
        if (showPreview)
            DrawPreview(config);

        EditorGUILayout.Space(10);

        // === ルール編集 ===
        EditorGUILayout.LabelField("=== ルール編集 ===", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("ルールは上から順に評価され、最初にマッチしたものが適用されます。", MessageType.None);
        EditorGUILayout.Space(4);

        SerializedProperty rulesProperty = serializedObject.FindProperty("rules");
        DrawRulesEditor(rulesProperty);

        serializedObject.ApplyModifiedProperties();
    }

    // ===================================
    //  プレビュー（読み取り専用・1行サマリー）
    // ===================================

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
            if (rule.fromState != prevFrom)
            {
                EditorGUILayout.Space(4);
                prevFrom = rule.fromState;
                int idx = (int)rule.fromState;
                string header = idx < StateLabels.Length ? StateLabels[idx] : rule.fromState.ToString();
                EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            }

            int toIdx     = (int)rule.toState;
            string toLabel = toIdx < StateLabels.Length ? StateLabels[toIdx] : rule.toState.ToString();
            string summary = BuildPreviewSummary(rule);

            var style = new GUIStyle(EditorStyles.miniLabel);
            if (!rule.enabled) style.normal.textColor = Color.gray;
            string prefix = rule.enabled ? "  → " : "  [OFF] → ";
            EditorGUILayout.LabelField($"{prefix}{toLabel}  {summary}", style);
        }
    }

    string BuildPreviewSummary(TransitionRule rule)
    {
        var sb = new StringBuilder();
        if (rule.requiredBand != BandRequirement.Any)
        {
            string band = (int)rule.requiredBand < BandLabels.Length ? BandLabels[(int)rule.requiredBand] : "?";
            sb.Append(rule.bandDuration > 0f ? $"{band} {rule.bandDuration}秒以上" : band);
        }
        if (rule.conditions != null)
        {
            foreach (var c in rule.conditions)
            {
                if (sb.Length > 0) sb.Append(" & ");
                string p  = (int)c.param < ParamLabels.Length ? ParamLabels[(int)c.param] : "?";
                string op = (int)c.op    < OpShort.Length     ? OpShort[(int)c.op]         : "?";
                string threshStr = (int)c.param == 4 ? $"{(int)c.threshold}回"
                                 : (int)c.param == 6 ? $"{(int)c.threshold}"
                                 : $"{c.threshold:F2}";
                sb.Append($"{p}{op}{threshStr}");
            }
        }
        return sb.Length > 0 ? $"（{sb}）" : "（常時）";
    }

    // ===================================
    //  ルール編集UI
    // ===================================

    void DrawRulesEditor(SerializedProperty rules)
    {
        SimState? prevFrom = null;
        int toDelete   = -1;
        int toMoveUp   = -1;
        int toMoveDown = -1;

        for (int i = 0; i < rules.arraySize; i++)
        {
            SerializedProperty rule         = rules.GetArrayElementAtIndex(i);
            SerializedProperty fromStateProp = rule.FindPropertyRelative("fromState");
            SimState fromState = (SimState)Mathf.Clamp(fromStateProp.intValue, 0, StateLabels.Length - 1);

            // 状態ヘッダー
            if (fromState != prevFrom)
            {
                EditorGUILayout.Space(8);
                prevFrom = fromState;
                string header = (int)fromState < StateLabels.Length ? StateLabels[(int)fromState] : fromState.ToString();
                EditorGUILayout.LabelField($"━━ {header} ━━━━━━━━━━━━━━━━━━━━━━", EditorStyles.boldLabel);
            }

            DrawOneRule(rule, i, rules.arraySize,
                out bool del, out bool up, out bool down);
            if (del)  toDelete   = i;
            if (up)   toMoveUp   = i;
            if (down) toMoveDown = i;
        }

        // 操作の反映（ループ外）
        if (toDelete   >= 0) rules.DeleteArrayElementAtIndex(toDelete);
        if (toMoveUp   >  0) rules.MoveArrayElement(toMoveUp,   toMoveUp   - 1);
        if (toMoveDown >= 0 && toMoveDown < rules.arraySize - 1)
            rules.MoveArrayElement(toMoveDown, toMoveDown + 1);

        // ルール追加ボタン
        EditorGUILayout.Space(10);
        if (GUILayout.Button("＋  ルールを追加", GUILayout.Height(28)))
        {
            rules.arraySize++;
            // 新要素のデフォルトをセット
            SerializedProperty newRule = rules.GetArrayElementAtIndex(rules.arraySize - 1);
            newRule.FindPropertyRelative("enabled").boolValue    = true;
            newRule.FindPropertyRelative("note").stringValue     = "";
            newRule.FindPropertyRelative("fromState").intValue   = 0;
            newRule.FindPropertyRelative("toState").intValue     = 1;
            newRule.FindPropertyRelative("requiredBand").intValue = 0;
            newRule.FindPropertyRelative("bandDuration").floatValue = 0f;
            newRule.FindPropertyRelative("conditions").arraySize = 0;
        }
    }

    void DrawOneRule(SerializedProperty rule, int index, int total,
        out bool deleteThis, out bool moveUp, out bool moveDown)
    {
        deleteThis = false;
        moveUp     = false;
        moveDown   = false;

        SerializedProperty enabledProp    = rule.FindPropertyRelative("enabled");
        SerializedProperty fromStateProp  = rule.FindPropertyRelative("fromState");
        SerializedProperty toStateProp    = rule.FindPropertyRelative("toState");
        SerializedProperty bandProp       = rule.FindPropertyRelative("requiredBand");
        SerializedProperty durationProp   = rule.FindPropertyRelative("bandDuration");
        SerializedProperty conditionsProp = rule.FindPropertyRelative("conditions");
        SerializedProperty noteProp       = rule.FindPropertyRelative("note");

        // --- ボックス開始 ---
        var boxStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(6, 6, 4, 4) };
        EditorGUILayout.BeginVertical(boxStyle);

        // --- ヘッダー行: [✓] [From▼] → [To▼]  [▲][▼][×] ---
        EditorGUILayout.BeginHorizontal();

        bool enabled = enabledProp.boolValue;
        enabledProp.boolValue = EditorGUILayout.Toggle(enabled, GUILayout.Width(18));

        // From State（小さめ）
        int fromIdx = Mathf.Clamp(fromStateProp.intValue, 0, StateLabels.Length - 1);
        fromStateProp.intValue = EditorGUILayout.Popup(fromIdx, StateLabels, GUILayout.MinWidth(130));

        EditorGUILayout.LabelField("→", GUILayout.Width(18));

        // To State
        int toIdx = Mathf.Clamp(toStateProp.intValue, 0, StateLabels.Length - 1);
        toStateProp.intValue = EditorGUILayout.Popup(toIdx, StateLabels, GUILayout.MinWidth(130));

        GUILayout.FlexibleSpace();

        // 並べ替え・削除ボタン
        GUI.enabled = index > 0;
        if (GUILayout.Button("▲", GUILayout.Width(24))) moveUp = true;
        GUI.enabled = index < total - 1;
        if (GUILayout.Button("▼", GUILayout.Width(24))) moveDown = true;
        GUI.enabled = true;
        if (GUILayout.Button("×", GUILayout.Width(24))) deleteThis = true;

        EditorGUILayout.EndHorizontal();

        // --- 無効時は本文スキップ ---
        if (!enabledProp.boolValue)
        {
            EditorGUILayout.LabelField("（このルールは無効です）", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
            return;
        }

        EditorGUILayout.Space(2);

        // --- Band継続条件行 ---
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Band :", GUILayout.Width(40));

        int bandIdx = Mathf.Clamp(bandProp.intValue, 0, BandLabels.Length - 1);
        bandProp.intValue = EditorGUILayout.Popup(bandIdx, BandLabels, GUILayout.MinWidth(100));

        if (bandProp.intValue != 0) // 「問わない」以外
        {
            EditorGUILayout.LabelField("が", GUILayout.Width(16));
            durationProp.floatValue = Mathf.Max(0f,
                EditorGUILayout.FloatField(durationProp.floatValue, GUILayout.Width(48)));
            EditorGUILayout.LabelField("秒以上", GUILayout.Width(44));
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // --- パラメータ条件行（複数）---
        int toDeleteCond = -1;
        for (int ci = 0; ci < conditionsProp.arraySize; ci++)
        {
            SerializedProperty cond      = conditionsProp.GetArrayElementAtIndex(ci);
            SerializedProperty paramProp = cond.FindPropertyRelative("param");
            SerializedProperty opProp    = cond.FindPropertyRelative("op");
            SerializedProperty threshProp = cond.FindPropertyRelative("threshold");

            EditorGUILayout.BeginHorizontal();

            // 「条件:」or「かつ」
            string condLabel = ci == 0 ? "条件 :" : "かつ  ";
            EditorGUILayout.LabelField(condLabel, GUILayout.Width(40));

            // パラメータ
            int paramIdx = Mathf.Clamp(paramProp.intValue, 0, ParamLabels.Length - 1);
            paramProp.intValue = EditorGUILayout.Popup(paramIdx, ParamLabels, GUILayout.MinWidth(80));

            EditorGUILayout.LabelField("が", GUILayout.Width(16));

            // 演算子
            int opIdx = Mathf.Clamp(opProp.intValue, 0, OpLabels.Length - 1);
            opProp.intValue = EditorGUILayout.Popup(opIdx, OpLabels, GUILayout.Width(56));

            // 閾値（パラメータ種類によってUIを切り替え）
            int pIdx = paramProp.intValue;
            if (pIdx == 4) // OrgasmCount: 整数入力
            {
                threshProp.floatValue = Mathf.Max(1f,
                    Mathf.Round(EditorGUILayout.FloatField(threshProp.floatValue, GUILayout.Width(48))));
                EditorGUILayout.LabelField("回", GUILayout.Width(20));
            }
            else if (pIdx == 5) // DriveBias: -1〜1 スライダー
            {
                threshProp.floatValue = EditorGUILayout.Slider(threshProp.floatValue, -1f, 1f);
            }
            else if (pIdx == 6) // BrokenDownMode: 0=None 1=アヘ顔 2=トロトロ
            {
                threshProp.floatValue = Mathf.Clamp(
                    Mathf.Round(EditorGUILayout.FloatField(threshProp.floatValue, GUILayout.Width(48))),
                    0f, 2f);
                EditorGUILayout.LabelField("(0=None 1=アヘ 2=トロ)", GUILayout.Width(130));
            }
            else // Arousal / Resistance / Fatigue / Drive: 0〜1 スライダー
            {
                threshProp.floatValue = EditorGUILayout.Slider(threshProp.floatValue, 0f, 1f);
            }

            // 条件削除ボタン
            if (GUILayout.Button("－", GUILayout.Width(24)))
                toDeleteCond = ci;

            EditorGUILayout.EndHorizontal();
        }
        if (toDeleteCond >= 0)
            conditionsProp.DeleteArrayElementAtIndex(toDeleteCond);

        // 条件追加ボタン
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(44);
        if (GUILayout.Button("＋ 条件を追加", EditorStyles.miniButton, GUILayout.Width(110)))
        {
            conditionsProp.arraySize++;
            SerializedProperty newCond = conditionsProp.GetArrayElementAtIndex(conditionsProp.arraySize - 1);
            newCond.FindPropertyRelative("param").intValue     = 0;
            newCond.FindPropertyRelative("op").intValue        = 0;
            newCond.FindPropertyRelative("threshold").floatValue = 0.5f;
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // --- メモ行 ---
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("メモ :", GUILayout.Width(40));
        noteProp.stringValue = EditorGUILayout.TextField(noteProp.stringValue);
        EditorGUILayout.EndHorizontal();

        // --- ボックス終了 ---
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }
}
