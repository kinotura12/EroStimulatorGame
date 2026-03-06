// ParameterUpdater.cs
// パラメータを毎フレーム更新する
// 係数はすべてSimStateConfigから取得

using UnityEngine;

public class ParameterUpdater
{
    // --- SubB効果計算 ---
    // Resistanceの高低でDriveBiasの方向が変わる
    public float CalcSubBEffect(float resistance, SimStateConfig config)
    {
        // Resistance高い → プラス方向（不快だが無視できない）
        // Resistance低い → マイナス方向（感じてしまってる）
        // 中間 → ±0付近で揺れる（困惑ゾーン）
        float direction = Mathf.Lerp(1f, -1f, 1f - resistance);
        return direction * 0.03f; // 係数は後でConfigに移動してもOK
    }

    public void Update(
        SimParameters    param,
        InputBand        band,
        bool             subAActive,
        bool             subBActive,
        SimStateConfig   config,
        float            deltaTime,
        ref float        aboveDuration,
        ref float        belowDuration,
        ref float        withinDuration,
        ref float        driveRampTimer)
    {
        switch (band)
        {
            case InputBand.Stop:
                UpdateStop(param, config, deltaTime);
                // タイマーリセット
                aboveDuration  = 0f;
                belowDuration  = 0f;
                withinDuration = 0f;
                driveRampTimer = 0f;
                break;

            case InputBand.Below:
                UpdateBelow(param, config, deltaTime, ref driveRampTimer);
                aboveDuration  = 0f;
                withinDuration = 0f;
                belowDuration += deltaTime;
                break;

            case InputBand.Within:
                UpdateWithin(param, config, deltaTime);
                aboveDuration  = 0f;
                belowDuration  = 0f;
                driveRampTimer = 0f;
                withinDuration += deltaTime;
                break;

            case InputBand.Above:
                UpdateAbove(param, config, deltaTime, ref driveRampTimer);
                belowDuration  = 0f;
                withinDuration = 0f;
                aboveDuration += deltaTime;
                break;
        }

        // SubA効果
        if (subAActive)
        {
            param.Arousal   = Clamp01(param.Arousal   + 0.04f * deltaTime);
            param.Resistance= Clamp01(param.Resistance - 0.01f * deltaTime);
            param.DriveBias += 0.02f * deltaTime;

            // SubBと同時ONのとき、SubAのDriveBias+効果はArousal増加に変換
            if (subBActive)
            {
                param.DriveBias -= 0.02f * deltaTime; // 打ち消し
                param.Arousal   = Clamp01(param.Arousal + 0.02f * deltaTime);
            }
        }

        // SubB効果
        if (subBActive)
        {
            float subBEffect = CalcSubBEffect(param.Resistance, config);
            param.DriveBias += subBEffect * deltaTime;

            // Below文脈でSubB執拗継続 → Drive小増（StateResolverで判定）
        }

        // DriveBias クランプ
        param.DriveBias = Mathf.Clamp(param.DriveBias, -1f, 1f);
    }

    // --- 各Band処理 ---

    void UpdateStop(SimParameters param, SimStateConfig config, float dt)
    {
        param.Arousal   = Clamp01(param.Arousal   - config.ArousalDecayStop  * dt);
        param.Resistance= Clamp01(param.Resistance - config.ResistanceDecayStop * dt);
        param.Fatigue   = Clamp01(param.Fatigue   - config.FatigueDecayStop  * dt);
        param.Drive     = Clamp01(param.Drive     - config.DriveDecayStop    * dt);
        param.DriveBias = Mathf.MoveTowards(param.DriveBias, 0f, config.DriveBiasDecayStop * dt);
    }

    void UpdateBelow(SimParameters param, SimStateConfig config, float dt, ref float driveRampTimer)
    {
        param.Arousal   = Clamp01(param.Arousal   + config.ArousalGainBelow    * dt);
        param.Resistance= Clamp01(param.Resistance - config.ResistanceDecayBelow * dt);
        param.Fatigue   = Clamp01(param.Fatigue   - 0.005f * dt); // 微減
        param.DriveBias = Mathf.MoveTowards(param.DriveBias, -1f, config.DriveBiasShiftBelow * -1f * dt);

        driveRampTimer += dt;
        if (driveRampTimer >= config.DriveTimeBeforeGain)
            param.Drive = Clamp01(param.Drive + config.DriveGainBelow * dt);
    }

    void UpdateWithin(SimParameters param, SimStateConfig config, float dt)
    {
        param.Arousal   = Clamp01(param.Arousal   + config.ArousalGainWithin  * dt);
        param.Resistance= Clamp01(param.Resistance - config.ResistanceDecayWithin * dt);
        param.Fatigue   = Clamp01(param.Fatigue   + config.FatigueGainWithin  * dt * config.FatigueMultiplier);
        param.Drive     = Clamp01(param.Drive     - config.DriveDecayWithin   * dt);
        param.DriveBias = Mathf.MoveTowards(param.DriveBias, 0f, config.DriveBiasDecayWithin * dt);
    }

    void UpdateAbove(SimParameters param, SimStateConfig config, float dt, ref float driveRampTimer)
    {
        param.Arousal   = Clamp01(param.Arousal   + config.ArousalGainAbove   * dt);
        param.Resistance= Clamp01(param.Resistance + config.ResistanceRiseAbove * dt);
        param.Fatigue   = Clamp01(param.Fatigue   + config.FatigueGainAbove   * dt * config.FatigueMultiplier);
        param.DriveBias = Mathf.MoveTowards(param.DriveBias, 1f, config.DriveBiasShiftAbove * dt);

        driveRampTimer += dt;
        if (driveRampTimer >= config.DriveTimeBeforeGain)
            param.Drive = Clamp01(param.Drive + config.DriveGainAbove * dt);
    }

    float Clamp01(float v) => Mathf.Clamp01(v);
}
