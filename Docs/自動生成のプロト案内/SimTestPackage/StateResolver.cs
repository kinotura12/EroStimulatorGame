// StateResolver.cs
// 状態遷移の判定ロジック
// 仕様書の遷移ルールをそのまま実装

using UnityEngine;

public class StateResolver
{
    // 遷移が発生したらnewStateを返す、変化なしは現在のstateを返す
    public SimState Resolve(
        SimState       currentState,
        SimParameters  param,
        InputBand      band,
        SimStateConfig config,
        float          aboveDuration,
        float          belowDuration,
        float          withinDuration,
        ref float      bandFlipTimer,
        InputBand      previousBand)
    {
        // エンド状態は遷移しない
        if (IsEndState(currentState)) return currentState;

        switch (currentState)
        {
            case SimState.Guarded:
                return ResolveGuarded(param, band, config, aboveDuration, withinDuration);

            case SimState.Defensive:
                return ResolveDefensive(param, band, config, aboveDuration, belowDuration, withinDuration);

            case SimState.Overridden:
                return ResolveOverridden(param, band, config, withinDuration, belowDuration);

            case SimState.FrustratedCraving:
                return ResolveFrustratedCraving(param, band, config, withinDuration, aboveDuration, belowDuration, ref bandFlipTimer, previousBand);

            case SimState.Acclimating:
                return ResolveAcclimating(param, band, config, withinDuration, aboveDuration, belowDuration);

            case SimState.Surrendered:
                return ResolveSurrendered(param, band, config, belowDuration);

            case SimState.BrokenDown:
                return ResolveBrokenDown(param, config);
        }

        return currentState;
    }

    // --- 各状態の遷移判定 ---

    SimState ResolveGuarded(SimParameters p, InputBand band, SimStateConfig c, float above, float within)
    {
        if (band == InputBand.Above && above >= c.TransitionAboveDuration)
            return SimState.Defensive;

        if (band == InputBand.Within && within >= c.TransitionWithinDuration)
            return SimState.Acclimating;

        return SimState.Guarded;
    }

    SimState ResolveDefensive(SimParameters p, InputBand band, SimStateConfig c, float above, float below, float within)
    {
        if (band == InputBand.Above && above >= c.TransitionAboveDuration * 1.5f)
            return SimState.Overridden;

        if (band == InputBand.Within && within >= c.TransitionWithinDuration)
            return SimState.Guarded;

        if (band == InputBand.Below && below >= c.TransitionBelowDuration)
            return SimState.FrustratedCraving;

        return SimState.Defensive;
    }

    SimState ResolveOverridden(SimParameters p, InputBand band, SimStateConfig c, float within, float below)
    {
        // 射精 + Fatigue閾値 → End_A（射精判定はEndJudgeで処理、ここではFatigue閾値のみ）
        if (p.Fatigue >= c.TransitionFatigueThreshold)
            return SimState.End_A;

        if (band == InputBand.Within && within >= c.TransitionWithinDuration)
            return SimState.Defensive;

        if (band == InputBand.Below && below >= c.TransitionBelowDuration)
            return SimState.FrustratedCraving;

        return SimState.Overridden;
    }

    SimState ResolveFrustratedCraving(SimParameters p, InputBand band, SimStateConfig c,
        float within, float above, float below, ref float bandFlipTimer, InputBand previousBand)
    {
        // ⑦直行条件（反転トリガー）
        bool bandFlipped = (previousBand == InputBand.Below && band == InputBand.Above)
                        || (previousBand == InputBand.Above && band == InputBand.Below);

        if (bandFlipped)
            bandFlipTimer += Time.deltaTime;
        else
            bandFlipTimer = 0f;

        if (p.FrustrationStack >= c.FrustrationStackThreshold
            && p.Drive >= c.FrustrationDriveThreshold
            && bandFlipTimer >= c.FrustrationBandFlipTime)
        {
            return SimState.BrokenDown;
        }

        // Within継続 → Arousal蓄積 → ③ Overridden（射精はEndJudgeで）
        if (band == InputBand.Within && p.Arousal >= 0.9f)
            return SimState.Overridden;

        // Below執拗 → Drive閾値 → ⑦ BrokenDown（トロトロ）
        if (band == InputBand.Below && below >= c.TransitionBelowDuration && p.Drive >= c.TransitionDriveThreshold)
            return SimState.BrokenDown;

        // Above執拗 → Drive閾値 → ⑦ BrokenDown（アヘ顔）
        if (band == InputBand.Above && above >= c.TransitionAboveDuration && p.Drive >= c.TransitionDriveThreshold)
            return SimState.BrokenDown;

        return SimState.FrustratedCraving;
    }

    SimState ResolveAcclimating(SimParameters p, InputBand band, SimStateConfig c, float within, float above, float below)
    {
        if (band == InputBand.Within && within >= c.TransitionWithinDuration && p.Arousal >= 0.8f)
            return SimState.Surrendered;

        if ((band == InputBand.Below || band == InputBand.Above)
            && p.Drive >= c.TransitionDriveThreshold)
            return SimState.BrokenDown;

        return SimState.Acclimating;
    }

    SimState ResolveSurrendered(SimParameters p, InputBand band, SimStateConfig c, float below)
    {
        if (p.Fatigue >= c.TransitionFatigueThreshold)
            return SimState.End_B;

        if (band == InputBand.Below && below >= c.TransitionBelowDuration && p.Drive >= c.TransitionDriveThreshold)
            return SimState.BrokenDown;

        if (p.Arousal < 0.3f)
            return SimState.Acclimating;

        return SimState.Surrendered;
    }

    SimState ResolveBrokenDown(SimParameters p, SimStateConfig c)
    {
        if (p.Fatigue >= c.TransitionFatigueThreshold)
        {
            return p.BrokenDownMode == BrokenDownMode.Ahegao
                ? SimState.End_C_Overload
                : SimState.End_C_White;
        }

        return SimState.BrokenDown;
    }

    bool IsEndState(SimState s) =>
        s == SimState.End_A ||
        s == SimState.End_B ||
        s == SimState.End_C_White ||
        s == SimState.End_C_Overload;
}
