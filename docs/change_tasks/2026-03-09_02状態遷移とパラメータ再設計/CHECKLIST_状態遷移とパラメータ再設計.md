# 改修チェックリスト

## 改修目的
- 状態遷移条件とパラメータ増減係数の大規模見直しを行う
- ただし両方を同時にいじらず、`状態の意味固定 -> 遷移条件整理 -> 増減係数調整` の 3 段で進める
- `Arousal=快感量`、`Drive=理性崩壊度`、`Resistance=拒絶度` を明確に分離し、状態差と駆け引きを改善する
- `EdgeTension` を Main 強弱の駆け引きの中心として機能させる

## 完了条件
- [ ] 各状態の意味が文章で固定されている
- [ ] 各状態の入口条件 / 出口条件が整理されている
- [ ] `main/sub` 入力に対する各パラメータ増減方針が整理されている
- [ ] `StateTransitionConfig` / `SimSharedConfig` / `SimStateConfig` の修正が実装されている
- [ ] デバッグ強制遷移と実プレイ感で新しい状態差が確認できる
- [ ] End 条件と主要遷移が破綻していない

## 進め方
- [x] フェーズ1: 現行設定の棚卸しを行う
- [x] フェーズ2: 各状態の意味を固定する
- [ ] フェーズ3: 遷移条件だけを整理する
- [ ] フェーズ4: 各パラメータの増減係数を調整する
- [ ] フェーズ5: 実装反映とデバッグ確認を行う

## フェーズ1: 現行設定の棚卸し

### 1-1. 遷移条件の問題点整理
- [x] `StateTransitionConfig.asset` の現行ルールを一覧化する
- [x] 各状態の入口条件 / 出口条件が、意図に対してどうズレているか整理する
- [x] `Surrendered -> End_B` の扱いを現状確認する

### 1-2. パラメータ変化の問題点整理
- [x] `SimSharedConfig` / `SimStateConfig` の現行係数を一覧化する
- [x] `main` 入力に対する `Arousal / EdgeTension / Resistance / Fatigue / Drive / DriveBias` の変化の問題点を整理する
- [x] `subA / subB` 入力に対する各パラメータ変化の問題点を整理する

### 1-3. 意図とのズレ整理
- [x] ユーザー定義のパラメータ意図と現行実装との差分を整理する
- [x] 状態差が弱い箇所を洗い出す
- [x] 駆け引きが弱い箇所を洗い出す

### フェーズ1の棚卸し結果

#### 遷移条件の問題点
- `Guarded -> Acclimating`、`Defensive -> Acclimating`、`FrustratedCraving -> Acclimating` がすべてほぼ同じ `Resistance低 + Arousal高` 条件で、状態ごとの意味差が弱い
- `Overridden -> Guarded` と `Surrendered -> Guarded` がどちらも `Arousal低 + Drive低` 系で、戻り先が単純すぎる
- `FrustratedCraving -> BrokenDown` が `Fatigue高 + Drive高 + Arousal高` の即時条件で、`Frustrated` らしい「焦らし蓄積」より疲労条件に寄りすぎている
- `Surrendered -> End_B` は現行 `StateTransitionConfig.asset` に存在しない
- End 条件の `OrgasmCount` は shared config で `EndA=2 / EndB=2 / EndC=3` だが、asset 側は `Overridden -> End_A` が `3回` になっていて不整合がある

#### main 入力に対する各パラメータ変化の問題点
- `Drive` は `Below / Above` で即時上昇しており、`DriveChangeDelay` は実装上使われていない
- `Arousal` は `DriveArousalBoostFactor` により Drive が高いほど伸びるため、快感量と理性崩壊度が強く連動しすぎやすい
- `Fatigue` は shared/state ともに `Within=0 / Above=0.02` が中心で、長期戦や End 制御軸としては軽い
- `BrokenDown` を含め、`FatigueMultiplier` がほぼ全状態で `1` のままで、後半状態の差が弱い
- `Resistance` は `Above` で上がりやすいが、後半状態でも多くが shared に近く、`Defensive` と `Acclimating` の差が十分大きくない
- `Tol` 以外の状態差分が少なく、Band 判定以外の「状態らしさ」が弱い

#### sub 入力に対する各パラメータ変化の問題点
- `SubA / SubB` の Drive 増加量が同じで、理性崩壊への寄与差が薄い
- `SubA / SubB` の Arousal 増加量差も小さく、入力キャラ差が弱い
- shared asset では `SubBResistanceChange = -0.01` になっており、ユーザー意図の「乱暴だとResistance増、優しいと低下」とズレる可能性がある
- 多くの状態で `SubA / SubB` 設定が shared 準拠のままで、状態別の sub 反応差がほとんどない

#### ユーザー意図とのズレ
- `Defensive` と `Acclimating` の差が、現状では `Resistance` と `Drive` より `Arousal` 条件に引っ張られやすい
- `Surrendered` と `BrokenDown` の差が、`Drive / Fatigue / DriveBias` より End 条件だけに寄りがち
- `EdgeTension` は重要パラメータだが、現段階の棚卸しでは遷移条件との接続が弱く、駆け引きの中核になり切れていない
- `Fatigue` を高 Drive 状態で軽くする方向性は未反映
- `DriveBias` は End 分岐では使われているが、途中状態の質感差としてはまだ弱い

## フェーズ2: 各状態の意味を固定する

### 2-1. 状態ごとの理想挙動シート
- [x] `状態理想挙動入力シート.md` の記入内容を確認する
- [x] Guarded
- [x] Defensive
- [x] Overridden
- [x] FrustratedCraving
- [x] Acclimating
- [x] Surrendered
- [x] BrokenDown

### 2-2. 各状態で明文化する項目
- [x] 拒絶 / 受容
- [x] Drive の高さ
- [x] Arousal の位置づけ
- [x] 疲労感
- [x] 気持ちいい入力 / 嫌な入力

### フェーズ2の固定内容

#### 状態差の核
- `Guarded / Defensive / Overridden / FrustratedCraving` は高反抗ルートの見どころ
- `Acclimating / Surrendered` は懐柔・受容ルートの見どころ
- `BrokenDown` は `Surrendered` の上位ではなく、理性崩壊と Bias 分岐が主役の最終局面

#### 各状態の固定ポイント
- `Guarded`: 緊張した制御状態。快感は低く、Drive はかなり低い。Defensive か Acclimating への分岐起点
- `Defensive`: 強い拒絶と苛立ち。Arousal は上がっても受容しない。`Above` で反発、`Below` で欲求不満化
- `Overridden`: 強い刺激で押し切られる状態。快感より「押し切られ感」を優先
- `FrustratedCraving`: 拒絶のまま身体だけ求める欲求不満状態。疲労より焦らし蓄積が核
- `Acclimating`: 快感を認め始める順応状態。Surrendered へ向かう前の駆け引きの主舞台
- `Surrendered`: 受容完成状態。BrokenDown と違い、まだ理性の残量がある
- `BrokenDown`: 理性崩壊状態。Bias による質感差とサービスタイム感が重要

#### フェーズ3へ持ち越す重要論点
- `Guarded -> Defensive` は反発増大の主ルートとして維持したい
- `Defensive -> Acclimating` は軽すぎると高反抗ルートの見どころを壊す
- `FrustratedCraving` は `Fatigue高` より `焦らし蓄積 / 欲求不満` を主因にしたい
- `Acclimating -> Surrendered` はすぐ飛ばず、快感の駆け引きを挟みたい
- `Surrendered -> BrokenDown` は受容完成後の理性崩壊として見せたい
- `BrokenDown` は通常状態へ戻さない前提で整理したい

## フェーズ3: 遷移条件だけを整理する

### 3-1. 入口条件 / 出口条件の整理
- [x] 各状態の入口条件を見直す
- [x] 各状態の出口条件を見直す
- [x] `Arousal >= x / Drive >= x / Resistance <= x / Band継続y秒` を基準に整理する
- [x] この段階では増減係数はまだ触らない

### 3-2. 状態差の重点確認
- [x] `Defensive` と `Acclimating` の差を `Resistance` と `Drive` で出せているか確認する
- [x] `Surrendered` と `BrokenDown` の差を `Drive / Fatigue / DriveBias` で出せているか確認する

### フェーズ3の確定内容: 遷移条件

#### 基本方針
- `Arousal` は「遷移タイミングが不自然にならないための補助条件」として使う
- 状態差の主軸は `Resistance` と `Drive`
- `FrustratedCraving` は `Fatigue` より `欲求不満 / 焦らし蓄積` を優先
- `Acclimating -> Surrendered` は OrgasmCount マイルストーンを挟む（OrgasmCount >= 1）
- `BrokenDown` は通常状態へ戻さない
- `EdgeDwellTime` は `param: 6`（BrokenDownMode廃止済みによる繰り上げ後の値）

#### ① Guarded
- `Guarded -> Defensive`
  - `Resistance >= 0.75`
- `Guarded -> Acclimating`
  - `Resistance <= 0.32` AND `Drive >= 0.30` AND `Arousal >= 0.50`
- `Guarded -> FrustratedCraving`
  - `Below` 継続 `5s` AND `Drive >= 0.50` AND `EdgeDwellTime >= 6s`
  - ※「初回除外」条件は実装不可のため閾値で代用

#### ② Defensive
- `Defensive -> Overridden`（優先）
  - `Drive >= 0.50` AND `OrgasmCount >= 1` AND `Fatigue >= 0.60`
- `Defensive -> FrustratedCraving`
  - `Below` 継続 `4s` AND `Drive >= 0.45` AND `EdgeDwellTime >= 6s`
- `Defensive -> Acclimating`（重い経路、残す）
  - `Resistance <= 0.25` AND `Drive >= 0.40` AND `Arousal >= 0.70`
- `Defensive -> Guarded`（後退、最後に評価）
  - `Resistance <= 0.55` AND `Drive <= 0.28` AND `Arousal <= 0.35`

#### ③ Overridden
- `Overridden -> End_A`（優先）
  - `OrgasmCount >= 3` AND `Fatigue >= 0.90`
- `Overridden -> FrustratedCraving`
  - `Below` 継続 `4s` AND `Drive >= 0.50` AND `EdgeDwellTime >= 6s`
- `Overridden -> Acclimating`
  - `Resistance <= 0.30` AND `Fatigue <= 0.25` AND `Arousal >= 0.60` AND `Drive >= 0.45`
- `Overridden -> Defensive`（後退）
  - `Arousal <= 0.38` AND `Drive <= 0.38` AND `Resistance >= 0.45`

#### ④ FrustratedCraving
- `FrustratedCraving -> BrokenDown`（優先）
  - `Drive >= 0.80` AND `OrgasmCount >= 2` AND `Arousal >= 0.60`
  - ※ Fatigue は必須条件から外す
- `FrustratedCraving -> Overridden`
  - `Above` 継続 `4s` AND `Drive >= 0.50` AND `OrgasmCount >= 1` AND `Fatigue >= 0.60`
- `FrustratedCraving -> Acclimating`
  - `Resistance <= 0.30` AND `Fatigue <= 0.25` AND `Arousal >= 0.70` AND `Drive >= 0.45`
- `FrustratedCraving -> Guarded`
  - なし

#### ⑤ Acclimating
- `Acclimating -> Surrendered`（優先）
  - `OrgasmCount >= 1` AND `Resistance <= 0.22`
- `Acclimating -> FrustratedCraving`
  - `Below` 継続 `4s` AND `Drive >= 0.50` AND `EdgeDwellTime >= 6s`
- `Acclimating -> Guarded`（緊急後退、閾値は低め）
  - `Arousal <= 0.20` AND `Drive <= 0.25`
- `Acclimating -> BrokenDown`
  - なし（Surrendered 経由のみ）

#### ⑥ Surrendered
- `Surrendered -> BrokenDown [トロトロ]`（優先）
  - `Below` 継続 `3s` AND `Drive >= 0.90` AND `DriveBias < 0` AND `OrgasmCount >= 3`
- `Surrendered -> BrokenDown [アヘ]`（優先）
  - `Above` 継続 `3s` AND `Drive >= 0.90` AND `DriveBias >= 0` AND `OrgasmCount >= 3`
- `Surrendered -> End_B`
  - `OrgasmCount >= 2` AND `Fatigue >= 0.88`
- `Surrendered -> Acclimating`（後退）
  - `Drive <= 0.35`
- `Surrendered -> Guarded`
  - なし

#### ⑦ BrokenDown
- `BrokenDown -> End_C_White`
  - `OrgasmCount >= 3` AND `Fatigue >= 0.90` AND `DriveBias < 0`
- `BrokenDown -> End_C_Overload`
  - `OrgasmCount >= 3` AND `Fatigue >= 0.90` AND `DriveBias >= 0`
- `BrokenDown -> 他通常状態`
  - なし

## フェーズ4: 各パラメータの増減係数を調整する

### 4-1. パラメータごとの設計順
- [x] フェーズ4初期数値案を `設計書_状態遷移とパラメータ.md` の「9. フェーズ4 数値設計案」に作成する
- [x] shared 基準値と状態別 override 方針を分けて整理する
- [ ] `Drive` と `Resistance` を先に調整する
- [ ] 次に `Arousal` と `EdgeTension` を調整する
- [ ] 最後に `Fatigue` を調整する
- [ ] `DriveBias` は最後に調整する

### 4-2. パラメータ別の役割確認
- [ ] `Arousal`: 瞬間的な気持ちよさ。状態差より入力反応差を作る軸
- [ ] `EdgeTension`: 射精欲。駆け引きの中心。独立性を保つ
- [ ] `Resistance`: 拒絶 / 受容の心理軸。状態遷移の横軸
- [ ] `Fatigue`: 長期消耗と End 用。短時間で効きすぎないようにする
- [ ] `Drive`: 理性崩壊の進行度。状態遷移の縦軸
- [ ] `DriveBias`: メス / オス寄りの質感。主遷移より分岐演出向け

### 4-3. 重要な調整方針
- [ ] `EdgeTension` を `Arousal` の従属にしすぎない
- [ ] 「感じている」だけではなく「感じながら我慢できなくなる」過程を作る
- [ ] 高 Drive 状態では `Fatigue` が溜まりにくい案を検討する
- [ ] 後半状態では `Fatigue` の増え方、または End 到達余裕を調整する
- [ ] `subA / subB` の個性を意図に沿って出す

## フェーズ5: 実装反映と確認
- [ ] `StateTransitionConfig.asset` を更新する
- [ ] `SimSharedConfig.asset` を更新する
- [ ] 必要に応じて各 `StateConfig_*.asset` を更新する
- [ ] 可視化ドキュメントを更新する
- [ ] デバッグ強制遷移で状態感を確認する
- [ ] テストプレイで遷移テンポと駆け引きを確認する

## 決定事項
- `Arousal` は快感量として扱う
- `Drive` は理性崩壊度として扱う
- `Resistance` は拒絶度として扱う
- 右側の可視化マップは `Drive-Resistance Map` を採用済み
- 強制遷移デバッグは、遷移条件から自動算出する代表プリセット方式に変更済み
- 大規模改修は `棚卸し -> 状態理想挙動整理 -> 遷移条件再設計 -> 係数再設計 -> 実装` の順で進める

## 保留事項 / 未解決事項
- `Surrendered -> End_B` を仕様として復活させるか、現行 asset どおり削除状態を維持するか
- `Fatigue` を高 Drive 状態でどこまで溜まりにくくするか
- `subA / subB` の個性をどの程度強く分けるか
- `BrokenDown` への代表流入ルートを 1 本だけで扱うか、複数ルートをサポートするか

## 実装後の確認
- [ ] `Guarded / Defensive / Overridden / Frustrated / Acclimating / Surrendered / BrokenDown` の体感差が出ている
- [ ] `EdgeTension` の上下が Main 強弱の駆け引きとして機能する
- [ ] `Defensive` で反発、`Acclimating` で受容の差が分かる
- [ ] `Surrendered` と `BrokenDown` の理性崩壊差が分かる
- [ ] End 条件に自然に到達する

## 作業ログ
- 2026-03-09: 改修用フォルダと運用ファイルを作成
- 2026-03-09: 事前合意として、`Arousal=快感量`、`Drive=理性崩壊度`、`Resistance=拒絶度` を採用
- 2026-03-09: 現状棚卸し開始。`SimSharedConfig` と `StateTransitionConfig` を再確認
- 2026-03-09: 初期所見として、Drive 系の共通増加量が弱く状態差が出にくい、Fatigue が全体的に軽く End 制御軸として薄い、SubB の役割再確認が必要と判断
- 2026-03-09: フェーズ2用に `状態理想挙動入力シート.md` を追加。次回以降はこの記入内容を参照して理想挙動シートを確定する
- 2026-03-09: 現行実装・`docs/sim_visualizations/2026-03-09_状態可視化/state_archetype_affect_grid.html` を参照して、`状態理想挙動入力シート.md` に暫定入力を実施
- 2026-03-09: フェーズ3確定済みの遷移条件と入力シートを前提に、`設計書_状態遷移とパラメータ.md` にフェーズ4の初期数値案を追記
