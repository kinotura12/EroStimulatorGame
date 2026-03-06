// EndJudge.cs
// 射精・エンド条件の判定

using UnityEngine;

public class EndJudge
{
    public bool CheckOrgasm(SimParameters param, SimStateConfig config)
    {
        float threshold = config.OrgasmThreshold * config.OrgasmThresholdMultiplier;
        return param.Arousal >= threshold;
    }

    // 射精後の処理
    public void OnOrgasm(SimParameters param)
    {
        param.Arousal = 0f; // リセット
        // FatigueはParameterUpdaterで蓄積済み
    }

    // BrokenDown突入時にDriveBiasでモードをロック
    public void LockBrokenDownMode(SimParameters param)
    {
        param.BrokenDownMode = param.DriveBias >= 0f
            ? BrokenDownMode.Ahegao
            : BrokenDownMode.Melting;
    }
}
