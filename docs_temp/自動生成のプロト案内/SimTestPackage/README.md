# テストシーン導入手順

Unityが初めてでも大丈夫！順番通りにやればOK。

---

## 最初に一回だけやること

### 1. Unityプロジェクトを作る

1. Unity Hub を開く
2. 「新しいプロジェクト」をクリック
3. テンプレートは **「2D」** を選ぶ
4. プロジェクト名は何でもOK（例: `SimTest`）
5. 「プロジェクトを作成」をクリック

---

### 2. スクリプトをUnityに入れる

1. zipを解凍する
2. 解凍したフォルダの中身を確認すると、こんな構成になってる

```
📁 解凍したフォルダ
　├── SimState.cs
　├── SimParameters.cs
　├── SimStateConfig.cs
　├── InputHandler.cs
　├── BandEvaluator.cs
　├── ParameterUpdater.cs
　├── StateResolver.cs
　├── EndJudge.cs
　├── SimulationOutput.cs
　├── OutputDriver.cs
　├── SimulationManager.cs
　├── AnimationBridge.cs
　├── DebugVisualizer.cs
　└── 📁 Editor
　　　└── SimTestSceneSetup.cs
```

3. Unityの **Project ウィンドウ**（画面下のファイル一覧）を開く
4. `Assets` フォルダを右クリック → 「フォルダを作成」→ 名前を `Scripts` にする
5. さらに `Scripts` フォルダの中に `Editor` フォルダを作る
6. **`Editor` 以外の `.cs` ファイル**（12個）を `Assets/Scripts/` にドラッグ＆ドロップ
7. **`Editor/SimTestSceneSetup.cs`** だけ `Assets/Scripts/Editor/` にドラッグ＆ドロップ

⚠️ `SimTestSceneSetup.cs` は必ず `Editor` という名前のフォルダに入れること！

Unityが自動でコンパイルを始めるので、画面下のくるくるが止まるまで待つ。

---

### 3. テストシーンを自動セットアップする

コンパイルが終わったら、Unityの上のメニューバーを見ると

```
Sim
```

という項目が増えているはず！

1. メニュー → **「Sim」** → **「① テストシーンを自動セットアップ」** をクリック
2. ダイアログが出たら「OK」
3. 次に メニュー → **「Sim」** → **「② StateConfig を7つ作成」** をクリック
4. ダイアログが出たら「OK」

これで準備完了！

---

### 4. 再生して確認する

1. 画面上部の **▶ ボタン**（再生ボタン）をクリック
2. 画面にこんなものが表示されればOK！

```
┌────────────────────────────────┐
│  State: Guarded                │
│                                │
│      ┌──────────┐              │
│      │          │  BodyTension  [████░░░░░░]  0.40
│      │  （四角）│  BodyYield    [██░░░░░░░░]  0.20
│      │          │  BreathDepth  [███░░░░░░░]  0.30
│      └──────────┘  ...
│                                │
│   ──────●──────────  ← スライダー
│   [SubA OFF]  [SubB OFF]       │
└────────────────────────────────┘
```

---

## 遊び方

| 操作 | 効果 |
|---|---|
| スライダーを右に動かす | ピストン強度が上がる |
| スライダーを左端に戻す | 停止状態になる |
| SubA をONにする | チンコ刺激（DriveBias+） |
| SubB をONにする | 乳首刺激（Resistance次第で効果が変わる） |

四角の **色・大きさ・位置** が内部パラメータに連動して変化するよ。
状態が変わると State の表示が切り替わる。

---

## うまくいかないときは

**コンパイルエラーが出る場合**
→ `Editor/SimTestSceneSetup.cs` が `Editor` フォルダに入っているか確認する

**「Sim」メニューが出ない場合**
→ 画面下にくるくるが回ってないか確認（コンパイル中）。止まるまで待つ。

**四角が動かない場合**
→ `SimulationManager` の `StateConfigs` に7つのアセットが設定されているか確認する
（「② StateConfig を7つ作成」を実行したか確認）

---

## ファイルの役割（参考）

| ファイル | 役割 |
|---|---|
| `SimulationManager.cs` | ゲームの頭脳。毎フレーム全体を統括する |
| `InputHandler.cs` | スライダーやボタンの入力を受け取る |
| `StateResolver.cs` | 状態遷移のルールが書いてある |
| `ParameterUpdater.cs` | Arousal・Resistance などを毎フレーム更新する |
| `OutputDriver.cs` | 内部パラメータをLive2D用の値に変換する |
| `DebugVisualizer.cs` | 四角の動き・色・テキスト表示を担当 |
| `AnimationBridge.cs` | Live2D本番接続用（テスト中は使わない） |
| `Editor/SimTestSceneSetup.cs` | 自動セットアップメニューを追加するやつ |
