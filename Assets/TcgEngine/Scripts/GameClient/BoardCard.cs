﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;
using UnityEngine.Events;
using TcgEngine.UI;
using TcgEngine.FX;

namespace TcgEngine.Client
{
    /// <summary>
    /// Represents the visual aspect of a card on the board.
    /// Will take the data from Card.cs and display it
    /// </summary>

    public class BoardCard : MonoBehaviour
    {
        public SpriteRenderer card_sprite;
        public SpriteRenderer card_glow;
        public SpriteRenderer card_shadow;

        public Image armor_icon;
        public Text armor;

        public CanvasGroup status_group;
        public Text status_text;

        public BoardCardEquip equipment;

        public AbilityButton[] buttons;

        public Color glow_ally;
        public Color glow_enemy;

        public UnityAction onKill;

        private CardUI card_ui;
        private BoardCardFX card_fx;
        private Canvas canvas;

        private string card_uid = "";
        private bool destroyed = false;
        private bool focus = false;
        private float timer = 0f;
        private float status_alpha_target = 0f;

        private bool back_to_hand;
        private Vector3 back_to_hand_target;

        private static List<BoardCard> card_list = new List<BoardCard>();

        void Awake()
        {
            card_list.Add(this);
            card_ui = GetComponent<CardUI>();
            card_fx = GetComponent<BoardCardFX>();
            canvas = GetComponentInChildren<Canvas>();
            card_glow.color = new Color(card_glow.color.r, card_glow.color.g, card_glow.color.b, 0f);
            canvas.gameObject.SetActive(false);
            status_alpha_target = 0f;

            if (equipment != null)
                equipment.Hide();

            if (status_group != null)
                status_group.alpha = 0f;
        }

        void OnDestroy()
        {
            card_list.Remove(this);
        }

        private void Start()
        {
            //Random slight rotation
            Vector3 board_rot = GameBoard.Get().GetAngles();
            transform.rotation = Quaternion.Euler(board_rot.x, board_rot.y, board_rot.z + Random.Range(-1f, 1f));
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            timer += Time.deltaTime;
            if (timer > 0.15f && !destroyed && !canvas.gameObject.activeSelf)
                canvas.gameObject.SetActive(true);

            PlayerControls controls = PlayerControls.Get();
            Game data = GameClient.Get().GetGameData();
            Player player = GameClient.Get().GetPlayer();
            Card card = data.GetCard(card_uid);
            if(!destroyed)
                card_ui.SetCard(card);

            bool selected = controls.GetSelected() == this;
            Vector3 targ_pos = GetTargetPos();
            float speed = 12f;

            transform.position = Vector3.MoveTowards(transform.position, targ_pos, speed * Time.deltaTime);

            float target_alpha = IsFocus() || selected ? 1f : 0f;
            if (destroyed || timer < 1f)
                target_alpha = 0f;
            if (equipment != null && equipment.IsFocus())
                target_alpha = 0f;

            Color ccolor = player.player_id == card.player_id ? glow_ally : glow_enemy;
            float calpha = Mathf.MoveTowards(card_glow.color.a, target_alpha * ccolor.a, 4f * Time.deltaTime);
            card_glow.color = new Color(ccolor.r, ccolor.g, ccolor.b, calpha);
            card_shadow.enabled = !destroyed && timer > 0.4f;
            card_sprite.color = card.HasStatus(StatusType.Stealth) ? Color.gray : Color.white;
            card_ui.hp.color = (destroyed || card.damage > 0) ? Color.yellow : Color.white;

            //armor
            int armor_val = card.GetStatusValue(StatusType.Armor);
            armor.text = armor_val.ToString();
            armor.enabled = armor_val > 0;
            armor_icon.enabled = armor_val > 0;

            //Update card image
            Sprite sprite = card.CardData.GetBoardArt(card.VariantData);
            if (sprite != card_sprite.sprite)
                card_sprite.sprite = sprite;

            //Update frame image
            Sprite frame = card.VariantData.frame_board;
            if (frame != null && card_ui.frame_image != null)
                card_ui.frame_image.sprite = frame;

            //Equipment
            if (equipment != null)
            {
                Card equip = data.GetEquipCard(card.equipped_uid);
                equipment.SetEquip(equip);
            }

            //Ability buttons
            foreach (AbilityButton button in buttons)
                button.Hide();

            if (selected && card.player_id == player.player_id)
            {
                int index = 0;
                List<AbilityData> abilities = card.GetAbilities();
                foreach (AbilityData iability in abilities)
                {
                    if (iability && data.CanCastAbility(card, iability))
                    {
                        if (iability.target != AbilityTarget.Self || iability.AreTargetConditionsMet(data, card, card))
                        {
                            if (index < buttons.Length)
                            {
                                AbilityButton button = buttons[index];
                                button.SetAbility(card, iability);
                            }
                            index++;
                        }
                    }
                }
            }

            //Status bar
            if (status_group != null)
                status_group.alpha = Mathf.MoveTowards(status_group.alpha, status_alpha_target, 5f * Time.deltaTime);
        }

        private Vector3 GetTargetPos()
        {
            Game data = GameClient.Get().GetGameData();
            Card card = data.GetCard(card_uid);

            if (destroyed && back_to_hand && timer > 0.5f)
                return back_to_hand_target;

            BSlot slot = BSlot.Get(card.slot);
            if (slot != null)
            {
                Vector3 targ_pos = slot.GetPosition(card.slot);
                return targ_pos;
            }

            return transform.position;
        }

        public void SetCard(Card card)
        {
            this.card_uid = card.uid;

            transform.position = GetTargetPos();

            CardData icard = CardData.Get(card.card_id);
            if (icard)
            {
                card_ui.SetCard(card);
                card_sprite.sprite = icard.GetBoardArt(card.VariantData);
                armor.enabled = false;
                armor_icon.enabled = false;
                status_alpha_target = 0f;
            }
        }

        public void SetOrder(int order)
        {
            card_sprite.sortingOrder = order;
            canvas.sortingOrder = order + 1;
        }

        public void Kill()
        {
            if (!destroyed)
            {
                Game data = GameClient.Get().GetGameData();
                Card card = data.GetCard(card_uid);
                Player player = data.GetPlayer(card.player_id);

                destroyed = true;
                timer = 0f;
                status_alpha_target = 0f;
                card_glow.enabled = false;
                card_shadow.enabled = false;

                SetOrder(card_sprite.sortingOrder - 2);
                Destroy(gameObject, 1.3f);

                TimeTool.WaitFor(0.8f, () =>
                {
                    canvas.gameObject.SetActive(false);
                });

                GameBoard board = GameBoard.Get();
                if (player.HasCard(player.cards_hand, card) || player.HasCard(player.cards_deck, card))
                {
                    back_to_hand = true;
                    back_to_hand_target = player.player_id == GameClient.Get().GetPlayerID() ? -board.transform.up : board.transform.up;
                    back_to_hand_target = back_to_hand_target * 10f;
                }

                if (!back_to_hand)
                {
                    card.hp = 0;
                    card_ui.SetCard(card);
                }

                if (onKill != null)
                    onKill.Invoke();
            }
        }

        private void ShowStatusBar()
        {
            Card card = GetCard();
            if (card != null && status_text != null && !destroyed)
            {
                string stxt = GetStatusText();
                string ttxt = GetTraitText();

                if (stxt.Length > 0 && ttxt.Length > 0)
                    status_text.text = ttxt + ", " + stxt;
                else
                    status_text.text = ttxt + stxt;
            }

            bool show_status = status_text != null && status_text.text.Length > 0;
            status_alpha_target = show_status ? 1f : 0f;
        }

        public string GetStatusText()
        {
            Card card = GetCard();
            string txt = "";
            foreach (CardStatus astatus in card.GetAllStatus())
            {
                StatusData istats = StatusData.Get(astatus.type);
                if (istats != null && !string.IsNullOrEmpty(istats.title))
                {
                    int ival = Mathf.Max(astatus.value, Mathf.CeilToInt(astatus.duration / 2f));
                    string sval = ival > 1 ? " " + ival : "";
                    txt += istats.GetTitle() + sval + ", ";
                }
            }
            if (txt.Length > 2)
                txt = txt.Substring(0, txt.Length - 2);
            return txt;
        }

        public string GetTraitText()
        {
            Card card = GetCard();
            string txt = "";
            foreach (CardTrait atrait in card.GetAllTraits())
            {
                TraitData itrait = TraitData.Get(atrait.id);
                if (itrait != null && !string.IsNullOrEmpty(itrait.title))
                {
                    int ival = atrait.value;
                    string sval = ival > 1 ? " " + ival : "";
                    txt += itrait.GetTitle() + sval + ", ";
                }
            }
            if (txt.Length > 2)
                txt = txt.Substring(0, txt.Length - 2);
            return txt;
        }

        public bool IsDead()
        {
            return destroyed;
        }

        public bool IsFocus()
        {
            return focus;
        }

        public bool IsEquipFocus()
        {
            return equipment != null && equipment.IsFocus();
        }

        public void OnMouseEnter()
        {
            if (GameUI.IsUIOpened())
                return;

            if (GameTool.IsMobile())
                return;

            focus = true;
            ShowStatusBar();
        }

        public void OnMouseExit()
        {
            focus = false;
            status_alpha_target = 0f;
        }

        public void OnMouseDown()
        {
            if (GameUI.IsOverUILayer("UI"))
                return;

            PlayerControls.Get().SelectCard(this);

            if (GameTool.IsMobile())
            {
                focus = true;
                ShowStatusBar();
            }
        }

        public void OnMouseUp()
        {

        }

        public void OnMouseOver()
        {
            if (Input.GetMouseButtonDown(1))
            {
                PlayerControls.Get().SelectCardRight(this);
            }
        }

        public string GetCardUID()
        {
            return card_uid;
        }

        //Return main card (not equip)
        public Card GetCard()
        {
            Game data = GameClient.Get().GetGameData();
            Card card = data.GetCard(card_uid);
            return card;
        }

        //Return equip card
        public Card GetEquipCard()
        {
            if (equipment != null)
                return equipment.GetCard();
            return null;
        }

        //Return either main or equip card based on which one is focused
        public Card GetFocusCard()
        {
            if (IsEquipFocus())
                return GetEquipCard();
            return GetCard();
        }
        
        public CardData GetCardData()
        {
            Card card = GetCard();
            if (card != null)
                return CardData.Get(card.card_id);
            return null;
        }

        public Slot GetSlot()
        {
            return GetCard().slot;
        }

        public BoardCardFX GetCardFX()
        {
            return card_fx;
        }

        public CardData CardData { get { return GetCardData(); } }

        public static int GetNbCardsBoardPlayer(int player_id)
        {
            int nb = 0;
            foreach (BoardCard acard in card_list)
            {
                if (acard != null && acard.GetCard().player_id == player_id)
                    nb++;
            }
            return nb;
        }

        public static BoardCard GetNearestPlayer(Vector3 pos, int skip_player_id, BoardCard skip, float range = 2f)
        {
            BoardCard nearest = null;
            float min_dist = range;
            foreach (BoardCard card in card_list)
            {
                float dist = (card.transform.position - pos).magnitude;
                if (dist < min_dist && card != skip && skip_player_id != card.GetCard().player_id)
                {
                    min_dist = dist;
                    nearest = card;
                }
            }
            return nearest;
        }

        public static BoardCard GetNearest(Vector3 pos, BoardCard skip, float range = 2f)
        {
            BoardCard nearest = null;
            float min_dist = range;
            foreach (BoardCard card in card_list)
            {
                float dist = (card.transform.position - pos).magnitude;
                if (dist < min_dist && card != skip)
                {
                    min_dist = dist;
                    nearest = card;
                }
            }
            return nearest;
        }

        public static BoardCard GetFocus()
        {
            foreach (BoardCard card in card_list)
            {
                if (card.IsFocus())
                    return card;
            }
            return null;
        }

        public static void UnfocusAll()
        {
            foreach (BoardCard card in card_list)
            {
                card.focus = false;
                card.status_alpha_target = 0f;
            }
        }

        public static BoardCard Get(string uid)
        {
            foreach (BoardCard card in card_list)
            {
                if (card.card_uid == uid)
                    return card;
            }
            return null;
        }

        public static List<BoardCard> GetAll()
        {
            return card_list;
        }
    }
}