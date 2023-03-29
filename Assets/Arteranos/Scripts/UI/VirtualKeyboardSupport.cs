using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Arteranos.UI
{
    public class VirtualKeyboardSupport : MonoBehaviour
    {
        public Keyboard SoftKeyboard = null;
        public TMP_InputField[] TextFields;

        private Keyboard AttachedKB = null;

        // Start is called before the first frame update
        void Start()
        {
            UnityAction<string> makeAttachmentFunc(TMP_InputField field) => (string x) => HookVirtualKB(field);

            TextFields = GetComponentsInChildren<TMP_InputField>();

            foreach(TMP_InputField text in TextFields)
            {
                text.onSelect.AddListener(makeAttachmentFunc(text));
            }

        }

        private void HookVirtualKB(TMP_InputField field)
        {
            if(AttachedKB == null)
            {
                AttachedKB = Instantiate(SoftKeyboard, 
                    transform.position + Vector3.forward * -0.01f, // Move a little bit to me to prevent z-fighting
                    transform.rotation);
                AttachedKB.Text = field.text;
                AttachedKB.StringPosition = field.text.Length;
                AttachedKB.OnFinishing += (string text, bool completed) => CommitEditing(field, text, completed);
            }
        }

        private void CommitEditing(TMP_InputField field, string text, bool completed)
        {
            if(completed) field.text = text;
            Destroy(AttachedKB.gameObject);
            AttachedKB = null;
        }
    }
}

