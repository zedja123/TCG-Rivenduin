using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Base class for all ability effects, override the IsConditionMet function
    /// </summary>
    
    public class EffectData : ScriptableObject
    {
        public virtual void DoEffect(GameLogic logic, AbilityData ability, Card caster)
        {
            //Server side gameplay logic
        }

        public virtual void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            //Server side gameplay logic
        }

        public virtual void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            //Server side gameplay logic
        }

        public virtual void DoEffect(GameLogic logic, AbilityData ability, Card caster, Slot target)
        {
            //Server side gameplay logic
        }

        public virtual void DoEffect(GameLogic logic, AbilityData ability, Card caster, CardData target)
        {
            //Server side gameplay logic
        }

        public virtual void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            //Ongoing effect only
        }

        public virtual void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            //Ongoing effect only
        }
    }
}