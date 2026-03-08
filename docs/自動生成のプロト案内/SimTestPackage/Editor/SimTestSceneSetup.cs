// Editor/SimTestSceneSetup.cs
// Unity のメニューからテストシーンを自動でセットアップするスクリプト
// ※ このファイルは必ず "Editor" という名前のフォルダの中に入れること！

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class SimTestSceneSetup
{
    [MenuItem("Sim/① テストシーンを自動セットアップ")]
    static void Setup()
    {
        // すでに存在する場合は確認
        if (GameObject.Find("SimulationRoot") != null)
        {
            bool ok = EditorUtility.DisplayDialog(
                "確認",
                "SimulationRoot がすでに存在します。作り直しますか？",
                "作り直す", "キャンセル");
            if (!ok) return;
            GameObject.DestroyImmediate(GameObject.Find("SimulationRoot"));
        }

        // --- SimulationRoot ---
        var root = new GameObject("SimulationRoot");
        var simManager   = root.AddComponent<SimulationManager>();
        var inputHandler = root.AddComponent<InputHandler>();

        // --- Canvas ---
        var canvasGo = new GameObject("Canvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // --- キャラ代わりの四角 ---
        var charaGo    = new GameObject("CharaRect");
        charaGo.transform.SetParent(canvasGo.transform, false);
        var charaRect  = charaGo.AddComponent<RectTransform>();
        charaRect.sizeDelta        = new Vector2(200f, 280f);
        charaRect.anchoredPosition = new Vector2(0f, 50f);
        var charaImage = charaGo.AddComponent<Image>();
        charaImage.color = new Color(0.5f, 0.6f, 0.8f);

        // --- ピストンスライダー ---
        var sliderGo  = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderGo.name = "PistonSlider";
        sliderGo.transform.SetParent(canvasGo.transform, false);
        var sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.anchoredPosition = new Vector2(0f, -180f);
        sliderRect.sizeDelta        = new Vector2(400f, 40f);
        var slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = 0f;

        // --- SubA トグル ---
        var subAGo  = DefaultControls.CreateToggle(new DefaultControls.Resources());
        subAGo.name = "SubAToggle";
        subAGo.transform.SetParent(canvasGo.transform, false);
        subAGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-120f, -240f);
        var subALabel = subAGo.GetComponentInChildren<Text>();
        if (subALabel != null) subALabel.text = "SubA（チンコ）";

        // --- SubB トグル ---
        var subBGo  = DefaultControls.CreateToggle(new DefaultControls.Resources());
        subBGo.name = "SubBToggle";
        subBGo.transform.SetParent(canvasGo.transform, false);
        subBGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(120f, -240f);
        var subBLabel = subBGo.GetComponentInChildren<Text>();
        if (subBLabel != null) subBLabel.text = "SubB（乳首）";

        // --- State ラベル ---
        var stateLabelGo  = DefaultControls.CreateText(new DefaultControls.Resources());
        stateLabelGo.name = "StateLabel";
        stateLabelGo.transform.SetParent(canvasGo.transform, false);
        stateLabelGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 220f);
        var stateText = stateLabelGo.GetComponent<Text>();
        stateText.text      = "State: Guarded";
        stateText.fontSize  = 24;
        stateText.alignment = TextAnchor.MiddleCenter;
        stateText.color     = Color.white;

        // --- パラメータ表示ラベル ---
        var paramLabelGo  = DefaultControls.CreateText(new DefaultControls.Resources());
        paramLabelGo.name = "ParamLabel";
        paramLabelGo.transform.SetParent(canvasGo.transform, false);
        var paramRect = paramLabelGo.GetComponent<RectTransform>();
        paramRect.anchoredPosition = new Vector2(300f, 50f);
        paramRect.sizeDelta        = new Vector2(320f, 300f);
        var paramText = paramLabelGo.GetComponent<Text>();
        paramText.text      = "（パラメータ表示）";
        paramText.fontSize  = 16;
        paramText.alignment = TextAnchor.UpperLeft;
        paramText.color     = Color.white;

        // --- DebugVisualizer のアタッチと参照設定 ---
        var visualizer = root.AddComponent<DebugVisualizer>();

        // SerializedObject 経由で private フィールドに参照を流し込む
        var so = new SerializedObject(visualizer);
        so.FindProperty("sim").objectReferenceValue          = simManager;
        so.FindProperty("inputHandler").objectReferenceValue = inputHandler;
        so.FindProperty("charaRect").objectReferenceValue    = charaRect;
        so.FindProperty("charaImage").objectReferenceValue   = charaImage;
        so.FindProperty("pistonSlider").objectReferenceValue = slider;
        so.FindProperty("subAToggle").objectReferenceValue   = subAGo.GetComponent<Toggle>();
        so.FindProperty("subBToggle").objectReferenceValue   = subBGo.GetComponent<Toggle>();
        so.FindProperty("stateLabel").objectReferenceValue   = stateText;
        so.FindProperty("paramLabel").objectReferenceValue   = paramText;
        so.ApplyModifiedProperties();

        // SimulationManager にも InputHandler を接続
        var simSo = new SerializedObject(simManager);
        simSo.FindProperty("inputHandler").objectReferenceValue = inputHandler;
        simSo.ApplyModifiedProperties();

        // 背景を黒にする
        Camera.main.backgroundColor = Color.black;

        Debug.Log("✅ テストシーンのセットアップ完了！\n" +
                  "⚠️ SimulationManager の stateConfigs に ScriptableObject を7つ設定してね。\n" +
                  "メニュー → Sim → ② StateConfig を7つ作成　で自動生成できるよ！");

        // Hierarchy で選択状態にする
        Selection.activeGameObject = root;

        EditorUtility.DisplayDialog(
            "セットアップ完了！",
            "テストシーンができたよ！\n\n" +
            "次に：\n" +
            "メニュー → Sim → ② StateConfig を7つ作成\n" +
            "を実行してね。",
            "OK");
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
        var simManager = GameObject.FindObjectOfType<SimulationManager>();
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
