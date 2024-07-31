using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arteranos.UI
{
    public class ChoiceBook : UIBehaviour
    {
        [Serializable]
        public struct ChoiceBookEntry
        {
            public string name;
            public UIBehaviour UI;
        }

        public ChoiceBookEntry[] ChoiceEntries = null;
        public int CurrentChoice = 0;

        public event Action<int, int> OnChoicePageChanged;

        public Transform ButtonList { get; private set; } = null;
        public Transform PaneList { get; private set; } = null;

        protected override void Awake()
        {
            UnityAction makeButtonPressedAction(int index) => () => OnButtonClicked(index);

            base.Start();

            Debug.Assert(ChoiceEntries != null && ChoiceEntries.Length > 0,
                "Choicebook has at least one page");

            ButtonList = transform.GetChild(0);
            PaneList = transform.GetChild(1);

            Button SampleButton = ButtonList.GetChild(0).gameObject.GetComponent<Button>();

            SampleButton.GetComponent<Button>().onClick.AddListener(makeButtonPressedAction(0));
            SampleButton.GetComponentInChildren<TMP_Text>().text = ChoiceEntries[0].name;

            for (int i = 1; i < ChoiceEntries.Length; i++)
            {
                Button btn = Instantiate(SampleButton, ButtonList);
                btn.onClick.AddListener(makeButtonPressedAction(i));
                btn.GetComponentInChildren<TMP_Text>().text = ChoiceEntries[i].name;
            }

            for (int j = 0; j < ChoiceEntries.Length; j++)
            {
                ChoiceEntries[j].UI.gameObject.SetActive(false);
                GameObject go = Instantiate(ChoiceEntries[j].UI.gameObject, PaneList);
            }

            PaneList.GetChild(CurrentChoice).gameObject.SetActive(true);
        }

        private void OnButtonClicked(int newChoice)
        {
            if (CurrentChoice == newChoice) return;

            PaneList.GetChild(CurrentChoice).gameObject.SetActive(false);
            PaneList.GetChild(newChoice).gameObject.SetActive(true);

            OnChoicePageChanged?.Invoke(CurrentChoice, newChoice);
            CurrentChoice = newChoice;
        }

        public void SetPageActive(int index, bool active)
        {
            PaneList.GetChild(index).gameObject.SetActive(active);
            ButtonList.GetChild(index).gameObject.SetActive(active);

            // Pulling the rug from under your feet?
            if (!active && CurrentChoice == index && CurrentChoice > 0) OnButtonClicked(CurrentChoice - 1);
        }
    }
}
