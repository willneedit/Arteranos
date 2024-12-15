using Arteranos.Core;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Arteranos.UI
{
    public class APChoiceBook : ActionPage
    {
        [Serializable]
        public struct ChoiceBookEntry
        {
            public string name;
            public GameObject UI;
        }

        public ChoiceBookEntry[] ChoiceEntries = null;
        public int CurrentChoice = 0;


        public Transform ButtonList { get; private set; } = null;
        public Transform PaneList { get; private set; } = null;


        protected override void Awake()
        {
            UnityAction makeButtonPressedAction(int index) => delegate { OnButtonClicked(index); };

            base.Start();

            Debug.Assert(ChoiceEntries != null && ChoiceEntries.Length > 0,
                "Choicebook has at least one page");

            ButtonList = transform.GetChild(0);
            PaneList = transform.GetChild(1);

            Button SampleButton = ButtonList.GetChild(0).gameObject.GetComponent<Button>();

            for (int i = 0; i < ChoiceEntries.Length; i++)
            {
                Button btn = i == 0 
                    ? SampleButton
                    : Instantiate(SampleButton, ButtonList);
                btn.onClick.AddListener(makeButtonPressedAction(i));
                btn.GetComponentInChildren<TMP_Text>().text = ChoiceEntries[i].name;
                ChoiceEntries[i].UI.SetActive(false);
                Instantiate(ChoiceEntries[i].UI, PaneList);
            }

            OnEnterLeaveAction(true);
        }

        private void OnButtonClicked(int newChoice) 
        {
            if (CurrentChoice == newChoice) return;

            OnEnterLeaveAction(false);
            CurrentChoice = newChoice;
            OnEnterLeaveAction(true);
        }

        // ---------------------------------------------------------------
        public override void OnEnterLeaveAction(bool onEnter) 
            => PaneList.GetChild(CurrentChoice).gameObject.SetActive(onEnter);
    }
}