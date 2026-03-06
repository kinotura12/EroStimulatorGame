using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class StateMachineBridgeWindow : EditorWindow
{
    const string HtmlRelativePath = "Assets/Sim/state_machine.html";
    const string TransitionConfigPath = "Assets/Sim/StateTransitionConfig.asset";

    [Serializable]
    class BridgeRoot
    {
        public string version = "state-machine-bridge-v1";
        public BridgeRule[] rules;
    }

    [Serializable]
    class BridgeRule
    {
        public bool enabled = true;
        public string note = "";
        public string fromState = "Guarded";
        public string toState = "Defensive";
        public string requiredBand = "Any";
        public float bandDuration = 0f;
        public BridgeCondition[] conditions;
    }

    [Serializable]
    class BridgeCondition
    {
        public string param = "Arousal";
        public string op = "GreaterEqual";
        public float threshold = 0.5f;
    }

    [MenuItem("Tools/Sim/State Machine Bridge")]
    public static void OpenWindow()
    {
        var window = GetWindow<StateMachineBridgeWindow>("State Machine Bridge");
        window.minSize = new Vector2(520f, 220f);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("State Machine Bridge (Unity <-> HTML)", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Open state_machine.html", GUILayout.Height(28)))
                OpenHtml();

            if (GUILayout.Button("Export Current Unity -> Bridge JSON...", GUILayout.Height(28)))
                ExportCurrentUnityToBridgeJson();
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Apply Bridge JSON -> StateTransitionConfig...", GUILayout.Height(28)))
            ApplyBridgeJsonToUnity();

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "1) Unity値をHTMLに反映: Export Current Unity -> Bridge JSON を出力し、state_machine.html の読込ボタンで取り込み。\n" +
            "2) HTML編集をUnityへ反映: state_machine.html で Bridge JSON を書き出し、このウィンドウで Apply。",
            MessageType.Info);

        EditorGUILayout.LabelField("- " + HtmlRelativePath);
        EditorGUILayout.LabelField("- " + TransitionConfigPath);
    }

    static void OpenHtml()
    {
        var projectRootDir = Directory.GetParent(Application.dataPath);
        if (projectRootDir == null)
        {
            EditorUtility.DisplayDialog("Open Failed", "Project root could not be resolved.", "OK");
            return;
        }
        var projectRoot = projectRootDir.FullName;
        var fullPath = Path.GetFullPath(Path.Combine(projectRoot, HtmlRelativePath));
        if (!File.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("Not Found", "HTML file not found:\n" + fullPath, "OK");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("[StateMachineBridge] OpenHtml failed: " + ex.Message);
            EditorUtility.DisplayDialog("Open Failed", ex.Message, "OK");
        }
    }

    static StateTransitionConfig LoadTransitionConfig()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<StateTransitionConfig>(TransitionConfigPath);
        if (cfg == null)
        {
            var guids = AssetDatabase.FindAssets("t:StateTransitionConfig");
            if (guids != null && guids.Length > 0)
                cfg = AssetDatabase.LoadAssetAtPath<StateTransitionConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return cfg;
    }

    static void ExportCurrentUnityToBridgeJson()
    {
        var cfg = LoadTransitionConfig();
        if (cfg == null)
        {
            EditorUtility.DisplayDialog("Not Found", "StateTransitionConfig asset not found.", "OK");
            return;
        }

        var root = new BridgeRoot
        {
            rules = BuildBridgeRules(cfg.rules).ToArray()
        };

        var json = JsonUtility.ToJson(root, true);
        var savePath = EditorUtility.SaveFilePanel(
            "Export StateMachine Bridge JSON",
            Application.dataPath,
            "state_machine_bridge",
            "json");

        if (string.IsNullOrEmpty(savePath))
            return;

        File.WriteAllText(savePath, json);
        UnityEngine.Debug.Log($"[StateMachineBridge] Exported: {savePath} ({root.rules.Length} rules)");
        EditorUtility.DisplayDialog("Exported", $"Saved:\n{savePath}\nRules: {root.rules.Length}", "OK");
    }

    static List<BridgeRule> BuildBridgeRules(List<TransitionRule> rules)
    {
        var list = new List<BridgeRule>();
        if (rules == null)
            return list;

        foreach (var rule in rules)
        {
            var outRule = new BridgeRule
            {
                enabled = rule.enabled,
                note = rule.note ?? "",
                fromState = EnumToHtmlStateId(rule.fromState),
                toState = EnumToHtmlStateId(rule.toState),
                requiredBand = rule.requiredBand.ToString(),
                bandDuration = rule.bandDuration,
                conditions = BuildBridgeConditions(rule.conditions).ToArray()
            };
            list.Add(outRule);
        }

        return list;
    }

    static List<BridgeCondition> BuildBridgeConditions(List<TransitionCondition> conditions)
    {
        var list = new List<BridgeCondition>();
        if (conditions == null)
            return list;

        foreach (var cond in conditions)
        {
            list.Add(new BridgeCondition
            {
                param = cond.param.ToString(),
                op = cond.op.ToString(),
                threshold = cond.threshold
            });
        }
        return list;
    }

    static void ApplyBridgeJsonToUnity()
    {
        var cfg = LoadTransitionConfig();
        if (cfg == null)
        {
            EditorUtility.DisplayDialog("Not Found", "StateTransitionConfig asset not found.", "OK");
            return;
        }

        var path = EditorUtility.OpenFilePanel("Select StateMachine Bridge JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path))
            return;

        BridgeRoot root;
        try
        {
            root = JsonUtility.FromJson<BridgeRoot>(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Invalid JSON", ex.Message, "OK");
            return;
        }

        if (root == null || root.rules == null)
        {
            EditorUtility.DisplayDialog("Invalid JSON", "rules が見つかりません。", "OK");
            return;
        }

        var newRules = new List<TransitionRule>();
        var warnings = new List<string>();
        for (int i = 0; i < root.rules.Length; i++)
        {
            var b = root.rules[i];
            if (b == null)
                continue;

            if (!TryParseSimState(b.fromState, out var fromState))
            {
                warnings.Add($"[{i}] fromState 不正: {b.fromState}");
                continue;
            }
            if (!TryParseSimState(b.toState, out var toState))
            {
                warnings.Add($"[{i}] toState 不正: {b.toState}");
                continue;
            }

            var rule = new TransitionRule
            {
                enabled = b.enabled,
                note = b.note ?? string.Empty,
                fromState = fromState,
                toState = toState,
                requiredBand = TryParseEnum(b.requiredBand, BandRequirement.Any),
                bandDuration = Mathf.Max(0f, b.bandDuration),
                conditions = new List<TransitionCondition>()
            };

            if (b.conditions != null)
            {
                foreach (var bc in b.conditions)
                {
                    if (bc == null)
                        continue;
                    rule.conditions.Add(new TransitionCondition
                    {
                        param = TryParseEnum(bc.param, ConditionParam.Arousal),
                        op = TryParseEnum(bc.op, CompareOp.GreaterEqual),
                        threshold = bc.threshold
                    });
                }
            }

            newRules.Add(rule);
        }

        Undo.RecordObject(cfg, "Apply StateMachine Bridge JSON");
        cfg.rules = newRules;
        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var warningText = warnings.Count > 0 ? ("\nWarnings:\n- " + string.Join("\n- ", warnings)) : "";
        UnityEngine.Debug.Log($"[StateMachineBridge] Applied {newRules.Count} rules from {path}");
        EditorUtility.DisplayDialog("Applied", $"Rules: {newRules.Count}{warningText}", "OK");
    }

    static bool TryParseSimState(string raw, out SimState state)
    {
        state = SimState.Guarded;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var mapped = HtmlStateIdToEnum(raw.Trim());
        return Enum.TryParse(mapped, true, out state);
    }

    static T TryParseEnum<T>(string raw, T fallback) where T : struct
    {
        if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse(raw, true, out T parsed))
            return parsed;
        return fallback;
    }

    static string EnumToHtmlStateId(SimState state)
    {
        return state switch
        {
            SimState.End_C_White => "End_CW",
            SimState.End_C_Overload => "End_CO",
            _ => state.ToString()
        };
    }

    static string HtmlStateIdToEnum(string stateId)
    {
        return stateId switch
        {
            "End_CW" => "End_C_White",
            "End_CO" => "End_C_Overload",
            _ => stateId
        };
    }
}
