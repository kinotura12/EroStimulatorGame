# AGENT.md

このフォルダは、シミュレーションの可視化用 HTML / Markdown / 補助資料を保管する場所です。

## 基本ルール

- 新しい可視化資料や履歴を追加するときは、`YYYY-MM-DD_内容` 形式のサブフォルダを作成すること
- 検討用・履歴用・比較用の HTML/MD はこのフォルダで管理すること
- 大きめの改修作業に紐づく可視化資料でも、ゲーム実行に直接使わないものはこのフォルダに置いてよい

## 置いてよいもの

- 状態図
- パラメータ可視化 HTML
- 検討用 Markdown
- 旧版・履歴版の可視化資料

## `Assets/Sim` に残すもの

以下は Unity の Bridge 機能や実運用アセットと連動するため、原則として `Assets/Sim` に残すこと。

- `StateTransitionConfig.asset`
- `SimSharedConfig.asset`
- `StateConfigs/`
- `state_machine.html`
- `param_dashboard_final.html`

## 注意点

- `Tools -> Bridge` 系のコードで固定パス参照されている HTML は、勝手に `docs/sim_visualizations` へ移動しないこと
- 参照先を変える場合は、必ず関連する Editor スクリプト側のパスも更新すること
