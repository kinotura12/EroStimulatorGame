# 仕様変更実装チェックリスト
## 設計変更：Drive→Arousal→Resistance フロー

**設計変更サマリー:**
旧フロー: `Resistance削る → Arousal上昇 → Drive上昇`
新フロー: `Drive蓄積 → Arousalが上がりやすくなる → イクとResistanceが削れる`

**参照ドキュメント:** `docs/game_design_instructions.docx`
**対象スクリプト:** `testKinbakuSim/Assets/Scripts/`

---

## フェーズ1：フィールド追加（他作業の前提）

- [x] **1-1.** `SimSharedConfig.cs` に以下のフィールドを追加
  - `DriveArousalBoostFactor` (float, default 0.5) — Drive係数セクションに追加、SyncToStateConfigも更新済み
  - `OrgasmResistanceBaseDrop` (float, default 0.3) — 射精時効果セクションに追加（0.1→0.3に更新済み）
- [x] **1-2.** `SimStateConfig.cs` に以下のフィールドを追加
  - `DriveArousalBoostFactor` (float) — Drive係数セクションに追加（SharedConfigから同期）
  - `OrgasmResistanceDropCoefficient` (float) — 新Header「射精時 Resistance 低下（状態別係数）」として追加
- [x] **1-3.** 各状態アセットの `OrgasmResistanceDropCoefficient` を設定（BaseDrop=0.3に合わせて再調整済み）
  - ① Guarded: `1.0` ✓（実低下量0.30 / Above刺激でResiが上がりやすいため係数大）
  - ② Defensive: `1.0` ✓（実低下量0.30 / 同上）
  - ③ Overridden: `0.8` ✓（実低下量0.24）
  - ④ Frustrated: `1.2` ✓（実低下量0.36）
  - ⑤ Acclimating: `1.2` ✓（実低下量0.36）
  - ⑥ Surrendered: `1.3` ✓（実低下量0.39）
  - ⑦ BrokenDown: `1.3` ✓（実低下量0.39）

---

## フェーズ2：ParameterUpdater.cs — DriveがArousalを増幅

- [x] **2-1.** `ParameterUpdater.cs` でArousal変化量計算部分を特定
- [x] **2-2.** Drive値に応じたArousalブースト乗算を追加
  - **全4バンド（Stop / Below / Within / Above）に統一適用**
  - `arousalDelta = config.ArousalChangeXxx * dt`
  - `if (arousalDelta > 0f) arousalDelta *= 1f + param.Drive * config.DriveArousalBoostFactor`
- [x] **2-3.** Stopバンドも同パターンで統一（`ArousalChangeStop < 0` のため実質ノーオペ）
  - `arousalDelta > 0f` の条件チェックにより、負値バンドは自動的にブースト除外される
  - 将来どのバンドのArousal係数を正値に変更しても正しく動作する設計

---

## フェーズ3：EndJudge.cs — 3つの変更

### 3-A. EdgeTension上昇にResistance係数を乗算（仕様2-1）
- [x] **3-A-1.** EdgeTension填充処理（fill）の箇所を特定
- [x] **3-A-2.** Resistance係数を乗算する処理を追加
  - `resistanceMod = 1f - param.Resistance * 0.8f`
  - `EdgeTension += fillRate / duration * resistanceMod * dt`
  - ※ドレイン（中立以下）・自然減衰には適用しない

### 3-B. 射精時にResistanceを状態別係数で削る（仕様2-2）
- [x] **3-B-1.** `OnOrgasm` の箇所を特定
- [x] **3-B-2.** Resistance低下処理を追加
  - `param.Resistance -= config.OrgasmResistanceBaseDrop * config.OrgasmResistanceDropCoefficient`
  - 前提：`SimResolvedConfig` に両フィールド追加 ✓
  - 前提：`SimulationManager.ResolveRuntimeConfig` にマージ処理追加 ✓

### 3-C. メスイキ/オスイキ分岐（仕様3-4）
- [x] **3-C-1.** `EndJudge.OnOrgasm` の戻り値を `bool`（true=メスイキ）に変更
- [x] **3-C-2.** DriveBias >= 0 → オスイキ（現行処理と同一）
- [x] **3-C-3.** DriveBias < 0 → メスイキ判定（DriveBias加算処理は削除済み）
- [x] **3-C-4.** `SimulationManager` に `OnFemaleOrgasm` / `OnMaleOrgasm` イベントを追加し、戻り値で分岐して発火
- [x] **3-C-5.** `DebugVisualizer` でメスイキ時フラッシュをピンク（colorBroken流用）、オスイキ時は白に変更

---

## フェーズ4：SimulationManager.cs — BrokenDown処理変更

### 4-A. DriveBias符号固定（ロック）の撤廃（仕様3-5）
- [x] **4-A-1.** BrokenDown突入時のバンド依存DriveBias変換を特定
- [x] **4-A-2.** バンド変換 + `LockBrokenDownMode` 呼び出しを削除（SimulationManager.cs）
- [x] **4-A-3.** `BrokenDownMode` 参照箇所を全て洗い出し対処
  - `SimParameters.cs`：`BrokenDownMode` フィールドを削除、`Reset()` 該当行削除
  - `EndJudge.cs`：`LockBrokenDownMode` メソッドを削除
  - `StateResolver.cs`：`ConditionParam.BrokenDownMode` ケースを `0f` 固定に変更（廃止コメント追記）
  - `StateTransitionConfig.asset`：End_C条件を `param:6→param:5(DriveBias)` に変更（下記4-Cで対応）
- [x] **4-A-4.** BrokenDown中のDriveBiasはリアルタイムに変化し続ける（固定処理がなくなったため自動的に対応）

### 4-B. BrokenDown突入時のFatigue回復処理追加（仕様2-3）
- [x] **4-B-1.** SimulationManager.cs のBrokenDown突入ブロックに追加
  - `if (param.DriveBias >= 0f) param.Fatigue *= 0.3f`
  - マイナスモード（トロトロ）はFatigue残存 → 短命設計（意図通り）

### 4-C. エンディング判定の変更
- [x] **4-C-1.** StateTransitionConfig.asset の End_C 条件を更新
  - `DriveBias >= 0`（`param:5 op:0 threshold:0`）→ End_C_Overload（アヘ顔）
  - `DriveBias < 0`（`param:5 op:3 threshold:0`）→ End_C_White（トロトロ）
  - Fatigue到達時点のDriveBias値で判定されるため、仕様通りのリアルタイム分岐

---

## フェーズ5：StateTransitionConfig — 遷移ルール全面見直し

**遷移マップ（ドキュメント3-3）に基づき全ルールを再設定**

- [x] **5-1.** 現在の全遷移ルールをリストアップして確認
- [x] **5-2.** 以下の遷移ルールを設定（現在の実装値）

| 現在の状態 | 遷移先 | 条件（実装値） |
|---|---|---|
| ① Guarded | ② Defensive | Resistance ≥ 0.75（突入ヒステリシス） |
| ① Guarded | ④ Frustrated | Below継続5秒 + Drive ≥ 0.5 |
| ① Guarded | ⑤ Acclimating | Resistance ≤ 0.35 + Arousal ≥ 0.5 |
| ② Defensive | ① Guarded | Arousal ≤ 0.35 + Drive ≤ 0.3 + Resistance ≤ 0.5（復帰ヒステリシス） |
| ② Defensive | ③ Overridden | Fatigue ≥ 0.7 + Drive ≥ 0.5 |
| ② Defensive | ④ Frustrated | Below継続5秒 + Drive ≥ 0.5 |
| ② Defensive | ⑤ Acclimating | Resistance ≤ 0.35 + Arousal ≥ 0.5 |
| ③ Overridden | End_A | OrgasmCount ≥ 3 + Fatigue ≥ 0.9 |
| ③ Overridden | ④ Frustrated | EdgeDwellTime ≥ 20秒 + Drive ≥ 0.5 |
| ③ Overridden | ① Guarded | Arousal ≤ 0.35 + Drive ≤ 0.3 |
| ④ Frustrated | ⑦ BrokenDown | Fatigue ≥ 0.8 + Drive ≥ 0.7 + Arousal ≥ 0.6 |
| ④ Frustrated | ③ Overridden | Above継続5秒 + Fatigue ≥ 0.6 |
| ④ Frustrated | ⑤ Acclimating | Resistance ≤ 0.35 + Arousal ≥ 0.5 |
| ⑤ Acclimating | ⑥ Surrendered | Arousal ≥ 0.65 + Drive ≥ 0.5 |
| ⑤ Acclimating | ④ Frustrated | EdgeDwellTime ≥ 20秒 + Drive ≥ 0.5 |
| ⑤ Acclimating | ① Guarded | Arousal ≤ 0.35 + Drive ≤ 0.3 |
| ⑥ Surrendered | ⑦ BrokenDown | OrgasmCount ≥ 2 + Drive ≥ 0.7 + Arousal ≥ 0.6 |
| ⑥ Surrendered | ④ Frustrated | EdgeDwellTime ≥ 20秒 + Drive ≥ 0.5 |
| ⑥ Surrendered | ① Guarded | Arousal ≤ 0.35 + Drive ≤ 0.3 |
| ⑦ BrokenDown | End_C_White | OrgasmCount ≥ 3 + Fatigue ≥ 0.9 + DriveBias < 0 |
| ⑦ BrokenDown | End_C_Overload | OrgasmCount ≥ 3 + Fatigue ≥ 0.9 + DriveBias ≥ 0 |

- [x] **5-3.** ④Frustratedから①Guardedへの戻り遷移が**存在しない**ことを確認・削除

---

## ~~フェーズ6：⑤⑥⑦ 状態のEdge挙動調整~~ （廃止）

---

## フェーズ7：デバッグUI実装（状態ジャンプボタンのみ）

- [x] **7-1.** `DebugPanel.cs` を新規作成（`#if UNITY_EDITOR || DEVELOPMENT_BUILD`）
- [x] **7-2.** 状態ジャンプボタン7個を実装（OnGUI、左上固定配置）
  - [x] ① Guarded / ② Defensive / ③ Overridden / ④ Frustrated / ⑤ Acclimating / ⑥ Surrendered / ⑦ BrokenDown
- [ ] **7-3.** ボタン押下時の初期パラメータ設定（TBD：各状態突入時の適切な値を設定）
- [x] **7-4.** 現在の状態をハイライト表示（黄色 + Bold）
- [x] **7-5.** `SimulationManager.ForceState()` 追加（パラメータ維持のまま強制遷移）

---

## フェーズ8：WebGL版 スマホピンチズーム対応

- [x] **8-1.** `docs/index.html` にピンチズーム JS を追加（モバイル判定ブロック内）
  - ピンチで 0.5〜3.0 倍スケール（CSS transform）
  - ダブルタップでスケールリセット（1.0倍）
  - 2本指操作のみ対象（Unityのタッチ入力と干渉しない）

---

## 完了確認

- [ ] 全フェーズ実装完了
- [ ] Unity上でコンパイルエラーなし
- [ ] デバッグUIで全状態への強制遷移確認
- [ ] DriveをためてからArousalが上がりやすくなることを確認
- [ ] BrokenDown突入時のFatigue回復（DriveBias≥0時のみ）を確認
