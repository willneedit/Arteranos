using Arteranos.Core;
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

        protected override void OnDisable() => XR.XRControl.Instance?.FreezeControls(false);

        private void FindHookableTextFields(GameObject go)
        {
            UnityAction<string> makeAttachmentFunc(TMP_InputField field)
                => (string x) =>
                {
                    HookVirtualKB(field);
                    XR.XRControl.Instance.FreezeControls(true);
                };

            UnityAction<string> makeDetachmentFunc(TMP_InputField field)
                => (string x) =>
                {
                    HookVirtualKB(field);
                    XR.XRControl.Instance.FreezeControls(false);
                };

            if(go.TryGetComponent(out TMP_InputField text))
            {
                text.onSelect.AddListener(makeAttachmentFunc(text));
                text.onDeselect.AddListener(makeDetachmentFunc(text));
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
            ClientSettings cs = SettingsManager.Client;

            if(cs.Controls.VK_Usage == VKUsage.Never) return;

            if(cs.Controls.VK_Usage == VKUsage.VROnly && !cs.VRMode) return;

            AttachedKB = FindObjectOfType<KeyboardUI>(true);

            Vector3 forward = Vector3.forward * (SettingsManager.Client.VRMode ? 0.25f : 0.99f);
            Vector3 scale = (SettingsManager.Client.VRMode ? 0.50f : 1.00f) * 0.005f * Vector3.one;

            CameraUITracker ct;
            if (AttachedKB == null)
            {
                AttachedKB = Instantiate(SoftKeyboard, 
                    transform.position + (transform.rotation * forward), // Move a little bit to me to prevent z-fighting
                    transform.rotation);

                ct = AttachedKB.gameObject.AddComponent<CameraUITracker>();
            }
            else
                ct = AttachedKB.gameObject.GetComponent<CameraUITracker>();

            ct.m_offset = forward;
            ct.enabled = FollowsCamera;

            AttachedKB.gameObject.SetActive(false);
            AttachedKB.transform.localScale = scale;
            AttachedKB.Text = field.text;
            AttachedKB.StringPosition = field.text.Length;
            AttachedKB.characterLimit = field.characterLimit;
            AttachedKB.OnFinishing += MakeKbdCallback(field);
            AttachedKB.gameObject.SetActive(true);
        }

        private void CommitEditing(TMP_InputField field, string text, bool completed)
        {
            AttachedKB.OnFinishing -= MakeKbdCallback(field);
            Destroy(AttachedKB.gameObject);
            if(completed) field.text = text;
        }
    }
}

