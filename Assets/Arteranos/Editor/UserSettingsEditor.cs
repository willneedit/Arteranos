#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

using Arteranos.Core;

namespace Arteranos.Editing
{
    [CustomEditor(typeof(ClientSettingsJSON))]
    public class UserSettingsEditor : Editor
    {
        SerializedProperty MicDeviceName;
        void OnEnable() => MicDeviceName = serializedObject.FindProperty("MicDeviceName");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // ClientSettings that = (ClientSettings)target;

            string deviceName = MicDeviceName.stringValue;

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Microphone Device");
            if(EditorGUILayout.DropdownButton(new GUIContent(deviceName), FocusType.Keyboard))
            {
                GenericMenu menu = new();

                foreach(string device in Microphone.devices)
                {
                    menu.AddItem(new GUIContent(device), deviceName == device,
                    OnDeviceSelected, device);
                }

                menu.ShowAsContext();
            }

            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        // Handler for when a menu item is selected
        private void OnDeviceSelected(object device_)
        {
            string device = (string) device_;

            MicDeviceName.stringValue = device;
            serializedObject.ApplyModifiedProperties();

        }
    }
}

#endif
