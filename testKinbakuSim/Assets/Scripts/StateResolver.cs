// StateResolver.cs
// 状態遷移判定ロジック
// 全遷移はデータ駆動（StateTransitionConfig）

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

        // データ駆動ルール（上から順、最初にマッチしたものを適用）
        if (transitionConfig != null)
        {
            foreach (var rule in transitionConfig.rules)
            {
                if (!rule.enabled) continue;
                if (rule.fromState != currentState) continue;
                if (EvaluateRule(rule, param, band, aboveDuration, belowDuration, withinDuration, stopDuration, orgasmCount))
                    return rule.toState;
            }
        }

        return currentState;
    }

    // --- ルール評価 ---

    bool EvaluateRule(
        TransitionRule rule,
        SimParameters  param,
        InputBand      band,
        float aboveDuration, float belowDuration, float withinDuration, float stopDuration,
        int orgasmCount)
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
                if (!EvaluateCondition(cond, param, orgasmCount)) return false;
            }
        }

        return true;
    }

    bool EvaluateCondition(TransitionCondition cond, SimParameters p, int orgasmCount)
    {
        float value = cond.param switch
        {
            ConditionParam.Arousal       => p.Arousal,
            ConditionParam.Resistance    => p.Resistance,
            ConditionParam.Fatigue       => p.Fatigue,
            ConditionParam.Drive         => p.Drive,
            ConditionParam.OrgasmCount   => (float)orgasmCount,
            ConditionParam.DriveBias     => p.DriveBias,
            ConditionParam.BrokenDownMode => (float)p.BrokenDownMode,
            _                            => 0f,
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
