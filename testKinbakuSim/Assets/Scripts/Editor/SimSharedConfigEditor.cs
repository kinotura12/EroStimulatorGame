// SimSharedConfigEditor.cs
// SimSharedConfig の Inspector 末尾に Override 状態マトリクスを表示する

using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(SimSharedConfig))]
public class SimSharedConfigEditor : Editor
{
    static readonly string[] OverrideLabels = {
        "Ar", "Rs", "Fa", "Dr", "Bi", "Or", "Tr", "Fr", "NM", "Su"
    };

    static readonly string[] OverrideTooltips = {
        "OverrideArousal",
        "OverrideResistance",
        "OverrideFatigue",
        "OverrideDrive",
        "OverrideDriveBias",
        "OverrideOrgasm",
        "OverrideTransition",
        "OverrideFrustration",
        "OverrideNeedMotion",
        "OverrideSub"
    };

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("=== Override 状態一覧 ===", EditorStyles.boldLabel);

        var guids = AssetDatabase.FindAssets("t:SimStateConfig");
        var configs = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<SimStateConfig>(
                AssetDatabase.GUIDToAssetPath(g)))
            .Where(c => c != null)
            .OrderBy(c => (int)c.State)
            .ToArray();

        if (configs.Length == 0)
        {
            EditorGUILayout.HelpBox("SimStateConfig アセットが見つかりません。", MessageType.Info);
            return;
        }

        float cellW  = 36f;
        float labelW = 120f;

        // ヘッダー行
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("State", GUILayout.Width(labelW));
        for (int i = 0; i < OverrideLabels.Length; i++)
        {
            GUILayout.Label(new GUIContent(OverrideLabels[i], OverrideTooltips[i]),
                EditorStyles.miniLabel, GUILayout.Width(cellW));
        }
        EditorGUILayout.EndHorizontal();

        // セパレータ
        var sepRect = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(sepRect, new Color(0.5f, 0.5f, 0.5f));

        // データ行
        var onStyle  = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.2f, 0.8f, 0.2f) } };
        var offStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } };

        foreach (var cfg in configs)
        {
            EditorGUILayout.BeginHorizontal();

            bool[] overrides = {
                cfg.OverrideArousal,
                cfg.OverrideResistance,
                cfg.OverrideFatigue,
                cfg.OverrideDrive,
                cfg.OverrideDriveBias,
                cfg.OverrideOrgasm,
                cfg.OverrideTransition,
                cfg.OverrideFrustration,
                cfg.OverrideNeedMotion,
                cfg.OverrideSub
            };

            if (GUILayout.Button(cfg.State.ToString(), EditorStyles.label, GUILayout.Width(labelW)))
                EditorGUIUtility.PingObject(cfg);

            foreach (var ov in overrides)
                GUILayout.Label(ov ? "●" : "○", ov ? onStyle : offStyle, GUILayout.Width(cellW));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(12);
        DrawDefaultInspector();
    }
}
