using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Arteranos.UI
{
    public class VirtualKeyboardSupport : UIBehaviour
    {
        public KeyboardUI SoftKeyboard = null;
        public bool FollowsCamera = true;

        private TMP_InputField[] TextFields;
        private KeyboardUI AttachedKB = null;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();

            UnityAction<string> makeAttachmentFunc(TMP_InputField field) 
                => (string x) => HookVirtualKB(field);

            TextFields = GetComponentsInChildren<TMP_InputField>();

            foreach(TMP_InputField text in TextFields)
            {
                text.onSelect.AddListener(makeAttachmentFunc(text));
            }

        }

        private Action<string, bool> MakeKbdCallback(TMP_InputField field) 
            => (string text, bool completed) => CommitEditing(field, text, completed);

        private void HookVirtualKB(TMP_InputField field)
        {
            if(AttachedKB == null)
            {
                AttachedKB = Instantiate(SoftKeyboard, 
                    transform.position + (transform.rotation * Vector3.forward * -0.01f), // Move a little bit to me to prevent z-fighting
                    transform.rotation);
                
                if (FollowsCamera)
                {
                    CameraUITracker ct = AttachedKB.gameObject.AddComponent<CameraUITracker>();
                    ct.m_offset = Vector3.forward;
                }
            }

            AttachedKB.Text = field.text;
            AttachedKB.StringPosition = field.text.Length;
            AttachedKB.OnFinishing += MakeKbdCallback(field);
            AttachedKB.gameObject.SetActive(true);
        }

        private void CommitEditing(TMP_InputField field, string text, bool completed)
        {
            AttachedKB.gameObject.SetActive(false);
            AttachedKB.OnFinishing -= MakeKbdCallback(field);
            if(completed) field.text = text;
        }
    }
}

