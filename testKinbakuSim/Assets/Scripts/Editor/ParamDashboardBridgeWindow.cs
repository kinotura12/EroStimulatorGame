using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ParamDashboardBridgeWindow : EditorWindow
{
    const string DashboardRelativePath = "Assets/Sim/param_dashboard_final.html";
    const string StateConfigSearchRoot = "Assets/Sim/StateConfigs";

    [Serializable]
    class BridgeRoot
    {
        public int version;
        public BridgeState[] states;
        public HeatEntry[] heatEntries;
        public HeatEntry[] subHeatEntries;
        public HeatEntry[] sharedHeatEntries;
        public HeatEntry[] sharedSubHeatEntries;
        public OverrideEntry[] overrideEntries;
        public string exportedAt;
    }

    [Serializable]
    class BridgeState
    {
        public string id;
        public float tolLow;
        public float tolHigh;
    }

    [Serializable]
    class HeatEntry
    {
        public string stateId;
        public string band;
        public string param;
        public float value;
    }

    [Serializable]
    class OverrideEntry
    {
        public string stateId;
        public string group;
        public bool enabled;
    }

    [MenuItem("Tools/Sim/Param Dashboard Bridge")]
    static void OpenWindow()
    {
        var window = GetWindow<ParamDashboardBridgeWindow>("Param Dashboard Bridge");
        window.minSize = new Vector2(520, 280);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Param Dashboard Bridge (B案)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1) HTMLで値を編集\n2) 「Unity連携JSON」で書き出し\n3) この画面からJSONを適用",
            MessageType.Info
        );

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Open Dashboard HTML", GUILayout.Height(28)))
                OpenDashboard();

            if (GUILayout.Button("Apply Unity Bridge JSON...", GUILayout.Height(28)))
                ApplyBridgeJson();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Export Current Unity -> Dashboard JSON...", GUILayout.Height(26)))
                ExportCurrentUnityToDashboardJson();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Target Assets", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("- " + DashboardRelativePath);
        EditorGUILayout.LabelField("- " + StateConfigSearchRoot + "/StateConfig_*.asset");
    }

    static void OpenDashboard()
    {
        var fullPath = Path.Combine(Application.dataPath, "Sim", "param_dashboard_final.html");
        if (!File.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("Not Found", "Dashboard file not found:\n" + fullPath, "OK");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{fullPath}\"",
                    UseShellExecute = true,
                });
            }
            catch
            {
                EditorUtility.DisplayDialog(
                    "Open Failed",
                    "Failed to open dashboard HTML.\n\n" + ex.Message + "\n\nPath:\n" + fullPath,
                    "OK"
                );
            }
        }
    }

    static void ApplyBridgeJson()
    {
        var path = EditorUtility.OpenFilePanel("Select Unity Bridge JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;
        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog("Not Found", "JSON file not found.", "OK");
            return;
        }

        BridgeRoot root;
        try
        {
            root = JsonUtility.FromJson<BridgeRoot>(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Parse Error", "Failed to parse JSON:\n" + ex.Message, "OK");
            return;
        }

        if (root == null)
        {
            EditorUtility.DisplayDialog("Parse Error", "JSON parse returned null.", "OK");
            return;
        }

        var stateConfigs = LoadStateConfigsById();
        if (stateConfigs.Count == 0)
        {
            EditorUtility.DisplayDialog("Missing Assets", "No StateConfig assets found.", "OK");
            return;
        }

        int changed = 0;
        int ignored = 0;

        var shared = AssetDatabase.LoadAssetAtPath<SimSharedConfig>("Assets/Sim/SimSharedConfig.asset");

        if (root.states != null)
        {
            foreach (var st in root.states)
            {
                if (st == null || string.IsNullOrEmpty(st.id)) continue;
                if (!stateConfigs.TryGetValue(st.id, out var cfg)) continue;

                Undo.RecordObject(cfg, "Apply Dashboard Tol");
                cfg.TolLow = Mathf.Clamp01(st.tolLow);
                cfg.TolHigh = Mathf.Clamp(Mathf.Max(st.tolHigh, cfg.TolLow + 0.01f), 0f, 1f);
                EditorUtility.SetDirty(cfg);
                changed++;
            }
        }

        if (root.heatEntries != null)
        {
            foreach (var e in root.heatEntries)
            {
                if (e == null || string.IsNullOrEmpty(e.stateId) || !stateConfigs.TryGetValue(e.stateId, out var cfg))
                    continue;

                Undo.RecordObject(cfg, "Apply Dashboard Heat");
                if (ApplyHeatEntry(cfg, e)) { EditorUtility.SetDirty(cfg); changed++; }
                else ignored++;
            }
        }

        if (shared != null && root.sharedHeatEntries != null)
        {
            foreach (var e in root.sharedHeatEntries)
            {
                if (e == null) continue;
                Undo.RecordObject(shared, "Apply Shared Heat");
                if (ApplySharedHeatEntry(shared, e)) { EditorUtility.SetDirty(shared); changed++; }
                else ignored++;
            }
        }

        if (root.subHeatEntries != null)
        {
            foreach (var e in root.subHeatEntries)
            {
                if (e == null || string.IsNullOrEmpty(e.stateId) || !stateConfigs.TryGetValue(e.stateId, out var cfg))
                    continue;

                Undo.RecordObject(cfg, "Apply Dashboard Sub Heat");
                if (ApplySubHeatEntry(cfg, e)) { EditorUtility.SetDirty(cfg); changed++; }
                else ignored++;
            }
        }

        if (shared != null && root.sharedSubHeatEntries != null)
        {
            foreach (var e in root.sharedSubHeatEntries)
            {
                if (e == null) continue;
                Undo.RecordObject(shared, "Apply Shared Sub Heat");
                if (ApplySharedSubHeatEntry(shared, e)) { EditorUtility.SetDirty(shared); changed++; }
                else ignored++;
            }
        }

        if (root.overrideEntries != null)
        {
            foreach (var e in root.overrideEntries)
            {
                if (e == null || string.IsNullOrEmpty(e.stateId) || !stateConfigs.TryGetValue(e.stateId, out var cfg))
                    continue;
                Undo.RecordObject(cfg, "Apply Override Flags");
                if (ApplyOverrideEntry(cfg, e)) { EditorUtility.SetDirty(cfg); changed++; }
                else ignored++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Applied",
            $"Applied entries: {changed}\nIgnored entries: {ignored}\n\nIgnored = current runtime has no direct field mapping.",
            "OK"
        );
    }

    static void ExportCurrentUnityToDashboardJson()
    {
        var stateConfigs = LoadStateConfigsById();
        var shared = AssetDatabase.LoadAssetAtPath<SimSharedConfig>("Assets/Sim/SimSharedConfig.asset");
        if (stateConfigs.Count == 0 || shared == null)
        {
            EditorUtility.DisplayDialog("Missing Assets", "StateConfigs or SimSharedConfig not found.", "OK");
            return;
        }

        var root = BuildBridgeRootFromAssets(stateConfigs, shared);
        var json = JsonUtility.ToJson(root, true);

        var path = EditorUtility.SaveFilePanel(
            "Export Dashboard JSON",
            Application.dataPath,
            "param_dashboard_unity_export",
            "json");
        if (string.IsNullOrEmpty(path)) return;

        File.WriteAllText(path, json);
        EditorUtility.RevealInFinder(path);
        EditorUtility.DisplayDialog("Exported", "Exported dashboard JSON.\n\n" + path, "OK");
    }

    static BridgeRoot BuildBridgeRootFromAssets(Dictionary<string, SimStateConfig> stateConfigs, SimSharedConfig shared)
    {
        var root = new BridgeRoot
        {
            version = 1,
            exportedAt = DateTime.UtcNow.ToString("o")
        };

        var stateOrder = new[]
        {
            SimState.Guarded, SimState.Defensive, SimState.Overridden,
            SimState.FrustratedCraving, SimState.Acclimating, SimState.Surrendered, SimState.BrokenDown
        };

        var stateList = new List<BridgeState>();
        var heatList = new List<HeatEntry>();
        var subList = new List<HeatEntry>();
        var sharedHeat = new List<HeatEntry>();
        var sharedSub = new List<HeatEntry>();
        var ovList = new List<OverrideEntry>();

        foreach (var s in stateOrder)
        {
            var id = ToStateId(s);
            if (!stateConfigs.TryGetValue(id, out var cfg)) continue;

            stateList.Add(new BridgeState { id = id, tolLow = cfg.TolLow, tolHigh = cfg.TolHigh });

            // Override flags
            ovList.Add(new OverrideEntry { stateId = id, group = "OverrideArousal", enabled = cfg.OverrideArousal });
            ovList.Add(new OverrideEntry { stateId = id, group = "OverrideResistance", enabled = cfg.OverrideResistance });
            ovList.Add(new OverrideEntry { stateId = id, group = "OverrideFatigue", enabled = cfg.OverrideFatigue });
            ovList.Add(new OverrideEntry { stateId = id, group = "OverrideDrive", enabled = cfg.OverrideDrive });
            ovList.Add(new OverrideEntry { stateId = id, group = "OverrideDriveBias", enabled = cfg.OverrideDriveBias });
            ovList.Add(new OverrideEntry { stateId = id, group = "OverrideFrustration", enabled = cfg.OverrideFrustration });
            ovList.Add(new OverrideEntry { stateId = id, group = "OverrideNeedMotion", enabled = cfg.OverrideNeedMotion });
            ovList.Add(new OverrideEntry { stateId = id, group = "OverrideSub", enabled = cfg.OverrideSub });

            // Heat entries by state
            AddHeatEntriesForState(heatList, id, cfg);
            AddSubEntriesForState(subList, id, cfg);
        }

        AddHeatEntriesForShared(sharedHeat, shared);
        AddSubEntriesForShared(sharedSub, shared);

        root.states = stateList.ToArray();
        root.heatEntries = heatList.ToArray();
        root.subHeatEntries = subList.ToArray();
        root.sharedHeatEntries = sharedHeat.ToArray();
        root.sharedSubHeatEntries = sharedSub.ToArray();
        root.overrideEntries = ovList.ToArray();
        return root;
    }

    static void AddHeatEntriesForState(List<HeatEntry> list, string id, SimStateConfig c)
    {
        // Arousal
        list.Add(new HeatEntry { stateId = id, band = "Stop", param = "Arousal", value = c.ArousalChangeStop });
        list.Add(new HeatEntry { stateId = id, band = "Below", param = "Arousal", value = c.ArousalChangeBelow });
        list.Add(new HeatEntry { stateId = id, band = "Within", param = "Arousal", value = c.ArousalChangeWithin });
        list.Add(new HeatEntry { stateId = id, band = "Above", param = "Arousal", value = c.ArousalChangeAbove });
        // Resistance
        list.Add(new HeatEntry { stateId = id, band = "Stop", param = "Resistance", value = c.ResistanceChangeStop });
        list.Add(new HeatEntry { stateId = id, band = "Below", param = "Resistance", value = c.ResistanceChangeBelow });
        list.Add(new HeatEntry { stateId = id, band = "Within", param = "Resistance", value = c.ResistanceChangeWithin });
        list.Add(new HeatEntry { stateId = id, band = "Above", param = "Resistance", value = c.ResistanceChangeAbove });
        // Fatigue
        list.Add(new HeatEntry { stateId = id, band = "Stop", param = "Fatigue", value = c.FatigueChangeStop });
        list.Add(new HeatEntry { stateId = id, band = "Below", param = "Fatigue", value = c.FatigueChangeBelow });
        list.Add(new HeatEntry { stateId = id, band = "Within", param = "Fatigue", value = c.FatigueChangeWithin * c.FatigueMultiplier });
        list.Add(new HeatEntry { stateId = id, band = "Above", param = "Fatigue", value = c.FatigueChangeAbove * c.FatigueMultiplier });
        // Drive
        list.Add(new HeatEntry { stateId = id, band = "Stop", param = "Drive", value = c.DriveChangeStop });
        list.Add(new HeatEntry { stateId = id, band = "Below", param = "Drive", value = c.DriveChangeBelow });
        list.Add(new HeatEntry { stateId = id, band = "Within", param = "Drive", value = c.DriveChangeWithin });
        list.Add(new HeatEntry { stateId = id, band = "Above", param = "Drive", value = c.DriveChangeAbove });
        // DriveBias (dashboard表示は正値ゲイン)
        list.Add(new HeatEntry { stateId = id, band = "Stop", param = "DriveBias", value = c.DriveBiasDecayStop });
        list.Add(new HeatEntry { stateId = id, band = "Below", param = "DriveBias", value = c.DriveBiasShiftBelow });
        list.Add(new HeatEntry { stateId = id, band = "Within", param = "DriveBias", value = c.DriveBiasDecayWithin });
        list.Add(new HeatEntry { stateId = id, band = "Above", param = "DriveBias", value = c.DriveBiasShiftAbove });
        // NeedMotion
        list.Add(new HeatEntry { stateId = id, band = "Stop", param = "NeedMotion", value = c.NeedMotionStopCalmChange });
        list.Add(new HeatEntry { stateId = id, band = "Below", param = "NeedMotion", value = c.NeedMotionBelowChange });
        list.Add(new HeatEntry { stateId = id, band = "Within", param = "NeedMotion", value = c.NeedMotionWithinChange });
        list.Add(new HeatEntry { stateId = id, band = "Above", param = "NeedMotion", value = c.NeedMotionAboveChange });
        // FrustrationStack
        list.Add(new HeatEntry { stateId = id, band = "Stop", param = "FrustrationStack", value = 0f });
        list.Add(new HeatEntry { stateId = id, band = "Below", param = "FrustrationStack", value = c.FrustrationStackGain });
        list.Add(new HeatEntry { stateId = id, band = "Within", param = "FrustrationStack", value = 0f });
        list.Add(new HeatEntry { stateId = id, band = "Above", param = "FrustrationStack", value = 0f });
    }

    static void AddSubEntriesForState(List<HeatEntry> list, string id, SimStateConfig c)
    {
        // SubA
        list.Add(new HeatEntry { stateId = id, band = "SubA", param = "Arousal", value = c.SubAArousalChange });
        list.Add(new HeatEntry { stateId = id, band = "SubA", param = "Resistance", value = c.SubAResistanceChange });
        list.Add(new HeatEntry { stateId = id, band = "SubA", param = "Fatigue", value = 0f });
        list.Add(new HeatEntry { stateId = id, band = "SubA", param = "Drive", value = c.SubADriveChange });
        list.Add(new HeatEntry { stateId = id, band = "SubA", param = "DriveBias", value = c.SubADriveBiasGain });
        list.Add(new HeatEntry { stateId = id, band = "SubA", param = "NeedMotion", value = 0f });
        list.Add(new HeatEntry { stateId = id, band = "SubA", param = "FrustrationStack", value = 0f });
        // SubB
        list.Add(new HeatEntry { stateId = id, band = "SubB", param = "Arousal", value = c.SubBArousalChange });
        list.Add(new HeatEntry { stateId = id, band = "SubB", param = "Resistance", value = c.SubBResistanceChange });
        list.Add(new HeatEntry { stateId = id, band = "SubB", param = "Fatigue", value = 0f });
        list.Add(new HeatEntry { stateId = id, band = "SubB", param = "Drive", value = c.SubBDriveChange });
        list.Add(new HeatEntry { stateId = id, band = "SubB", param = "DriveBias", value = c.SubBDriveBiasGain });
        list.Add(new HeatEntry { stateId = id, band = "SubB", param = "NeedMotion", value = 0f });
        list.Add(new HeatEntry { stateId = id, band = "SubB", param = "FrustrationStack", value = 0f });
        // SubA+B
        list.Add(new HeatEntry { stateId = id, band = "SubA+B", param = "Arousal", value = c.SubAArousalChange + c.SubBArousalChange + Mathf.Max(0f, c.SubBothArousalChange) });
        list.Add(new HeatEntry { stateId = id, band = "SubA+B", param = "Resistance", value = c.SubAResistanceChange + c.SubBResistanceChange });
        list.Add(new HeatEntry { stateId = id, band = "SubA+B", param = "Fatigue", value = 0f });
        list.Add(new HeatEntry { stateId = id, band = "SubA+B", param = "Drive", value = c.SubADriveChange + c.SubBDriveChange });
        list.Add(new HeatEntry { stateId = id, band = "SubA+B", param = "DriveBias", value = -Mathf.Abs(c.SubADriveBiasGain + c.SubBDriveBiasGain) });
        list.Add(new HeatEntry { stateId = id, band = "SubA+B", param = "NeedMotion", value = 0f });
        list.Add(new HeatEntry { stateId = id, band = "SubA+B", param = "FrustrationStack", value = 0f });
    }

    static void AddHeatEntriesForShared(List<HeatEntry> list, SimSharedConfig c)
    {
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Stop", param = "Arousal", value = c.ArousalChangeStop });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Below", param = "Arousal", value = c.ArousalChangeBelow });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Within", param = "Arousal", value = c.ArousalChangeWithin });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Above", param = "Arousal", value = c.ArousalChangeAbove });

        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Stop", param = "Resistance", value = c.ResistanceChangeStop });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Below", param = "Resistance", value = c.ResistanceChangeBelow });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Within", param = "Resistance", value = c.ResistanceChangeWithin });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Above", param = "Resistance", value = c.ResistanceChangeAbove });

        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Stop", param = "Fatigue", value = c.FatigueChangeStop });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Below", param = "Fatigue", value = c.FatigueChangeBelow });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Within", param = "Fatigue", value = c.FatigueChangeWithin * c.FatigueMultiplier });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Above", param = "Fatigue", value = c.FatigueChangeAbove * c.FatigueMultiplier });

        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Stop", param = "Drive", value = c.DriveChangeStop });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Below", param = "Drive", value = c.DriveChangeBelow });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Within", param = "Drive", value = c.DriveChangeWithin });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Above", param = "Drive", value = c.DriveChangeAbove });

        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Stop", param = "DriveBias", value = c.DriveBiasDecayStop });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Below", param = "DriveBias", value = c.DriveBiasShiftBelow });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Within", param = "DriveBias", value = c.DriveBiasDecayWithin });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Above", param = "DriveBias", value = c.DriveBiasShiftAbove });

        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Stop", param = "NeedMotion", value = c.NeedMotionStopCalmChange });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Below", param = "NeedMotion", value = c.NeedMotionBelowChange });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Within", param = "NeedMotion", value = c.NeedMotionWithinChange });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Above", param = "NeedMotion", value = c.NeedMotionAboveChange });

        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Stop", param = "FrustrationStack", value = 0f });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Below", param = "FrustrationStack", value = c.FrustrationStackGain });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Within", param = "FrustrationStack", value = 0f });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "Above", param = "FrustrationStack", value = 0f });
    }

    static void AddSubEntriesForShared(List<HeatEntry> list, SimSharedConfig c)
    {
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubA", param = "Arousal", value = c.SubAArousalChange });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubA", param = "Resistance", value = c.SubAResistanceChange });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubA", param = "Fatigue", value = 0f });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubA", param = "Drive", value = c.SubADriveChange });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubA", param = "DriveBias", value = c.SubADriveBiasGain });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubA", param = "NeedMotion", value = 0f });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubA", param = "FrustrationStack", value = 0f });

        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubB", param = "Arousal", value = c.SubBArousalChange });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubB", param = "Resistance", value = c.SubBResistanceChange });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubB", param = "Fatigue", value = 0f });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubB", param = "Drive", value = c.SubBDriveChange });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubB", param = "DriveBias", value = c.SubBDriveBiasGain });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubB", param = "NeedMotion", value = 0f });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubB", param = "FrustrationStack", value = 0f });

        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubA+B", param = "Arousal", value = c.SubAArousalChange + c.SubBArousalChange + Mathf.Max(0f, c.SubBothArousalChange) });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubA+B", param = "Resistance", value = c.SubAResistanceChange + c.SubBResistanceChange });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubA+B", param = "Fatigue", value = 0f });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubA+B", param = "Drive", value = c.SubADriveChange + c.SubBDriveChange });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubA+B", param = "DriveBias", value = -Mathf.Abs(c.SubADriveBiasGain + c.SubBDriveBiasGain) });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubA+B", param = "NeedMotion", value = 0f });
        list.Add(new HeatEntry { stateId = "SharedConfig", band = "SubA+B", param = "FrustrationStack", value = 0f });
    }

    static Dictionary<string, SimStateConfig> LoadStateConfigsById()
    {
        var map = new Dictionary<string, SimStateConfig>(StringComparer.OrdinalIgnoreCase);
        var guids = AssetDatabase.FindAssets("t:SimStateConfig", new[] { StateConfigSearchRoot });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<SimStateConfig>(path);
            if (cfg == null) continue;
            map[ToStateId(cfg.State)] = cfg;
        }
        return map;
    }

    static string ToStateId(SimState state) => state switch
    {
        SimState.Guarded => "Guarded",
        SimState.Defensive => "Defensive",
        SimState.Overridden => "Overridden",
        SimState.FrustratedCraving => "FrustratedCraving",
        SimState.Acclimating => "Acclimating",
        SimState.Surrendered => "Surrendered",
        SimState.BrokenDown => "BrokenDown",
        _ => state.ToString(),
    };

    static bool ApplyHeatEntry(SimStateConfig cfg, HeatEntry e)
    {
        var band = (e.band ?? string.Empty).Trim();
        var param = (e.param ?? string.Empty).Trim();
        var v = e.value;

        if (param == "Arousal")
            return SetByBand(band, () => cfg.ArousalChangeStop = v, () => cfg.ArousalChangeBelow = v, () => cfg.ArousalChangeWithin = v, () => cfg.ArousalChangeAbove = v);

        if (param == "Resistance")
            return SetByBand(band, () => cfg.ResistanceChangeStop = v, () => cfg.ResistanceChangeBelow = v, () => cfg.ResistanceChangeWithin = v, () => cfg.ResistanceChangeAbove = v);

        if (param == "Fatigue")
            return SetByBand(band, () => cfg.FatigueChangeStop = v, () => cfg.FatigueChangeBelow = v, () => cfg.FatigueChangeWithin = v, () => cfg.FatigueChangeAbove = v);

        if (param == "Drive")
            return SetByBand(band, () => cfg.DriveChangeStop = v, () => cfg.DriveChangeBelow = v, () => cfg.DriveChangeWithin = v, () => cfg.DriveChangeAbove = v);

        if (param == "DriveBias")
            return SetByBand(
                band,
                () => cfg.DriveBiasDecayStop = Mathf.Abs(v),
                () => cfg.DriveBiasShiftBelow = Mathf.Abs(v),
                () => cfg.DriveBiasDecayWithin = Mathf.Abs(v),
                () => cfg.DriveBiasShiftAbove = Mathf.Abs(v)
            );

        if (param == "NeedMotion")
            return SetByBand(band, () => cfg.NeedMotionStopCalmChange = v, () => cfg.NeedMotionBelowChange = v, () => cfg.NeedMotionWithinChange = v, () => cfg.NeedMotionAboveChange = v);

        if (param == "FrustrationStack")
        {
            if (string.Equals(band, "Below", StringComparison.OrdinalIgnoreCase))
            {
                cfg.FrustrationStackGain = v;
                return true;
            }
            return false;
        }

        return false;
    }

    static bool ApplySubHeatEntry(SimStateConfig cfg, HeatEntry e)
    {
        var input = (e.band ?? string.Empty).Trim();
        var param = (e.param ?? string.Empty).Trim();
        var v = e.value;

        if (string.Equals(input, "SubA", StringComparison.OrdinalIgnoreCase))
        {
            if (param == "Drive") { cfg.SubADriveChange = v; return true; }
            if (param == "Arousal") { cfg.SubAArousalChange = v; return true; }
            if (param == "Resistance") { cfg.SubAResistanceChange = v; return true; }
            if (param == "DriveBias") { cfg.SubADriveBiasGain = v; return true; }
            return false;
        }

        if (string.Equals(input, "SubB", StringComparison.OrdinalIgnoreCase))
        {
            if (param == "Drive") { cfg.SubBDriveChange = v; return true; }
            if (param == "Arousal") { cfg.SubBArousalChange = v; return true; }
            if (param == "Resistance") { cfg.SubBResistanceChange = v; return true; }
            if (param == "DriveBias") { cfg.SubBDriveBiasGain = v; return true; }
            return false;
        }

        if (string.Equals(input, "SubA+B", StringComparison.OrdinalIgnoreCase))
        {
            if (param == "Arousal") { cfg.SubBothArousalChange = Mathf.Max(0f, v); return true; }
            return false;
        }

        return false;
    }

    static bool ApplySharedHeatEntry(SimSharedConfig cfg, HeatEntry e)
    {
        var band = (e.band ?? string.Empty).Trim();
        var param = (e.param ?? string.Empty).Trim();
        var v = e.value;

        if (param == "Arousal")
            return SetByBand(band, () => cfg.ArousalChangeStop = v, () => cfg.ArousalChangeBelow = v, () => cfg.ArousalChangeWithin = v, () => cfg.ArousalChangeAbove = v);
        if (param == "Resistance")
            return SetByBand(band, () => cfg.ResistanceChangeStop = v, () => cfg.ResistanceChangeBelow = v, () => cfg.ResistanceChangeWithin = v, () => cfg.ResistanceChangeAbove = v);
        if (param == "Fatigue")
            return SetByBand(band, () => cfg.FatigueChangeStop = v, () => cfg.FatigueChangeBelow = v, () => cfg.FatigueChangeWithin = v, () => cfg.FatigueChangeAbove = v);
        if (param == "Drive")
            return SetByBand(band, () => cfg.DriveChangeStop = v, () => cfg.DriveChangeBelow = v, () => cfg.DriveChangeWithin = v, () => cfg.DriveChangeAbove = v);
        if (param == "DriveBias")
            return SetByBand(
                band,
                () => cfg.DriveBiasDecayStop = Mathf.Abs(v),
                () => cfg.DriveBiasShiftBelow = Mathf.Abs(v),
                () => cfg.DriveBiasDecayWithin = Mathf.Abs(v),
                () => cfg.DriveBiasShiftAbove = Mathf.Abs(v)
            );
        if (param == "NeedMotion")
            return SetByBand(band, () => cfg.NeedMotionStopCalmChange = v, () => cfg.NeedMotionBelowChange = v, () => cfg.NeedMotionWithinChange = v, () => cfg.NeedMotionAboveChange = v);
        if (param == "FrustrationStack")
        {
            if (string.Equals(band, "Below", StringComparison.OrdinalIgnoreCase))
            {
                cfg.FrustrationStackGain = v;
                return true;
            }
            return false;
        }
        return false;
    }

    static bool ApplySharedSubHeatEntry(SimSharedConfig cfg, HeatEntry e)
    {
        var input = (e.band ?? string.Empty).Trim();
        var param = (e.param ?? string.Empty).Trim();
        var v = e.value;

        if (string.Equals(input, "SubA", StringComparison.OrdinalIgnoreCase))
        {
            if (param == "Drive") { cfg.SubADriveChange = v; return true; }
            if (param == "Arousal") { cfg.SubAArousalChange = v; return true; }
            if (param == "Resistance") { cfg.SubAResistanceChange = v; return true; }
            if (param == "DriveBias") { cfg.SubADriveBiasGain = v; return true; }
            return false;
        }
        if (string.Equals(input, "SubB", StringComparison.OrdinalIgnoreCase))
        {
            if (param == "Drive") { cfg.SubBDriveChange = v; return true; }
            if (param == "Arousal") { cfg.SubBArousalChange = v; return true; }
            if (param == "Resistance") { cfg.SubBResistanceChange = v; return true; }
            if (param == "DriveBias") { cfg.SubBDriveBiasGain = v; return true; }
            return false;
        }
        if (string.Equals(input, "SubA+B", StringComparison.OrdinalIgnoreCase))
        {
            if (param == "Arousal") { cfg.SubBothArousalChange = Mathf.Max(0f, v); return true; }
            return false;
        }
        return false;
    }

    static bool ApplyOverrideEntry(SimStateConfig cfg, OverrideEntry e)
    {
        var g = (e.group ?? string.Empty).Trim();
        if (g == "OverrideArousal") { cfg.OverrideArousal = e.enabled; return true; }
        if (g == "OverrideResistance") { cfg.OverrideResistance = e.enabled; return true; }
        if (g == "OverrideFatigue") { cfg.OverrideFatigue = e.enabled; return true; }
        if (g == "OverrideDrive") { cfg.OverrideDrive = e.enabled; return true; }
        if (g == "OverrideDriveBias") { cfg.OverrideDriveBias = e.enabled; return true; }
        if (g == "OverrideOrgasm") { cfg.OverrideOrgasm = e.enabled; return true; }
        if (g == "OverrideTransition") { cfg.OverrideTransition = e.enabled; return true; }
        if (g == "OverrideFrustration") { cfg.OverrideFrustration = e.enabled; return true; }
        if (g == "OverrideNeedMotion") { cfg.OverrideNeedMotion = e.enabled; return true; }
        if (g == "OverrideSub") { cfg.OverrideSub = e.enabled; return true; }
        return false;
    }

    static bool SetByBand(string band, Action stop, Action below, Action within, Action above)
    {
        if (string.Equals(band, "Stop", StringComparison.OrdinalIgnoreCase)) { stop(); return true; }
        if (string.Equals(band, "Below", StringComparison.OrdinalIgnoreCase)) { below(); return true; }
        if (string.Equals(band, "Within", StringComparison.OrdinalIgnoreCase)) { within(); return true; }
        if (string.Equals(band, "Above", StringComparison.OrdinalIgnoreCase)) { above(); return true; }
        return false;
    }
}
