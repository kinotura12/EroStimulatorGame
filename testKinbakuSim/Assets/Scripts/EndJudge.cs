// EndJudge.cs
// 射精・エンド条件の判定

using UnityEngine;

public class EndJudge
{
    // Arousal が閾値以上 = エッジモード突入
    // エッジモード中: mainIntensity の強弱がそのままEdgeTensionに反映
    //   TolHigh より強い → 蓄積、TolHigh より弱い → 減衰、ちょうどで静止
    // エッジモード外: EdgeDecayRate/秒で自然減衰
    public bool UpdateOrgasm(SimParameters param, SimResolvedConfig config, float mainIntensity, float dt)
    {
        float threshold = config.OrgasmThreshold * config.OrgasmThresholdMultiplier;
        bool  isEdging  = param.Arousal >= threshold;

        if (isEdging)
        {
            param.EdgeDwellTime += dt;
            float neutral = config.EdgeNeutralIntensity;
            float diff    = mainIntensity - neutral;
            if (diff >= 0f)
            {
                // 中立以上 → 増加（neutral〜1.0 の幅で正規化、カーブ適用）
                float headroom       = Mathf.Max(0.01f, 1f - neutral);
                float normalizedDiff = diff / headroom;  // 0〜1
                float curvedDiff     = Mathf.Pow(normalizedDiff, Mathf.Max(0.01f, config.EdgeFillCurve));  // 指数<1: 中立直上で急増、遠くなるほど緩やか
                float fillRate       = curvedDiff * config.EdgeFillRate;
                float duration       = Mathf.Max(0.01f, config.WithholdDuration);
                param.EdgeTension    = Mathf.Clamp01(param.EdgeTension + fillRate / duration * dt);
            }
            else
            {
                // 中立以下 → エッジモード内でも減衰（じらし効果）
                float drainFrac   = (-diff) / Mathf.Max(0.01f, neutral);
                param.EdgeTension = Mathf.Clamp01(param.EdgeTension - drainFrac * config.EdgeDrainRate * dt);
            }
        }
        else
        {
            // エッジモード外は自然減衰
            param.EdgeTension = Mathf.Clamp01(param.EdgeTension - config.EdgeDecayRate * dt);
        }

        // 累積オーガズムは常時緩やかに減衰
        param.CumulativeOrgasm = Mathf.Clamp01(param.CumulativeOrgasm - config.OrgasmCumulativeDecayRate * dt);

        // EdgeTension最大維持タイマー
        if (param.EdgeTension >= 1f)
        {
            param.EdgeTension = 1f;  // 上限でクランプ（減衰ロジックは引き続き動く）
            param.EdgePeakTimer += dt;
            if (param.EdgePeakTimer >= config.EdgePeakHoldDuration)
            {
                // 滞在時間からOrgasmScaleを確定（長いほど激しい）+ CumulativeOrgasmのボーナス加算
                float baseScale      = Mathf.Clamp01(param.EdgeDwellTime / Mathf.Max(0.1f, config.EdgeDwellScaleMax));
                param.OrgasmScale    = Mathf.Clamp01(baseScale + param.CumulativeOrgasm * config.OrgasmCumulativeBonusScale);
                param.EdgeTension    = 0f;
                param.EdgeDwellTime  = 0f;
                param.EdgePeakTimer  = 0f;
                return true;
            }
        }
        else
        {
            // 最大値を下回ったらタイマーリセット（維持できなかった）
            param.EdgePeakTimer = 0f;
        }
        return false;
    }

    // 射精後の処理
    public void OnOrgasm(SimParameters param, SimResolvedConfig config)
    {
        // Arousalは完全リセットせず閾値以下にクランプ（BreathDepthの大幅低下を防ぐ）
        param.Arousal = Mathf.Min(param.Arousal, config.OrgasmArousalResetTo);
        // 射精時に疲弊をぐっと増やす
        param.Fatigue = Mathf.Clamp01(param.Fatigue + config.OrgasmFatigueGain);
        // 累積オーガズム加算（OrgasmScaleが大きいほどより多く蓄積）
        param.CumulativeOrgasm = Mathf.Clamp01(param.CumulativeOrgasm + param.OrgasmScale * config.OrgasmCumulativeGain);
    }

    // BrokenDown突入時にDriveBiasでモードをロック
    public void LockBrokenDownMode(SimParameters param)
    {
        param.BrokenDownMode = param.DriveBias >= 0f
            ? BrokenDownMode.Ahegao
            : BrokenDownMode.Melting;
    }
}
