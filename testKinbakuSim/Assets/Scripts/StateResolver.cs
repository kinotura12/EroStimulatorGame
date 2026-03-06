// StateResolver.cs
// 状態遷移の判定ロジック
// 仕様書の遷移ルールをそのまま実装

public class StateResolver
{
    // 遷移が発生したらnewStateを返す、変化なしは現在のstateを返す
    public SimState Resolve(
        SimState       currentState,
        SimParameters  param,
        InputBand      band,
        SimResolvedConfig config,
        float          aboveDuration,
        float          belowDuration,
        float          withinDuration,
        int            orgasmCount)
    {
        // エンド状態は遷移しない
        if (IsEndState(currentState)) return currentState;

        switch (currentState)
        {
            case SimState.Guarded:
                return ResolveGuarded(param, band, config, aboveDuration, belowDuration, withinDuration);

            case SimState.Defensive:
                return ResolveDefensive(param, band, config, aboveDuration, belowDuration, withinDuration);

            case SimState.Overridden:
                return ResolveOverridden(param, band, config, withinDuration, belowDuration, orgasmCount);

            case SimState.FrustratedCraving:
                return ResolveFrustratedCraving(param, band, config, withinDuration, aboveDuration, belowDuration);

            case SimState.Acclimating:
                return ResolveAcclimating(param, band, config, withinDuration, aboveDuration, belowDuration);

            case SimState.Surrendered:
                return ResolveSurrendered(param, band, config, belowDuration, orgasmCount);

            case SimState.BrokenDown:
                return ResolveBrokenDown(param, config, orgasmCount);
        }

        return currentState;
    }

    // --- 各状態の遷移判定 ---

    SimState ResolveGuarded(SimParameters p, InputBand band, SimResolvedConfig c, float above, float below, float within)
    {
        if (band == InputBand.Above && above >= c.TransitionAboveDuration)
            return SimState.Defensive;

        // Resistance上昇 → ② Defensive
        if (p.Resistance >= c.TransitionDefensiveResistanceThreshold)
            return SimState.Defensive;

        if (band == InputBand.Within && within >= c.TransitionWithinDuration && p.Arousal >= c.TransitionAcclimatingArousalThreshold)
            return SimState.Acclimating;

        if (band == InputBand.Below && below >= c.TransitionBelowDuration && p.Arousal >= c.TransitionAcclimatingArousalThreshold)
            return SimState.Acclimating;

        return SimState.Guarded;
    }

    SimState ResolveDefensive(SimParameters p, InputBand band, SimResolvedConfig c, float above, float below, float within)
    {
        if (band == InputBand.Above && above >= c.TransitionAboveDuration * 1.5f)
            return SimState.Overridden;

        if (band == InputBand.Within && within >= c.TransitionWithinDuration)
            return SimState.Guarded;

        if (band == InputBand.Below && below >= c.TransitionBelowDuration)
            return SimState.FrustratedCraving;

        return SimState.Defensive;
    }

    SimState ResolveOverridden(SimParameters p, InputBand band, SimResolvedConfig c, float within, float below, int orgasmCount)
    {
        // ③変更: Overridden突入後に規定回数以上射精 + Fatigue閾値 → End_A
        if (orgasmCount >= c.EndAOrgasmCount && p.Fatigue >= c.TransitionFatigueThreshold)
            return SimState.End_A;

        // Arousal低下 → ② Defensive
        if (p.Arousal < c.TransitionOverriddenExitArousal)
            return SimState.Defensive;

        // Within安定継続 → ⑤ Acclimating（受け入れモードへ移行）
        if (band == InputBand.Within && within >= c.TransitionWithinDuration)
            return SimState.Acclimating;

        if (band == InputBand.Below && below >= c.TransitionBelowDuration)
            return SimState.FrustratedCraving;

        return SimState.Overridden;
    }

    SimState ResolveFrustratedCraving(SimParameters p, InputBand band, SimResolvedConfig c,
        float within, float above, float below)
    {
        // Within継続 → ⑤ Acclimating（焦らし後に馴化へ戻る）
        if (band == InputBand.Within && within >= c.TransitionWithinDuration)
            return SimState.Acclimating;

        // Below執拗継続 → ⑥ Surrendered（物足りなさで降参）
        if (band == InputBand.Below && below >= c.TransitionBelowDuration && p.Arousal >= c.TransitionSurrenderedArousalThreshold)
            return SimState.Surrendered;

        // Above執拗継続 → ③ Overridden（強引に押し切られる）
        if (band == InputBand.Above && above >= c.TransitionAboveDuration)
            return SimState.Overridden;

        return SimState.FrustratedCraving;
    }

    SimState ResolveAcclimating(SimParameters p, InputBand band, SimResolvedConfig c, float within, float above, float below)
    {
        // Within安定継続 → ⑥ Surrendered
        if (band == InputBand.Within && within >= c.TransitionWithinDuration && p.Arousal >= c.TransitionSurrenderedArousalThreshold)
            return SimState.Surrendered;

        // Below執拗継続 → ⑥ Surrendered（物足りなさで降参）
        if (band == InputBand.Below && below >= c.TransitionBelowDuration && p.Arousal >= c.TransitionSurrenderedArousalThreshold)
            return SimState.Surrendered;

        // Drive閾値超え + Above執拗継続 → ⑦ BrokenDown（DriveBias変換はManagerで）
        if (band == InputBand.Above && above >= c.TransitionAboveDuration && p.Drive >= c.TransitionDriveThreshold && p.Arousal >= c.TransitionBrokenDownArousalThreshold)
            return SimState.BrokenDown;

        return SimState.Acclimating;
    }

    SimState ResolveSurrendered(SimParameters p, InputBand band, SimResolvedConfig c, float below, int orgasmCount)
    {
        // ⑥変更: Surrendered突入後に規定回数以上射精 + Fatigue閾値 → End_B
        if (orgasmCount >= c.EndBOrgasmCount && p.Fatigue >= c.TransitionFatigueThreshold)
            return SimState.End_B;

        // Drive閾値超え + Below執拗継続 → ⑦ BrokenDown（DriveBias変換はManagerで）
        if (band == InputBand.Below && below >= c.TransitionBelowDuration && p.Drive >= c.TransitionDriveThreshold && p.Arousal >= c.TransitionBrokenDownArousalThreshold)
            return SimState.BrokenDown;

        return SimState.Surrendered;
    }

    SimState ResolveBrokenDown(SimParameters p, SimResolvedConfig c, int orgasmCount)
    {
        // ⑦変更: BrokenDown突入後に規定回数以上射精 + Fatigue閾値 → End_C
        if (orgasmCount >= c.EndCOrgasmCount && p.Fatigue >= c.TransitionFatigueThreshold)
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
