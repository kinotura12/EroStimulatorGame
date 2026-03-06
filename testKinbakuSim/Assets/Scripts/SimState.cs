// SimState.cs
// ゲーム内の状態定義

public enum SimState
{
    Guarded,           // ① 初期状態：心を閉ざして冷や汗
    Defensive,         // ② 不機嫌・拒絶
    Overridden,        // ③ 強い快感で押し切られる
    FrustratedCraving, // ④ お預け：体だけ求めてしまう
    Acclimating,       // ⑤ 馴化：身体が受け入れていく
    Surrendered,       // ⑥ 通常快楽落ち
    BrokenDown,        // ⑦ 理性崩壊・サービスタイム

    End_A,             // グッタリエンド（③でFatigue閾値）
    End_B,             // 快楽落ちエンド（⑥でFatigue閾値）
    End_C_White,       // とろけ落ちエンド（⑦トロトロ）
    End_C_Overload,    // アヘ顔崩壊エンド（⑦アヘ顔）
}

public enum InputBand
{
    Stop,   // isActive == false
    Below,  // MainIntensity < TolLow
    Within, // TolLow <= MainIntensity <= TolHigh
    Above,  // MainIntensity > TolHigh
}

public enum BrokenDownMode
{
    None,
    Ahegao,  // DriveBias > 0
    Melting, // DriveBias < 0
}
