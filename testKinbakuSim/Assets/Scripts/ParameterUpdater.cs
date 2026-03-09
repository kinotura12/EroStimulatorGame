// ParameterUpdater.cs
// パラメータを毎フレーム更新する
// 係数はすべてSimStateConfigから取得

using UnityEngine;

public class ParameterUpdater
{

    public void Update(
        SimParameters    param,
        SimState         currentState,
        InputBand        band,
        bool             subAActive,
        bool             subBActive,
        SimResolvedConfig config,
        float            deltaTime,
        ref float        aboveDuration,
        ref float        belowDuration,
        ref float        withinDuration,
        ref float        stopDuration,
        ref float        driveRampTimer)
    {
        switch (band)
        {
            case InputBand.Stop:
                UpdateStop(param, config, deltaTime);
                stopDuration  += deltaTime;
                aboveDuration  = 0f;
                belowDuration  = 0f;
                withinDuration = 0f;
                driveRampTimer = 0f;
                break;

            case InputBand.Below:
                UpdateBelow(param, config, deltaTime, ref driveRampTimer);
                stopDuration   = 0f;
                aboveDuration  = 0f;
                withinDuration = 0f;
                belowDuration += deltaTime;
                break;

            case InputBand.Within:
                UpdateWithin(param, config, deltaTime);
                stopDuration   = 0f;
                aboveDuration  = 0f;
                belowDuration  = 0f;
                driveRampTimer = 0f;
                withinDuration += deltaTime;
                break;

            case InputBand.Above:
                UpdateAbove(param, config, deltaTime, ref driveRampTimer);
                stopDuration   = 0f;
                belowDuration  = 0f;
                withinDuration = 0f;
                aboveDuration += deltaTime;
                break;
        }

        // SubA効果
        if (subAActive)
        {
            param.Drive      = Clamp01(param.Drive      + config.SubADriveChange      * deltaTime);
            param.Arousal    = Clamp01(param.Arousal    + config.SubAArousalChange    * deltaTime);
            param.Resistance = Clamp01(param.Resistance + config.SubAResistanceChange * deltaTime);
        }

        // SubB効果
        if (subBActive)
        {
            param.Drive      = Clamp01(param.Drive      + config.SubBDriveChange      * deltaTime);
            param.Arousal    = Clamp01(param.Arousal    + config.SubBArousalChange    * deltaTime);
            param.Resistance = Clamp01(param.Resistance + config.SubBResistanceChange * deltaTime);
        }

        // DriveBias（A:+方向 B:-方向 を合算、同時ONのとき絶対値を－方向に強制）
        if (subAActive || subBActive)
        {
            float biasDelta = 0f;
            if (subAActive) biasDelta += config.SubADriveBiasGain;
            if (subBActive) biasDelta += config.SubBDriveBiasGain;
            if (subAActive && subBActive)
                biasDelta = -Mathf.Abs(biasDelta);
            param.DriveBias += biasDelta * deltaTime;
        }

        // 同時ONボーナスArousal（SubBothArousalChange > 0 のとき有効）
        if (subAActive && subBActive && config.SubBothArousalChange > 0f)
            param.Arousal = Clamp01(param.Arousal + config.SubBothArousalChange * deltaTime);

        // DriveBias クランプ
        param.DriveBias = Mathf.Clamp(param.DriveBias, -1f, 1f);

        // NeedMotion 更新（新仕様）
        UpdateNeedMotion(param, currentState, band, subAActive, subBActive, config, deltaTime);
    }

    // --- 各Band処理 ---

    void UpdateStop(SimParameters param, SimResolvedConfig config, float dt)
    {
        // 上昇時のみDriveブースト（通常はArousalChangeStop < 0 なので実質無効）
        float arousalDelta = config.ArousalChangeStop * dt;
        if (arousalDelta > 0f)
            arousalDelta *= 1f + param.Drive * config.DriveArousalBoostFactor;
        param.Arousal    = Clamp01(param.Arousal    + arousalDelta);
        param.Resistance = Clamp01(param.Resistance + config.ResistanceChangeStop * dt);
        param.Fatigue    = Clamp01(param.Fatigue    + config.FatigueChangeStop    * dt);
        param.Drive      = Clamp01(param.Drive      + config.DriveChangeStop      * dt);
        // 符号に依存せず「減衰速度」として扱う
        param.DriveBias  = Mathf.MoveTowards(param.DriveBias, 0f, Mathf.Abs(config.DriveBiasDecayStop) * dt);
    }

    void UpdateBelow(SimParameters param, SimResolvedConfig config, float dt, ref float driveRampTimer)
    {
        // Driveが高いほどArousal上昇率がブースト（上昇時のみ適用）
        float arousalDelta = config.ArousalChangeBelow * dt;
        if (arousalDelta > 0f)
            arousalDelta *= 1f + param.Drive * config.DriveArousalBoostFactor;
        param.Arousal    = Clamp01(param.Arousal    + arousalDelta);
        param.Resistance = Clamp01(param.Resistance + config.ResistanceChangeBelow  * dt);
        param.Fatigue    = Clamp01(param.Fatigue    + config.FatigueChangeBelow     * dt);
        // Below帯は常にマイナス方向へ寄せる（速度は絶対値）
        param.DriveBias  = Mathf.MoveTowards(param.DriveBias, -1f, Mathf.Abs(config.DriveBiasShiftBelow) * dt);
        // Driveは遅延なしで即時反映
        param.Drive = Clamp01(param.Drive + config.DriveChangeBelow * dt);
    }

    void UpdateWithin(SimParameters param, SimResolvedConfig config, float dt)
    {
        // Driveが高いほどArousal上昇率がブースト（上昇時のみ適用）
        float arousalDelta = config.ArousalChangeWithin * dt;
        if (arousalDelta > 0f)
            arousalDelta *= 1f + param.Drive * config.DriveArousalBoostFactor;
        param.Arousal    = Clamp01(param.Arousal    + arousalDelta);
        param.Resistance = Clamp01(param.Resistance + config.ResistanceChangeWithin  * dt);
        param.Fatigue    = Clamp01(param.Fatigue    + config.FatigueChangeWithin     * dt * config.FatigueMultiplier);
        param.Drive      = Clamp01(param.Drive      + config.DriveChangeWithin       * dt); // 0=保持
        // 符号に依存せず「減衰速度」として扱う
        param.DriveBias  = Mathf.MoveTowards(param.DriveBias, 0f, Mathf.Abs(config.DriveBiasDecayWithin) * dt);
    }

    void UpdateAbove(SimParameters param, SimResolvedConfig config, float dt, ref float driveRampTimer)
    {
        // Driveが高いほどArousal上昇率がブースト（上昇時のみ適用）
        float arousalDelta = config.ArousalChangeAbove * dt;
        if (arousalDelta > 0f)
            arousalDelta *= 1f + param.Drive * config.DriveArousalBoostFactor;
        param.Arousal    = Clamp01(param.Arousal    + arousalDelta);
        param.Resistance = Clamp01(param.Resistance + config.ResistanceChangeAbove   * dt);
        param.Fatigue    = Clamp01(param.Fatigue    + config.FatigueChangeAbove      * dt * config.FatigueMultiplier);
        // Above帯は常にプラス方向へ寄せる（速度は絶対値）
        param.DriveBias  = Mathf.MoveTowards(param.DriveBias, 1f, Mathf.Abs(config.DriveBiasShiftAbove) * dt);
        // Driveは遅延なしで即時反映
        param.Drive = Clamp01(param.Drive + config.DriveChangeAbove * dt);
    }

    void UpdateNeedMotion(
        SimParameters param,
        SimState currentState,
        InputBand band,
        bool subAActive,
        bool subBActive,
        SimResolvedConfig config,
        float dt)
    {
        // 「興奮強い状態」: Aboveでのみ減少、Within帯はキープ
        bool isHighExcitement =
            currentState == SimState.Overridden ||
            currentState == SimState.Surrendered ||
            currentState == SimState.BrokenDown;

        float delta = 0f;

        switch (band)
        {
            case InputBand.Stop:
                // Arousal高い or Sub入力あり → StopArousedChange、それ以外 → StopCalmChange（負=減少）
                if (param.Arousal >= config.NeedMotionArousalThreshold || subAActive || subBActive)
                    delta = config.NeedMotionStopArousedChange * dt;
                else
                    delta = config.NeedMotionStopCalmChange * dt;
                break;

            case InputBand.Below:
                delta = config.NeedMotionBelowChange * dt;
                break;

            case InputBand.Within:
                // isHighExcitement: キープ（変化なし）
                if (!isHighExcitement)
                    delta = config.NeedMotionWithinChange * dt;  // 正=増加、負=減少
                break;

            case InputBand.Above:
                // 全状態で変化（負値=減少）
                delta = config.NeedMotionAboveChange * dt;
                break;
        }

        param.NeedMotion = Clamp01(param.NeedMotion + delta);
    }

    float Clamp01(float v) => Mathf.Clamp01(v);
}
