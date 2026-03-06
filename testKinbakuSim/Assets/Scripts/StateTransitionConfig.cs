// StateTransitionConfig.cs
// 全状態の遷移ルールをまとめるScriptableObject

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StateTransitionConfig", menuName = "Sim/TransitionConfig")]
public class StateTransitionConfig : ScriptableObject
{
    [Tooltip("全遷移ルール（リスト上位から評価し、最初にマッチしたルールが適用される）")]
    public List<TransitionRule> rules = new();
}
