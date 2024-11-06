using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TcgEngine
{
    /// <summary>
    /// Resolve abilties and actions one by one, with an optional delay in between each
    /// </summary>

    public class ResolveQueue 
    {
        private Pool<CardQueueElement> card_elem_pool = new Pool<CardQueueElement>();
        private Pool<AbilityQueueElement> ability_elem_pool = new Pool<AbilityQueueElement>();
        private Pool<SecretQueueElement> secret_elem_pool = new Pool<SecretQueueElement>();
        private Pool<AttackQueueElement> attack_elem_pool = new Pool<AttackQueueElement>();
        private Pool<CallbackQueueElement> callback_elem_pool = new Pool<CallbackQueueElement>();
        private Stack<AbilityQueueElement> ability_queue = new Stack<AbilityQueueElement>();
        private Stack<SecretQueueElement> secret_queue = new Stack<SecretQueueElement>();
        private Stack<AttackQueueElement> attack_queue = new Stack<AttackQueueElement>();
        private Stack<CallbackQueueElement> callback_queue = new Stack<CallbackQueueElement>();
        private Stack<CardQueueElement> card_elem_queue = new Stack<CardQueueElement>();

        private bool stack = false;

        private Game game_data;
        private bool is_resolving = false;
        private float resolve_delay = 0f;
        private bool skip_delay = false;

        public ResolveQueue(Game data, bool skip)
        {
            game_data = data;
            skip_delay = skip;
        }

        public void SetData(Game data)
        {
            game_data = data;
        }

        public virtual void Update(float delta)
        {
            this.stack = game_data.response_phase != ResponsePhase.Response;
            if (resolve_delay > 0f)
            {
                resolve_delay -= delta;
                if (resolve_delay <= 0f)
                    ResolveAll();
            }
        }

        public virtual void AddCard(Card caster, Player owner, Slot slot, Action<Card, Player, Slot> callback)
        {
            if (caster != null && owner != null)
            {
                CardQueueElement elem = card_elem_pool.Create();
                elem.caster = caster;
                elem.owner = owner;
                elem.slot = slot;
                elem.callback = callback;
                card_elem_queue.Push(elem);
            }
        }

        public virtual void AddAbility(AbilityData ability, Card caster, Card triggerer, Action<AbilityData, Card, Card> callback)
        {
            if (ability != null && caster != null)
            {
                AbilityQueueElement elem = ability_elem_pool.Create();
                elem.caster = caster;
                elem.triggerer = triggerer;
                elem.ability = ability;
                elem.callback = callback;
                ability_queue.Push(elem);
            }
        }

        public virtual void AddAttack(Card attacker, Card target, Action<Card, Card, bool> callback, bool skip_cost = false)
        {
            if (attacker != null && target != null)
            {
                AttackQueueElement elem = attack_elem_pool.Create();
                elem.attacker = attacker;
                elem.target = target;
                elem.ptarget = null;
                elem.skip_cost = skip_cost;
                elem.callback = callback;
                attack_queue.Push(elem);
            }
        }

        public virtual void AddAttack(Card attacker, Player target, Action<Card, Player, bool> callback, bool skip_cost = false)
        {
            if (attacker != null && target != null)
            {
                AttackQueueElement elem = attack_elem_pool.Create();
                elem.attacker = attacker;
                elem.target = null;
                elem.ptarget = target;
                elem.skip_cost = skip_cost;
                elem.pcallback = callback;
                attack_queue.Push(elem);
            }
        }

        public virtual void AddSecret(AbilityTrigger secret_trigger, Card secret, Card trigger, Action<AbilityTrigger, Card, Card> callback)
        {
            if (secret != null && trigger != null)
            {
                SecretQueueElement elem = secret_elem_pool.Create();
                elem.secret_trigger = secret_trigger;
                elem.secret = secret;
                elem.triggerer = trigger;
                elem.callback = callback;
                secret_queue.Push(elem);
            }
        }

        public virtual void AddCallback(Action callback)
        {
            if (callback != null)
            {
                CallbackQueueElement elem = callback_elem_pool.Create();
                elem.callback = callback;
                callback_queue.Push(elem);
            }
        }

        public virtual void Resolve(bool stack = false)
        {
            if (ability_queue.Count > 0)
            {
                //Resolve Ability
                AbilityQueueElement elem = ability_queue.Pop();
                ability_elem_pool.Dispose(elem);
                elem.callback?.Invoke(elem.ability, elem.caster, elem.triggerer);
            }
            else if (secret_queue.Count > 0)
            {
                //Resolve Secret
                SecretQueueElement elem = secret_queue.Pop();
                secret_elem_pool.Dispose(elem);
                elem.callback?.Invoke(elem.secret_trigger, elem.secret, elem.triggerer);
            }
            else if (attack_queue.Count > 0)
            {
                //Resolve Attack
                AttackQueueElement elem = attack_queue.Pop();
                attack_elem_pool.Dispose(elem);
                if (elem.ptarget != null)
                    elem.pcallback?.Invoke(elem.attacker, elem.ptarget, elem.skip_cost);
                else
                    elem.callback?.Invoke(elem.attacker, elem.target, elem.skip_cost);
            }
            else if (callback_queue.Count > 0)
            {
                CallbackQueueElement elem = callback_queue.Pop();
                callback_elem_pool.Dispose(elem);
                elem.callback.Invoke();
            }
            else if (stack && card_elem_queue.Count > 0)
            {
                //Resolve Card
                CardQueueElement elem = card_elem_queue.Pop();
                card_elem_pool.Dispose(elem);
                elem.callback?.Invoke(elem.caster, elem.owner, elem.slot);
            }
            else if (callback_queue.Count > 0)
            {
            }
        }

        public virtual void ResolveAll(float delay)
        {
            SetDelay(delay);
            ResolveAll();  //Resolve now if no delay
        }

        public virtual void ResolveAll(bool force_stack = false)
        {
            if (is_resolving)
                return;

            is_resolving = true;
            while (CanResolve(force_stack))
            {
                Resolve(force_stack);
            }
            is_resolving = false;
        }

        public virtual void SetDelay(float delay)
        {
            if (!skip_delay)
            {
                resolve_delay = Mathf.Max(resolve_delay, delay);
            }
        }

        public virtual bool CanResolve(bool canresolve)
        {
            if (resolve_delay > 0f)
                return false;   //Is waiting delay
            if (game_data.state == GameState.GameEnded)
                return false; //Cant execute anymore when game is ended
            if (game_data.selector != SelectorType.None)
                return false; //Waiting for player input, in the middle of resolve loop
            return (stack && card_elem_queue.Count > 0) || attack_queue.Count > 0 || ability_queue.Count > 0 || secret_queue.Count > 0 || callback_queue.Count > 0;
        }

        public virtual bool IsResolving()
        {
            return is_resolving || resolve_delay > 0f;
        }

        public virtual void Clear()
        {
            card_elem_pool.DisposeAll();
            card_elem_queue.Clear();
            /*attack_elem_pool.DisposeAll();
            ability_elem_pool.DisposeAll();
            secret_elem_pool.DisposeAll();
            callback_elem_pool.DisposeAll();*/
            attack_queue.Clear();
            ability_queue.Clear();
            secret_queue.Clear();
            callback_queue.Clear();
        }
        public Stack<CardQueueElement> GetCardQueue()
        {
            return card_elem_queue;
        }

        public Stack<AttackQueueElement> GetAttackQueue()
        {
            return attack_queue;
        }

        public Stack<AbilityQueueElement> GetAbilityQueue()
        {
            return ability_queue;
        }

        public Stack<SecretQueueElement> GetSecretQueue()
        {
            return secret_queue;
        }

        public Stack<CallbackQueueElement> GetCallbackQueue()
        {
            return callback_queue;
        }
    }

    public class AbilityQueueElement
    {
        public AbilityData ability;
        public Card caster;
        public Card triggerer;
        public Action<AbilityData, Card, Card> callback;
    }

    public class AttackQueueElement
    {
        public Card attacker;
        public Card target;
        public Player ptarget;
        public bool skip_cost;
        public Action<Card, Card, bool> callback;
        public Action<Card, Player, bool> pcallback;
    }

    public class SecretQueueElement
    {
        public AbilityTrigger secret_trigger;
        public Card secret;
        public Card triggerer;
        public Action<AbilityTrigger, Card, Card> callback;
    }

    public class CallbackQueueElement
    {
        public Action callback;
    }

    public class CardQueueElement
    {
        public Card caster;
        public Slot slot;
        public Player owner;
        public Action<Card, Player, Slot> callback;
    }

}
