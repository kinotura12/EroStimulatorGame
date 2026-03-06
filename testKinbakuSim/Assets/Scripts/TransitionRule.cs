// TransitionRule.cs
// 状態遷移ルールのデータ構造定義

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>パラメータ閾値条件のパラメータ種類</summary>
public enum ConditionParam
{
    Arousal,    // 0
    Resistance, // 1
    Fatigue,    // 2
    Drive,      // 3
}

/// <summary>比較演算子</summary>
public enum CompareOp
{
    GreaterEqual, // >= 0
    LessEqual,    // <= 1
    GreaterThan,  // >  2
    LessThan,     // <  3
}

/// <summary>Band継続条件（Any=Band不問）</summary>
public enum BandRequirement
{
    Any,    // 0 Band不問
    Stop,   // 1
    Below,  // 2
    Within, // 3
    Above,  // 4
}

/// <summary>1つのパラメータ閾値条件（AND結合で使う）</summary>
[Serializable]
public class TransitionCondition
{
    public ConditionParam param     = ConditionParam.Arousal;
    public CompareOp      op        = CompareOp.GreaterEqual;
    [Range(0f, 1f)]
    public float          threshold = 0.5f;
}

/// <summary>1本の遷移ルール</summary>
[Serializable]
public class TransitionRule
{
    [Tooltip("この遷移の意図・理由（表示用メモ）")]
    public string note = "";

    [Tooltip("このルールを有効にするか（OFFにすると評価をスキップ）")]
    public bool enabled = true;

    public SimState fromState = SimState.Guarded;
    public SimState toState   = SimState.Defensive;

    [Tooltip("Band継続条件。Any=Band不問")]
    public BandRequirement requiredBand = BandRequirement.Any;

    [Tooltip("必要Band継続時間（秒）。0=即時")]
    [Min(0f)]
    public float bandDuration = 0f;

    [Tooltip("パラメータ閾値条件（全てAND）")]
    public List<TransitionCondition> conditions = new();
}
