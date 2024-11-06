using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CardStack", order = 10)]
    public class ConditionCardStack : ConditionData
    {
        [Header("Card is in stack")]
        public ConditionOperatorBool oper;

        public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            return data.history_list.Count > 0;
        }
    }
}