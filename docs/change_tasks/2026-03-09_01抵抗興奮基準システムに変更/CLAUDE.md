# Claude Code 作業指示

## このプロジェクトについて

Unity製ゲームのシミュレーションパラメータ管理システムの改修プロジェクト。
スクリプトは `testKinbakuSim/Assets/Scripts/` にある。

## 作業チェックリスト

**実装作業は `IMPLEMENTATION_CHECKLIST.md` で管理する。**

### チェックリスト更新のルール

1. **タスクに着手したとき** → そのチェックボックスを `- [x]` に変更する
2. **タスクを完了したとき** → チェックボックスが `- [x]` になっていることを確認する
3. **作業を中断・終了するとき** → 次に取り組む未完了タスクに `<!-- 次回ここから -->` コメントを付ける
4. **新たな作業項目が発生したとき** → 適切なフェーズにタスクを追記する

### 更新タイミング

- コードを1ファイル編集し終えたら、対応するチェックボックスを更新する
- セッション終了前に必ずチェックリストの状態を最新にする

## プロジェクト構造

```
v0.1.1_プロトタイプ/
├── CLAUDE.md                    ← このファイル
├── IMPLEMENTATION_CHECKLIST.md  ← 作業チェックリスト
├── docs/
│   └── game_design_instructions.docx  ← 設計変更指示書
└── testKinbakuSim/Assets/Scripts/
    ├── SimulationManager.cs     ← メインオーケストレーター
    ├── SimParameters.cs         ← パラメータデータ
    ├── ParameterUpdater.cs      ← パラメータ更新ロジック
    ├── StateResolver.cs         ← 状態遷移判定
    ├── EndJudge.cs              ← 射精・エンディング判定
    ├── SimStateConfig.cs        ← 状態別設定（ScriptableObject）
    ├── SimSharedConfig.cs       ← グローバル共有設定
    ├── SimResolvedConfig.cs     ← 実行時マージ設定
    ├── StateTransitionConfig.cs ← 遷移ルール集
    ├── TransitionRule.cs        ← 遷移ルール定義
    ├── OutputDriver.cs          ← Live2D出力変換
    └── InputHandler.cs          ← 入力管理
```

## 設計変更の核心

**旧フロー:** Resistance削る → Arousal上昇 → Drive上昇
**新フロー:** Drive蓄積 → Arousalが上がりやすくなる → イクとResistanceが削れる

重要な設計意図：
- **②Defensiveの係数0.1が肝** → 何度イカせてもResistanceがほぼ崩れない主戦場
- **BrokenDown突入時のDriveBias固定は廃止** → リアルタイムで変化し続ける
- **④Frustratedからの①Guarded戻り遷移は存在しない**

## コーディング規約

- C# / Unity 標準に従う
- デバッグ用コードは `#if UNITY_EDITOR || DEBUG_MODE` で囲む
- ScriptableObjectのフィールド追加時は `[Header("")]` でグループ分けする
