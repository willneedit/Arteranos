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

        public bool initial = true;

        private KeyboardUI AttachedKB = null;

        private void Update()
        {
            if(initial)
            {
                FindHookableTextFields(gameObject);
                initial = false;
            }
        }

        private void FindHookableTextFields(GameObject go)
        {
            UnityAction<string> makeAttachmentFunc(TMP_InputField field)
                => (string x) => HookVirtualKB(field);

            TMP_InputField text = go.GetComponent<TMP_InputField>();
            if(text != null)
            {
                text.onSelect.AddListener(makeAttachmentFunc(text));
            }
            else
            {
                for(int i = 0, c = go.transform.childCount; i < c; ++i)
                    FindHookableTextFields(go.transform.GetChild(i).gameObject);
            }
        }

        private Action<string, bool> MakeKbdCallback(TMP_InputField field) 
            => (string text, bool completed) => CommitEditing(field, text, completed);

        private void HookVirtualKB(TMP_InputField field)
        {
            if(AttachedKB == null)
            {
                AttachedKB = Instantiate(SoftKeyboard, 
                    transform.position + (transform.rotation * Vector3.forward * 0.99f), // Move a little bit to me to prevent z-fighting
                    transform.rotation);
                
                if (FollowsCamera)
                {
                    CameraUITracker ct = AttachedKB.gameObject.AddComponent<CameraUITracker>();
                    ct.m_offset = Vector3.forward * 0.99f;
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

