using System.Collections;
using System.Collections.Generic;
using TcgEngine.Client;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// History bar shows all the previous moved perform by a player this turn
    /// </summary>

    public class StackUI : MonoBehaviour
    {
        public List<TurnHistoryLine> history_lines;
        public GameObject template;
        public Text header;

        void Start()
        {
            template.SetActive(false);
            header.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!GameClient.Get().IsReady())
                return;

            Game data = GameClient.Get().GetGameData();

            if (data.history_list != null)
            {
                header.gameObject.SetActive(data.history_list.Count > 0);

                int index = 0;
                foreach (ActionHistory order in data.history_list)
                {
                    if (index < history_lines.Count)
                    {
                        history_lines[index].SetLine(order);
                        history_lines[index].gameObject.SetActive(true);
                        index++;
                    }
                    else
                    {
                        var line = Instantiate(template, transform).GetComponent<TurnHistoryLine>();
                        line.gameObject.SetActive(true);
                        line.SetLine(order);
                        history_lines.Add(line);
                    }
                }

                while (index < history_lines.Count)
                {
                    history_lines[index].gameObject.SetActive(false);
                    index++;
                }
            }
        }
    }
}