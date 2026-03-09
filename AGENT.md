# AGENT.md

このプロトタイプで大きめの改修作業を行う場合は、まず `docs/change_tasks/AGENT.md` を読み、その運用ルールに従って作業を進めてください。

## フォルダ構成の簡易案内

- `testKinbakuSim/`
  - Unity プロジェクト本体
  - `Assets/Scripts/` に実装コード
  - `Assets/Sim/` に実運用のシミュレーション設定アセットと、Bridge 連動中の HTML
- `docs/`
  - テストプレイ用の `index.html` とビルド済みゲーム実体を置く場所
  - 改修管理や可視化資料もここで管理する
- `docs/change_tasks/`
  - 大きめの改修作業ごとの作業フォルダ置き場
  - `YYYY-MM-DD_改修内容/` ごとに `AGENT.md` と `CHECKLIST_改修作業名.md` を持つ
- `docs/sim_visualizations/`
  - シミュレーション可視化用の HTML / Markdown / 履歴資料
  - Bridge 固定参照ではない可視化資料は基本的にここで管理する
- `.github/`
  - GitHub 関連設定
- `.claude/`
  - ローカル運用補助用ファイル

## AI 向け補足

- 実装を変えるときは、まず `testKinbakuSim/Assets/Scripts/` と `testKinbakuSim/Assets/Sim/` を見る
- 大きめの改修は `docs/change_tasks/` 側の作業フォルダで進める
- 可視化用 HTML を探すときは `docs/sim_visualizations/` と `testKinbakuSim/Assets/Sim/` の両方を確認する
- `Assets/Sim/state_machine.html` と `Assets/Sim/param_dashboard_final.html` は Bridge 連動中なので、移動時はコード側確認が必要

## ルール

- 大きめの改修作業では、`docs/change_tasks/` 配下に作業フォルダを作成すること
- 作業フォルダ名は `YYYY-MM-DD_改修内容` 形式にすること
- 各作業フォルダには必ず以下を作成すること
  - `AGENT.md`
  - `CHECKLIST_改修作業名.md`
- 実装・調査・仕様変更を進めるたびに、チェックリストを更新すること

## docs 配下について

- `docs/` には `change_tasks/` のほか、テストプレイ用の `index.html` とビルド済みのゲーム実体が入る
- テストプレイ用のビルドを行った際は、`docs/` 配下にそのビルド成果物を配置すること

## 参照先

- `docs/change_tasks/AGENT.md`
