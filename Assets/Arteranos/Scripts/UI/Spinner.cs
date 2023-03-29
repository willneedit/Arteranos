using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Spinner : UIBehaviour
{
    public Button ArrowDown = null;
    public Button ArrowUp = null;
    public TextMeshProUGUI Selection = null;

    public string[] Options = null;

    public int CurrentlySelected { get; set; } = 0;

    public event Action<int, bool> OnChanged = null;

    protected override void Start()
    {
        base.Start();

        ArrowDown.onClick.AddListener(() => OnMakeChange(false));
        ArrowUp.onClick.AddListener(() => OnMakeChange(true));

        if(Options?.Length == 0) return;

        Selection.text = Options[CurrentlySelected];
    }

    private void OnMakeChange(bool up)
    {
        if(Options?.Length == 0) return;

        CurrentlySelected += up ? 1 : -1;
        if(CurrentlySelected < 0) CurrentlySelected += Options.Length;
        CurrentlySelected %= Options.Length;

        Selection.text = Options[CurrentlySelected];

        OnChanged?.Invoke(CurrentlySelected, up);
    }
}
