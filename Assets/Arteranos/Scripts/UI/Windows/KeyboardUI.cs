using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System;
using Arteranos.Core;

namespace Arteranos.UI
{
    internal struct Keycap
    {
        public string key;
        public string name;
        public float width;

        public string modeswitch;
        public string modelock;
        public string action;
    }

    internal struct Keymap
    {
        public string mode;
        public Keycap[][] map; // Row, column
    }

    public class KeyboardUI : UIBehaviour
    {
        public const string PATH_KEYBOARDLAYOUTS = "KeyboardLayouts/";

        public string layout = "de";
        public CanvasRenderer KeyboardBackplate = null;
        public Button KeyCap = null;
        public TMP_InputField PreviewField = null;
        public TextMeshProUGUI lbl_RemainingChars = null;
        public Button btn_CloseButton = null;

        public event Action<string, bool> OnFinishing;

        public string Text
        {
            get => PreviewField.text;
            set => PreviewField.text = value;
        }

        public int StringPosition
        {
            get => PreviewField.stringPosition;
            set => PreviewField.stringPosition = value;
        }

        public int CharacterLimit
        {
            get => PreviewField.characterLimit;
            set {
                PreviewField.characterLimit = value;
                lbl_RemainingChars.gameObject.SetActive(value != 0);
            }
        }

        public Vector2 TopLeft = new(1, -41);
        public Vector2 GridSize = new(20, 20);

        private int current_modeIndex = 0;
        private bool current_modeLock = false;
        private Keymap[] current_map;

        private readonly List<List<TextMeshProUGUI>> description = new();

        protected override void Awake()
        {
            base.Awake();

            layout = GetKeyboardLayout();

            TextAsset json = Resources.Load<TextAsset>(PATH_KEYBOARDLAYOUTS + layout);
            current_map = JsonConvert.DeserializeObject<Keymap[]>(json.text);

            SanityCheck();

            LayoutKeyboard();

            PreviewField.onValueChanged.AddListener(OnValueChanged);

            btn_CloseButton.onClick.AddListener(OnCloseButtonClicked);

            Debug.Log($"Loaded keyboard layout: {layout}");
        }

        private void OnValueChanged(string text) => lbl_RemainingChars.text = (PreviewField.characterLimit - text.Length).ToString();

        protected override void OnEnable()
        {
            base.OnEnable();

            // Update the new current character limit, if any
            lbl_RemainingChars.gameObject.SetActive(PreviewField.characterLimit != 0);
            lbl_RemainingChars.text = (PreviewField.characterLimit - PreviewField.text.Length).ToString();

            // Reset the leftover mode from the previous use.
            ShowModeChange(0);
            current_modeIndex= 0;
            current_modeLock= false;
        }

        public string GetKeyboardLayout()
        {
            ClientSettings cs = SettingsManager.Client;

            switch(cs.Controls.VK_Layout)
            {
                case VKLayout.de_DE_full:
                    return "de";
                case VKLayout.en_US_full:
                    return "en";
                default:
                    Debug.LogError($"Unknown layout enum {cs.Controls.VK_Layout}, reverting to en");
                    return "en";
            }
        }

        public void SanityCheck()
        {
            Keymap def = current_map[0];

            foreach(Keymap map in current_map)
            {
                Debug.Assert(map.map.Length == def.map.Length,
                    $"Row count mismatch: {map.mode} has {map.map.Length}, not {def.map.Length}");

                for(int row = 0, rows = map.map.Length; row < rows; row++)
                {
                    Keycap[] keyrow = map.map[row];
                    Keycap[] defrow = def.map[row];

                    Debug.Assert(keyrow.Length == defrow.Length,
                        $"Cols count mismatch: {map.mode}, row {row} has keyrow {keyrow.Length}, not {defrow.Length}");
                }
            }
        }

        public void LayoutKeyboard()
        {
            UnityAction makeKeyPressedFunc(int row, int col) => () => OnKeyPressed(row, col);

            Keymap mmap = current_map[0];

            float y = TopLeft.y;
            for(int row = 0, rows = mmap.map.Length; row < rows; row++)
            {
                Keycap[] keyrow = mmap.map[row];
                List<TextMeshProUGUI> descrow = new();
                description.Add(descrow);

                float x = TopLeft.x;
                for(int col = 0, cols = keyrow.Length; col < cols; col++)
                {
                    string name = keyrow[col].name ?? keyrow[col].key;
                    float keywidth = ((keyrow[col].width == 0) ? 1.0f : keyrow[col].width);

                    Button btn = Instantiate(KeyCap, KeyboardBackplate.transform);
                    TextMeshProUGUI desccap = btn.GetComponentInChildren<TextMeshProUGUI>();
                    descrow.Add(desccap);

                    btn.onClick.AddListener(makeKeyPressedFunc(row, col));

                    RectTransform rt = btn.GetComponent<RectTransform>();
                    rt.localPosition = new Vector3(x, y, -2);
                    rt.sizeDelta = new(rt.sizeDelta.x * keywidth, rt.sizeDelta.y);

                    btn.name = $"Key_{name}";

                    x += keywidth * GridSize.x;
                }

                y -= GridSize.y;
            }
        }

        public int ShowModeChange(int modeIndex)
        {
            Keymap mmap = current_map[modeIndex];

            for(int row = 0, rows = mmap.map.Length; row < rows; row++)
            {
                Keycap[] keyrow = mmap.map[row];
                for(int col = 0, cols = keyrow.Length; col < cols; col++)
                {
                    string name = keyrow[col].name ?? keyrow[col].key;

                    description[row][col].text = name;

                }
            }

            return modeIndex;
        }

        public int ShowModeChange(string mode)
        {
            for(int i = 0, l = current_map.Length; i < l; i++)
                if(current_map[i].mode == mode) return ShowModeChange(i);

            Debug.LogError($"Unknown mode: {mode}");
            return 0;
        }

        private void OnKeyPressed(int row, int col)
        {
            Keycap keycap = current_map[current_modeIndex].map[row][col];
            string keyaction = keycap.key;

            if(keycap.modeswitch != null)
            {
                current_modeIndex = ShowModeChange(keycap.modeswitch);
                current_modeLock = false;
                return;
            }
            else if(keycap.modelock != null)
            {
                current_modeIndex = ShowModeChange(keycap.modelock);
                current_modeLock = true;
                return;
            }
            else if(keycap.action != null)
            {
                string mode = current_map[current_modeIndex].mode;
                string action = keycap.action;

                EventModifiers modifiers = EventModifiers.None;
                KeyCode code = 0;

                switch(mode)
                {
                    case "shift": modifiers = EventModifiers.Shift; break;
                    case "alt": modifiers = EventModifiers.Alt; break;
                    case "ctrl": modifiers = EventModifiers.Control; break;
                    default:
                        break;
                }

                switch(action)
                {
                    case "left": code = KeyCode.LeftArrow; break;
                    case "right": code = KeyCode.RightArrow; break;

                    case "selectall": code = KeyCode.A; modifiers = EventModifiers.Control; break;
                    case "cut": code = KeyCode.X; modifiers = EventModifiers.Control; break;
                    case "copy": code = KeyCode.C; modifiers = EventModifiers.Control; break;
                    case "paste": code = KeyCode.V; modifiers = EventModifiers.Control; break;

                    default:
                        break;
                }

                // Typical for that worm ridden fruit. Having to add more lines to satisfy these...
                if(modifiers == EventModifiers.Control)
                {
                    modifiers = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX 
                        ? EventModifiers.Command
                        : EventModifiers.Control;
                }

                SynthesizeAndSendKeyDownEvent(code, '\0', modifiers);

                return;
            }

            if(string.IsNullOrEmpty(keyaction)) return;

            // Return, submit.
            if(keyaction[0] == '\u000d')
            {
                OnFinishing?.Invoke(Text, true);
                return;
            }

            SynthesizeAndSendKeyDownEvent((KeyCode) keyaction[0], keyaction[0]);

            if(!current_modeLock) current_modeIndex = ShowModeChange(0);
        }

        void SynthesizeAndSendKeyDownEvent(KeyCode code, char character = '\0',
            EventModifiers modifiers = EventModifiers.None)
        {
            // Create a UnityEngine.Event to hold initialization data.
            // Also, this event will be forwarded to IMGUIContainer.m_OnGUIHandler
            Event evt = new()
            {
                type = EventType.KeyDown,
                keyCode = code,
                character = character,
                modifiers = modifiers
            };

            PreviewField.Select();
            PreviewField.ProcessEvent(evt);
        }

        private void OnCloseButtonClicked()
        {
            OnFinishing?.Invoke(Text, false);
            return;
        }
    }
}
