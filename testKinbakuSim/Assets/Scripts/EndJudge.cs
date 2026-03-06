// EndJudge.cs
// 射精・エンド条件の判定

using UnityEngine;

public class EndJudge
{
    public bool CheckOrgasm(SimParameters param, SimResolvedConfig config)
    {
        float threshold = config.OrgasmThreshold * config.OrgasmThresholdMultiplier;
        return param.Arousal >= threshold;
    }

    // 射精後の処理
    public void OnOrgasm(SimParameters param, SimResolvedConfig config)
    {
        // Arousalは完全リセットせず閾値以下にクランプ（BreathDepthの大幅低下を防ぐ）
        param.Arousal = Mathf.Min(param.Arousal, config.OrgasmArousalResetTo);
        // 射精時に疲弊をぐっと増やす
        param.Fatigue = Mathf.Clamp01(param.Fatigue + config.OrgasmFatigueGain);
    }

    // BrokenDown突入時にDriveBiasでモードをロック
    public void LockBrokenDownMode(SimParameters param)
    {
        param.BrokenDownMode = param.DriveBias >= 0f
            ? BrokenDownMode.Ahegao
            : BrokenDownMode.Melting;
    }
}
