# AGENT.md

## この改修の目的
- 状態遷移条件を、現在のゲーム体験イメージに合うよう再設計する
- `main/sub` 入力に対する各パラメータの増減値を整理し、状態差と駆け引きが明確に出るようにする
- 特に `Drive` と `Resistance` を状態差の主軸にし、`EdgeTension` の駆け引きを気持ちよくする

## 最初に確認するもの
- `CHECKLIST_状態遷移とパラメータ再設計.md`
- `testKinbakuSim/Assets/Scripts/SimulationManager.cs`
- `testKinbakuSim/Assets/Scripts/SimStateConfig.cs`
- `testKinbakuSim/Assets/Scripts/SimSharedConfig.cs`
- `testKinbakuSim/Assets/Scripts/StateResolver.cs`
- `testKinbakuSim/Assets/Sim/StateTransitionConfig.asset`
- `docs/要件整理/旧反応シミュレーション設計ドキュメント.md`
- `docs/sim_visualizations/2026-03-09_状態可視化/state_archetype_affect_grid.html`

## 作業ルール
- 作業を始める前に `CHECKLIST_状態遷移とパラメータ再設計.md` を読むこと
- 作業を進めるたびに `CHECKLIST_状態遷移とパラメータ再設計.md` を更新すること
- 完了した項目、仕様変更、未解決事項、確認結果を必ず反映すること
- 実装だけ先に進めて、チェックリスト更新を後回しにしないこと
- 遷移条件と増減係数は一気に混ぜず、棚卸し → 遷移再設計 → 係数再設計 → 実装の順で進めること

## 注意点
- `Arousal` は快感量、`Drive` は理性崩壊度、`Resistance` は拒絶度として扱う
- `EdgeTension` は `Arousal` の従属値にしすぎず、駆け引きの中心として設計する
- `Defensive` と `Acclimating`、`Surrendered` と `BrokenDown` の差が十分に出ることを重視する
- `StateTransitionConfig.asset` と `SimStateConfig/SimSharedConfig` の整合が崩れないように注意する
