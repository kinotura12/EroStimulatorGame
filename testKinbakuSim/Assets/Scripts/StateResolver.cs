// StateResolver.cs
// 状態遷移判定ロジック
// 基本遷移はデータ駆動（StateTransitionConfig）
// End判定・突入時特殊処理はコードに残す

public class StateResolver
{
    public SimState Resolve(
        SimState              currentState,
        SimParameters         param,
        InputBand             band,
        SimResolvedConfig     config,
        float                 aboveDuration,
        float                 belowDuration,
        float                 withinDuration,
        float                 stopDuration,
        int                   orgasmCount,
        StateTransitionConfig transitionConfig)
    {
        if (IsEndState(currentState)) return currentState;

        // 1. End条件（コード固定 ― 射精カウントを伴う特殊判定）
        SimState endResult = CheckEndConditions(currentState, param, config, orgasmCount);
        if (endResult != currentState) return endResult;

        // 2. データ駆動ルール（上から順、最初にマッチしたものを適用）
        if (transitionConfig != null)
        {
            foreach (var rule in transitionConfig.rules)
            {
                if (!rule.enabled) continue;
                if (rule.fromState != currentState) continue;
                if (EvaluateRule(rule, param, band, aboveDuration, belowDuration, withinDuration, stopDuration))
                    return rule.toState;
            }
        }

        return currentState;
    }

    // --- End条件（コード側に残す） ---

    SimState CheckEndConditions(SimState state, SimParameters p, SimResolvedConfig c, int orgasmCount)
    {
        switch (state)
        {
            case SimState.Overridden:
                if (orgasmCount >= c.EndAOrgasmCount && p.Fatigue >= c.TransitionFatigueThreshold)
                    return SimState.End_A;
                break;

            case SimState.Surrendered:
                if (orgasmCount >= c.EndBOrgasmCount && p.Fatigue >= c.TransitionFatigueThreshold)
                    return SimState.End_B;
                break;

            case SimState.BrokenDown:
                if (orgasmCount >= c.EndCOrgasmCount && p.Fatigue >= c.TransitionFatigueThreshold)
                    return p.BrokenDownMode == BrokenDownMode.Ahegao
                        ? SimState.End_C_Overload
                        : SimState.End_C_White;
                break;
        }
        return state;
    }

    // --- ルール評価 ---

    bool EvaluateRule(
        TransitionRule rule,
        SimParameters  param,
        InputBand      band,
        float aboveDuration, float belowDuration, float withinDuration, float stopDuration)
    {
        // Band継続条件チェック
        if (rule.requiredBand != BandRequirement.Any)
        {
            if (band != ToInputBand(rule.requiredBand)) return false;

            float duration = GetDuration(rule.requiredBand, aboveDuration, belowDuration, withinDuration, stopDuration);
            if (duration < rule.bandDuration) return false;
        }

        // パラメータ閾値条件チェック（全てAND）
        if (rule.conditions != null)
        {
            foreach (var cond in rule.conditions)
            {
                if (!EvaluateCondition(cond, param)) return false;
            }
        }

        return true;
    }

    bool EvaluateCondition(TransitionCondition cond, SimParameters p)
    {
        float value = cond.param switch
        {
            ConditionParam.Arousal    => p.Arousal,
            ConditionParam.Resistance => p.Resistance,
            ConditionParam.Fatigue    => p.Fatigue,
            ConditionParam.Drive      => p.Drive,
            _                         => 0f,
        };

        return cond.op switch
        {
            CompareOp.GreaterEqual => value >= cond.threshold,
            CompareOp.LessEqual    => value <= cond.threshold,
            CompareOp.GreaterThan  => value >  cond.threshold,
            CompareOp.LessThan     => value <  cond.threshold,
            _                      => false,
        };
    }

    InputBand ToInputBand(BandRequirement req) => req switch
    {
        BandRequirement.Stop   => InputBand.Stop,
        BandRequirement.Below  => InputBand.Below,
        BandRequirement.Within => InputBand.Within,
        BandRequirement.Above  => InputBand.Above,
        _                      => InputBand.Stop,
    };

    float GetDuration(BandRequirement req, float above, float below, float within, float stop) => req switch
    {
        BandRequirement.Above  => above,
        BandRequirement.Below  => below,
        BandRequirement.Within => within,
        BandRequirement.Stop   => stop,
        _                      => 0f,
    };

    bool IsEndState(SimState s) =>
        s == SimState.End_A ||
        s == SimState.End_B ||
        s == SimState.End_C_White ||
        s == SimState.End_C_Overload;
}
