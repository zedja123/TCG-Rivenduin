﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using UnityEngine.Events;
using TcgEngine.UI;

namespace TcgEngine.Client
{
    /// <summary>
    /// Script that contain main controls for clicking on cards, attacking, activating abilities
    /// Holds the currently selected card and will send action to GameClient on click release
    /// </summary>

    public class PlayerControls : MonoBehaviour
    {
        private BoardCard selected_card = null;

        private static PlayerControls instance;

        void Awake()
        {
            instance = this;
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            if (Input.GetMouseButtonDown(1))
                UnselectAll();

            if (selected_card != null)
            {
                if (Input.GetMouseButtonUp(0))
                    ReleaseClick();
            }
        }

        public void SelectCard(BoardCard bcard)
        {
            Game gdata = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();
            Card card = bcard.GetFocusCard();

            if (gdata.IsPlayerSelectorTurn(player) && gdata.selector == SelectorType.SelectTarget)
            {
                //Target selector, select this card
                GameClient.Get().SelectCard(card);
            }
            else if (gdata.IsPlayerActionTurn(player) && card.player_id == player.player_id)
            {
                //Start dragging card
                selected_card = bcard;
            }
        }

        public void SelectCardRight(BoardCard card)
        {
            if (!Input.GetMouseButton(0))
            {
                //Nothing on right-click
            }
        }

        private void ReleaseClick()
        {
            bool yourturn = GameClient.Get().IsYourTurn();

            if (yourturn && selected_card != null)
            {
                Card card = selected_card.GetCard();
                Vector3 wpos = GameBoard.Get().RaycastMouseBoard();
                BSlot tslot = BSlot.GetNearest(wpos);
                Card target = tslot?.GetSlotCard(wpos);
                AbilityButton ability = AbilityButton.GetFocus(wpos, 1f);

                if (ability != null && ability.IsVisible())
                {
                    GameClient.Get().CastAbility(card, ability.GetAbility());
                }
                else if (tslot is BoardSlotPlayer)
                {
                    if (card.exhausted)
                        WarningText.ShowExhausted();
                    else
                        GameClient.Get().AttackPlayer(card, tslot.GetPlayer());
                }
                else if (target != null && target.uid != card.uid && target.player_id != card.player_id)
                {
                    if(card.exhausted)
                        WarningText.ShowExhausted();
                    else
                        GameClient.Get().AttackTarget(card, target);
                }
                else if (tslot != null && tslot is BoardSlot)
                {
                    GameClient.Get().Move(card, tslot.GetSlot());
                }
            }

            UnselectAll();
        }

        public void UnselectAll()
        {
            selected_card = null;
        }

        public BoardCard GetSelected()
        {
            return selected_card;
        }

        public static PlayerControls Get()
        {
            return instance;
        }
    }
}