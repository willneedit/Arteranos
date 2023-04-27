using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arteranos.UI
{
    public class DialogUI : UIBehaviour
    {
        public string text = "Dialog text";
        public string[] buttons = null;

        public TMP_Text Caption = null;
        public GameObject ButtonPane = null;

        public event Action<int> OnDialogDone;

        private readonly SemaphoreSlim dialogFinished = new(0, 1);

        public static DialogUI New()
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_Dialog"));
            return go.GetComponent<DialogUI>();
        }

        protected override void Start()
        {
            UnityAction makeButtonPressedAction(int index) => () => OnButtonClicked(index);
            GameObject sampleButton = ButtonPane.transform.GetChild(0).gameObject;
            GameObject sampleSpacer = ButtonPane.transform.GetChild(1).gameObject;

            base.Start();

            Caption.text = text;

            // No buttons, only to close by outside means.
            if(buttons == null)
            {
                Destroy(sampleButton);
                Destroy(sampleSpacer);
                return;
            }

            if(buttons.Length != 0)
            {
                sampleButton.GetComponent<Button>().onClick.AddListener(makeButtonPressedAction(0));
                sampleButton.GetComponentInChildren<TMP_Text>().text = buttons[0];
            }
            else
                Destroy(sampleButton);


            if(buttons.Length < 2)
                Destroy(sampleSpacer);

            for(int i = 1; i < buttons.Length; i++)
            {
                if(i > 1)
                    Instantiate(sampleSpacer, ButtonPane.transform);

                Button btn = Instantiate(sampleButton, ButtonPane.transform).GetComponent<Button>();
                btn.onClick.AddListener(makeButtonPressedAction(i));
                btn.GetComponentInChildren<TMP_Text>().text = buttons[i];
            }



        }

        private void OnButtonClicked(int index)
        {
            OnDialogDone?.Invoke(index);
            dialogFinished.Release();

            Destroy(gameObject);
        }

        // Purely convenient for write a process driven control flow rather than
        // a event driven one and reducing the boilerplate.
        public async Task<int> PerformDialogAsync(string text, string[] buttons)
        {
            this.text = text;
            this.buttons = buttons;

            int rc = -1;

            OnDialogDone += (index) => rc = index;
            await dialogFinished.WaitAsync();
            OnDialogDone -= (index) => rc = index;

            return rc;
        }
    }
}
