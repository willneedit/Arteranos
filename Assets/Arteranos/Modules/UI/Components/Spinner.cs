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

#pragma warning disable IDE1006 // Benennungsstile
        public int value
        {
            get => m_value;
            set { m_value = value; Selection.text = Options[m_value]; }
        }
#pragma warning restore IDE1006 // Benennungsstile

        public event Action<int, bool> OnChanged = null;

        private Image Background = null;
        private int m_value = 0;

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

            m_value += up ? 1 : -1 + Options.Length;
            m_value %= Options.Length;

            Selection.text = Options[m_value];

            OnChanged?.Invoke(m_value, up);
        }
    }
}
