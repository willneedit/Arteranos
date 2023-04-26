using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    private Transform ButtonList = null;
    private Transform PaneList = null;

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

        for(int i = 1; i < ChoiceEntries.Length; i++)
        {
            Button btn = Instantiate(SampleButton, ButtonList);
            btn.onClick.AddListener(makeButtonPressedAction(i));
            btn.GetComponentInChildren<TMP_Text>().text = ChoiceEntries[i].name;
        }

        for(int j = 0; j < ChoiceEntries.Length; j++)
        {
            GameObject go = Instantiate(ChoiceEntries[j].UI.gameObject, PaneList);
            go.SetActive(false);
        }

        PaneList.GetChild(CurrentChoice).gameObject.SetActive(true);
    }

    private void OnButtonClicked(int newChoice)
    {
        if(CurrentChoice == newChoice) return;

        PaneList.GetChild(CurrentChoice).gameObject.SetActive(false);
        PaneList.GetChild(newChoice).gameObject.SetActive(true);

        OnChoicePageChanged?.Invoke(CurrentChoice, newChoice);
        CurrentChoice = newChoice;
    }
}
