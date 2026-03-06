// Editor/SimTestSceneSetup.cs
// Unity のメニューからテストシーンを自動でセットアップするスクリプト
// ※ このファイルは必ず "Editor" という名前のフォルダの中に入れること！

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SimTestSceneSetup
{
    [MenuItem("Sim/① テストシーンを自動セットアップ")]
    static void Setup()
    {
        // 既存オブジェクトがあればスキップ（位置・サイズなど手動調整は保持）
        // 新規オブジェクトだけ作成し、最後に参照を再接続する

        EnsureEventSystem();

        // --- SimulationRoot ---
        var rootGo       = GameObject.Find("SimulationRoot") ?? new GameObject("SimulationRoot");
        var simManager   = rootGo.GetComponent<SimulationManager>()   ?? rootGo.AddComponent<SimulationManager>();
        var inputHandler = rootGo.GetComponent<InputHandler>()         ?? rootGo.AddComponent<InputHandler>();

        // --- Canvas ---
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("Canvas");
            var c = canvasGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }
        var canvasTf = canvasGo.transform;

        // --- キャラ四角 ---
        var charaGo = canvasTf.Find("CharaRect")?.gameObject;
        if (charaGo == null)
        {
            charaGo = new GameObject("CharaRect");
            charaGo.transform.SetParent(canvasTf, false);
            var r = charaGo.AddComponent<RectTransform>();
            r.sizeDelta        = new Vector2(200f, 280f);
            r.anchoredPosition = new Vector2(0f, 50f);
            charaGo.AddComponent<Image>().color = new Color(0.5f, 0.6f, 0.8f);
        }
        var charaRect  = charaGo.GetComponent<RectTransform>();
        var charaImage = charaGo.GetComponent<Image>();

        // --- ピストンスライダー ---
        var sliderGo = canvasTf.Find("PistonSlider")?.gameObject;
        if (sliderGo == null)
        {
            sliderGo      = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderGo.name = "PistonSlider";
            sliderGo.transform.SetParent(canvasTf, false);
            var r = sliderGo.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(0f, -180f);
            r.sizeDelta        = new Vector2(400f, 40f);
            var s = sliderGo.GetComponent<Slider>();
            s.minValue = 0f; s.maxValue = 1f; s.value = 0f;
        }
        var slider = sliderGo.GetComponent<Slider>();

        // --- 閾値ゾーンバー ---
        var threshBarGo = canvasTf.Find("ThresholdBar")?.gameObject;
        if (threshBarGo == null)
        {
            threshBarGo = new GameObject("ThresholdBar");
            threshBarGo.transform.SetParent(canvasTf, false);
            var r = threshBarGo.AddComponent<RectTransform>();
            r.anchoredPosition = new Vector2(0f, -165f);
            r.sizeDelta        = new Vector2(400f, 10f);
            threshBarGo.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
        }
        var threshTf    = threshBarGo.transform;
        var zoneLowImg    = GetOrCreateZone(threshTf, "ZoneLow",    new Vector2(0f,    0f), new Vector2(0.35f, 1f), new Color(0.85f, 0.3f,  0.3f,  0.9f));
        var zoneWithinImg = GetOrCreateZone(threshTf, "ZoneWithin", new Vector2(0.35f, 0f), new Vector2(0.65f, 1f), new Color(0.3f,  0.8f,  0.3f,  0.9f));
        var zoneHighImg   = GetOrCreateZone(threshTf, "ZoneHigh",   new Vector2(0.65f, 0f), new Vector2(1f,    1f), new Color(0.9f,  0.55f, 0.15f, 0.9f));

        // --- SubA トグル ---
        var subAGo = canvasTf.Find("SubAToggle")?.gameObject;
        if (subAGo == null)
        {
            subAGo      = DefaultControls.CreateToggle(new DefaultControls.Resources());
            subAGo.name = "SubAToggle";
            subAGo.transform.SetParent(canvasTf, false);
            subAGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-120f, -240f);
            var bg = subAGo.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var ck = subAGo.transform.Find("Background/Checkmark")?.GetComponent<Image>();
            if (ck != null) ck.color = new Color(0.2f, 1f, 0.2f, 1f);
            var lbl = subAGo.GetComponentInChildren<Text>();
            if (lbl != null) { lbl.text = "SubA（チンコ）"; lbl.color = Color.white; }
            var t = subAGo.GetComponent<Toggle>();
            if (t != null) t.isOn = false;
        }

        // --- SubB トグル ---
        var subBGo = canvasTf.Find("SubBToggle")?.gameObject;
        if (subBGo == null)
        {
            subBGo      = DefaultControls.CreateToggle(new DefaultControls.Resources());
            subBGo.name = "SubBToggle";
            subBGo.transform.SetParent(canvasTf, false);
            subBGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(120f, -240f);
            var bg = subBGo.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var ck = subBGo.transform.Find("Background/Checkmark")?.GetComponent<Image>();
            if (ck != null) ck.color = new Color(0.2f, 1f, 0.2f, 1f);
            var lbl = subBGo.GetComponentInChildren<Text>();
            if (lbl != null) { lbl.text = "SubB（乳首）"; lbl.color = Color.white; }
            var t = subBGo.GetComponent<Toggle>();
            if (t != null) t.isOn = false;
        }

        // --- SubA/B インジケーター（移動済みの場合は GameObject.Find でシーン全体から探す） ---
        var subAIndGo = GameObject.Find("SubAIndicator");
        if (subAIndGo == null)
        {
            subAIndGo = new GameObject("SubAIndicator");
            subAIndGo.transform.SetParent(canvasTf, false);
            var r = subAIndGo.AddComponent<RectTransform>();
            r.anchoredPosition = new Vector2(0f, -108f);
            r.sizeDelta        = new Vector2(20f, 40f);
            subAIndGo.AddComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f, 0.9f);
        }
        var subAIndImg = subAIndGo.GetComponent<Image>();

        var subBIndGo = GameObject.Find("SubBIndicator");
        if (subBIndGo == null)
        {
            subBIndGo = new GameObject("SubBIndicator");
            subBIndGo.transform.SetParent(canvasTf, false);
            var r = subBIndGo.AddComponent<RectTransform>();
            r.anchoredPosition = new Vector2(0f, 78f);
            r.sizeDelta        = new Vector2(200f, 7f);
            subBIndGo.AddComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f, 0.9f);
        }
        var subBIndImg = subBIndGo.GetComponent<Image>();

        // --- State ラベル ---
        var stateLabelGo = canvasTf.Find("StateLabel")?.gameObject;
        if (stateLabelGo == null)
        {
            stateLabelGo      = DefaultControls.CreateText(new DefaultControls.Resources());
            stateLabelGo.name = "StateLabel";
            stateLabelGo.transform.SetParent(canvasTf, false);
            var r = stateLabelGo.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(0f, 220f);
            r.sizeDelta        = new Vector2(400f, 50f);
            var t = stateLabelGo.GetComponent<Text>();
            t.text = "State: Guarded"; t.fontSize = 24;
            t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
        }
        var stateText = stateLabelGo.GetComponent<Text>();

        // --- END ラベル ---
        var endLabelGo = canvasTf.Find("EndLabel")?.gameObject;
        if (endLabelGo == null)
        {
            endLabelGo      = DefaultControls.CreateText(new DefaultControls.Resources());
            endLabelGo.name = "EndLabel";
            endLabelGo.transform.SetParent(canvasTf, false);
            var r = endLabelGo.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(0f, 180f);
            r.sizeDelta        = new Vector2(420f, 44f);
            var t = endLabelGo.GetComponent<Text>();
            t.text = "";
            t.fontSize = 26;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(1f, 0.85f, 0.2f, 1f);
        }
        var endText = endLabelGo.GetComponent<Text>();
        endLabelGo.SetActive(false);

        // --- リスタートボタン ---
        var restartButtonGo = canvasTf.Find("RestartButton")?.gameObject;
        if (restartButtonGo == null)
        {
            restartButtonGo      = DefaultControls.CreateButton(new DefaultControls.Resources());
            restartButtonGo.name = "RestartButton";
            restartButtonGo.transform.SetParent(canvasTf, false);
            var r = restartButtonGo.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(0f, 145f);
            r.sizeDelta        = new Vector2(220f, 44f);
            var label = restartButtonGo.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = "リスタート";
                label.fontSize = 22;
                label.color = Color.white;
            }
        }
        var restartButton = restartButtonGo.GetComponent<Button>();
        restartButtonGo.SetActive(false);

        // --- キャラパラメータラベル ---
        var charaParamLabelGo = canvasTf.Find("CharaParamLabel")?.gameObject;
        if (charaParamLabelGo == null)
        {
            charaParamLabelGo      = DefaultControls.CreateText(new DefaultControls.Resources());
            charaParamLabelGo.name = "CharaParamLabel";
            charaParamLabelGo.transform.SetParent(canvasTf, false);
            var r = charaParamLabelGo.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(-310f, 60f);
            r.sizeDelta        = new Vector2(260f, 180f);
            var t = charaParamLabelGo.GetComponent<Text>();
            t.text = "（キャラパラメータ）"; t.fontSize = 18;
            t.alignment = TextAnchor.UpperLeft; t.color = Color.white;
        }
        var charaParamText = charaParamLabelGo.GetComponent<Text>();

        // --- デバッグ用パラメータラベル ---
        var paramLabelGo = canvasTf.Find("ParamLabel")?.gameObject;
        if (paramLabelGo == null)
        {
            paramLabelGo      = DefaultControls.CreateText(new DefaultControls.Resources());
            paramLabelGo.name = "ParamLabel";
            paramLabelGo.transform.SetParent(canvasTf, false);
            var r = paramLabelGo.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(310f, 60f);
            r.sizeDelta        = new Vector2(260f, 260f);
            var t = paramLabelGo.GetComponent<Text>();
            t.text = "（デバッグ出力）"; t.fontSize = 13;
            t.alignment = TextAnchor.UpperLeft; t.color = new Color(0.6f, 0.6f, 0.6f);
        }
        var paramText = paramLabelGo.GetComponent<Text>();

        // --- DebugVisualizer の接続（常に再接続して参照切れを防ぐ） ---
        var visualizer = rootGo.GetComponent<DebugVisualizer>() ?? rootGo.AddComponent<DebugVisualizer>();
        var so = new SerializedObject(visualizer);
        so.FindProperty("sim").objectReferenceValue               = simManager;
        so.FindProperty("inputHandler").objectReferenceValue      = inputHandler;
        so.FindProperty("charaRect").objectReferenceValue         = charaRect;
        so.FindProperty("charaImage").objectReferenceValue        = charaImage;
        so.FindProperty("pistonSlider").objectReferenceValue      = slider;
        so.FindProperty("subAToggle").objectReferenceValue        = subAGo.GetComponent<Toggle>();
        so.FindProperty("subBToggle").objectReferenceValue        = subBGo.GetComponent<Toggle>();
        so.FindProperty("stateLabel").objectReferenceValue        = stateText;
        so.FindProperty("endLabel").objectReferenceValue          = endText;
        so.FindProperty("charaParamLabel").objectReferenceValue   = charaParamText;
        so.FindProperty("paramLabel").objectReferenceValue        = paramText;
        so.FindProperty("restartButton").objectReferenceValue     = restartButton;
        so.FindProperty("zoneLow").objectReferenceValue           = zoneLowImg;
        so.FindProperty("zoneWithin").objectReferenceValue        = zoneWithinImg;
        so.FindProperty("zoneHigh").objectReferenceValue          = zoneHighImg;
        so.FindProperty("subAIndicator").objectReferenceValue     = subAIndImg;
        so.FindProperty("subBIndicator").objectReferenceValue     = subBIndImg;
        so.ApplyModifiedProperties();

        var simSo = new SerializedObject(simManager);
        simSo.FindProperty("inputHandler").objectReferenceValue = inputHandler;
        simSo.ApplyModifiedProperties();

        if (Camera.main != null) Camera.main.backgroundColor = Color.black;

        Debug.Log("✅ テストシーンのセットアップ完了！\n" +
                  "⚠️ SimulationManager の stateConfigs に ScriptableObject を7つ設定してね。\n" +
                  "メニュー → Sim → ② StateConfig を7つ作成　で自動生成できるよ！");

        Selection.activeGameObject = rootGo;

        EditorUtility.DisplayDialog(
            "セットアップ完了！",
            "テストシーンができたよ！\n（既存オブジェクトの手動調整は保持されます）\n\n" +
            "次に：\n" +
            "メニュー → Sim → ② StateConfig を7つ作成\n" +
            "を実行してね。",
            "OK");
    }

    static Image GetOrCreateZone(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<Image>();
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;

        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        eventSystemGo.AddComponent<StandaloneInputModule>();
#endif
    }

    [MenuItem("Sim/② StateConfig を7つ作成")]
    static void CreateStateConfigs()
    {
        // 保存先フォルダを作る
        if (!AssetDatabase.IsValidFolder("Assets/Sim"))
            AssetDatabase.CreateFolder("Assets", "Sim");
        if (!AssetDatabase.IsValidFolder("Assets/Sim/StateConfigs"))
            AssetDatabase.CreateFolder("Assets/Sim", "StateConfigs");

        var states = new[]
        {
            (SimState.Guarded,           "StateConfig_Guarded"),
            (SimState.Defensive,         "StateConfig_Defensive"),
            (SimState.Overridden,        "StateConfig_Overridden"),
            (SimState.FrustratedCraving, "StateConfig_FrustratedCraving"),
            (SimState.Acclimating,       "StateConfig_Acclimating"),
            (SimState.Surrendered,       "StateConfig_Surrendered"),
            (SimState.BrokenDown,        "StateConfig_BrokenDown"),
        };

        // 仕様書の閾値をデフォルト値として設定
        var tolLows  = new[] { 0.35f, 0.55f, 0.30f, 0.25f, 0.30f, 0.20f, 0.05f };
        var tolHighs = new[] { 0.65f, 0.85f, 0.70f, 0.55f, 0.70f, 0.80f, 0.95f };

        var createdConfigs = new SimStateConfig[7];

        for (int i = 0; i < states.Length; i++)
        {
            var (state, name) = states[i];
            string path = $"Assets/Sim/StateConfigs/{name}.asset";

            // すでに存在する場合はスキップ
            var existing = AssetDatabase.LoadAssetAtPath<SimStateConfig>(path);
            if (existing != null)
            {
                createdConfigs[i] = existing;
                continue;
            }

            var config    = ScriptableObject.CreateInstance<SimStateConfig>();
            config.State  = state;
            config.TolLow = tolLows[i];
            config.TolHigh= tolHighs[i];

            // BrokenDown は Fatigue 蓄積2倍・射精閾値40%
            if (state == SimState.BrokenDown)
            {
                config.FatigueMultiplier           = 2.0f;
                config.OrgasmThresholdMultiplier   = 0.4f;
            }

            AssetDatabase.CreateAsset(config, path);
            createdConfigs[i] = config;
        }

        AssetDatabase.SaveAssets();

        // SimulationManager に自動接続
        var simManager = GameObject.FindAnyObjectByType<SimulationManager>();
        if (simManager != null)
        {
            var so = new SerializedObject(simManager);
            var configsProp = so.FindProperty("stateConfigs");
            configsProp.arraySize = 7;
            for (int i = 0; i < 7; i++)
                configsProp.GetArrayElementAtIndex(i).objectReferenceValue = createdConfigs[i];
            so.ApplyModifiedProperties();

            Debug.Log("✅ StateConfig を SimulationManager に自動接続したよ！");
        }

        EditorUtility.DisplayDialog(
            "StateConfig 作成完了！",
            "Assets/Sim/StateConfigs/ に7つ作成して\nSimulationManager に接続したよ！\n\n" +
            "あとは ▶ ボタンで再生してみてね！",
            "OK");
    }
}
