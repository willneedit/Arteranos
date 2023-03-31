using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arteranos.UI
{
    public class Spinner : UIBehaviour
    {
        public Button ArrowDown = null;
        public Button ArrowUp = null;
        public TextMeshProUGUI Selection = null;

        public ColorBlock colors = ColorBlock.defaultColorBlock;

        public string[] Options = null;
        public int value = 0;

        public event Action<int, bool> OnChanged = null;

        private Image Background = null;

        protected override void Awake()
        {
            base.Awake();

            Background = Selection.GetComponentInParent<Image>();
        }

        protected override void Start()
        {
            base.Start();

            ArrowUp.colors = colors;
            ArrowDown.colors = colors;

            ArrowDown.onClick.AddListener(() => OnMakeChange(false));
            ArrowUp.onClick.AddListener(() => OnMakeChange(true));

            if(Options?.Length == 0) return;

            Selection.text = Options[value];
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            ArrowDown.interactable = true;
            ArrowUp.interactable = true;

            Background.color = colors.normalColor;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            ArrowDown.interactable = false;
            ArrowUp.interactable = false;

            Background.color = colors.disabledColor;
        }

        private void OnMakeChange(bool up)
        {
            if(Options?.Length == 0) return;

            value += up ? 1 : -1 + Options.Length;
            value %= Options.Length;

            Selection.text = Options[value];

            OnChanged?.Invoke(value, up);
        }
    }
}
