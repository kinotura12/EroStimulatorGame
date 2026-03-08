# Live2D アニメーションクリップ一覧

## 概要

| カテゴリ | クリップ数 | 実装方法 |
|---|---|---|
| 体幹・腰 | 8 | Cubism Editor でクリップ作成 |
| NeedMotion | 2 | Cubism Editor でクリップ作成 |
| 呼吸 | 5 | Cubism Editor でクリップ作成 |
| イベント・トリガー | 10 | Cubism Editor でクリップ作成 |
| 表情・目線 | 0 | コードで直接パラメータ操作 |
| **合計** | **25** | |

---

## Animator Controller レイヤー構成

```
Layer 0: Body Base    → body_xxx を BlendTree + StateMachine
Layer 1: NeedMotion   → Additive合成、④時のみ有効
Layer 2: Breath       → CrossFadeで切り替え
Layer 3: Event/Trigger → Override合成、トリガーで単発
```

---

## Layer 0：体幹・腰（8クリップ）

速度は `PlaybackSpeed` で制御、強度は `BlendTree` で対応。

| クリップ名 | 内容 | 対応状態 |
|---|---|---|
| `body_guarded` | 硬直・緊張、ほぼ無反応 | ① Guarded |
| `body_defensive` | 拒絶、体を強張らせる | ② Defensive |
| `body_overridden` | 押し切られてる、制御失いかけ | ③ Overridden |
| `body_frustrated` | 体だけ反応してしまってる | ④ FrustratedCraving |
| `body_acclimating` | 徐々に力が抜けてくる | ⑤ Acclimating |
| `body_surrendered` | 腰が素直に動く | ⑥ Surrendered |
| `body_broken_ahegao` | 制御不能・激しい | ⑦ BrokenDown（アヘ顔） |
| `body_broken_melting` | 腰砕け・とろとろ | ⑦ BrokenDown（トロトロ） |

---

## Layer 1：NeedMotion（2クリップ）

④ FrustratedCraving 専用。体が勝手に動く表現。Additive合成。

| クリップ名 | 内容 |
|---|---|
| `need_twitch_weak` | 小さくびくびく動く |
| `need_thrust_involuntary` | 無意識に腰が動く |

---

## Layer 2：呼吸（5クリップ）

状態変化時に `CrossFade` で切り替え。

| クリップ名 | 内容 | 対応状態 |
|---|---|---|
| `breath_calm` | 緊張した浅い呼吸 | ①② |
| `breath_suppressed` | 堪えてる、息を殺してる | ②③ |
| `breath_ragged` | 乱れてきた | ③④ |
| `breath_panting` | 素直な喘ぎ | ⑤⑥ |
| `breath_broken` | 制御不能な呼吸 | ⑦ |

---

## Layer 3：イベント・トリガー（10クリップ）

トリガーで単発再生。Override合成。

| クリップ名 | 発火条件 |
|---|---|
| `react_flinch_light` | ランダムビクッ（前半・低興奮） |
| `react_flinch_heavy` | ランダムビクッ（後半・高興奮） |
| `react_orgasm_a` | End_A 射精 |
| `react_orgasm_b` | End_B 射精 |
| `react_orgasm_broken` | ⑦ BrokenDown 連続射精 |
| `react_aftershock` | Aftershock（射精直後の余韻） |
| `react_touch_subA` | SubA クリック反応 |
| `react_touch_subB_reject` | SubB クリック（Resistance 高） |
| `react_touch_subB_confused` | SubB クリック（Resistance 中） |
| `react_touch_subB_melt` | SubB クリック（Resistance 低） |

---

## コードで処理（クリップ不要）

以下の出力パラメータは `SmoothDamp` / `Lerp` で毎フレーム直接操作する。
Cubism Editor でのクリップ作成は不要。

| 出力パラメータ | 対応する表現 |
|---|---|
| `FaceHeat` | 顔の赤み・火照り |
| `EyeFocus` | 目の焦点（理性の残量） |
| `ControlMask` | 感情抑制の度合い |
| `BodyTension` | 肩・体幹の細かい緊張感 |

---

## 作業メモ

- **Cubism Editor で作るのは25クリップ**、表情系はコードで処理するので不要
- 速度変化は `PlaybackSpeed` で対応。激しい状態は別クリップ推奨（速度上げすぎると動きが破綻しやすい）
- `SimulationOutput` の各値を Animator Parameter に接続して BlendTree に食わせる
- `OutputDriver.cs` が SimulationOutput を計算しているので、そこから値を取得する
