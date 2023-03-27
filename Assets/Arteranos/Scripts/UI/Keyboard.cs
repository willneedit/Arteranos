using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace Arteranos.UI
{
    internal struct Keycap
    {
        public string key;
        public string name;
        public float width;
    }

    internal struct Keymap
    {
        public string mode;
        public Keycap[][] map; // Row, column
    }

    public class Keyboard : UIBehaviour
    {
        public const string PATH_KEYBOARDLAYOUTS = "KeyboardLayouts/";

        public string layout = "de";
        public CanvasRenderer KeyboardBackplate = null;
        public Button KeyCap = null;
        public TMP_InputField PreviewField = null;

        public Vector2 TopLeft = new(1, -21);
        public Vector2 GridSize = new(20, 20);

        private int current_modeIndex = 0;
        private bool current_modeLock = false;
        private Keymap[] current_map;

        private readonly List<List<TextMeshProUGUI>> description = new();

        protected override void Awake()
        {
            base.Awake();

            TextAsset json = Resources.Load<TextAsset>(PATH_KEYBOARDLAYOUTS + layout);
            current_map = JsonConvert.DeserializeObject<Keymap[]>(json.text);

            SanityCheck();

            LayoutKeyboard();

            ShowModeChange(0);

            Debug.Log($"Loaded keyboard layout: {layout}");
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
                    rt.localPosition = new Vector2(x, y);
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
            //Debug.Log($"Key pressed: {row},{col}");

            string keyaction = current_map[current_modeIndex].map[row][col].key;

            if(string.IsNullOrEmpty(keyaction)) return;

            if(keyaction.Length > 3 && keyaction[..3] == "-->")
            {
                string mode = keyaction[3..];
                current_modeIndex = ShowModeChange(mode);
                current_modeLock = false;
                Debug.Log($"Mode change: {mode} ({current_modeIndex})");
                return;
            }
            else if(keyaction.Length > 3 && keyaction[..3] == "==>")
            {
                string mode = keyaction[3..];
                current_modeIndex = ShowModeChange(mode);
                current_modeLock = true;
                Debug.Log($"Mode lock: {mode} ({current_modeIndex})");
                return;
            }
            else if(keyaction.Length > 1 && keyaction[..1] == "*")
            {
                Debug.Log($"Action key: {keyaction}");
                return;
            }

            SynthesizeAndSendKeyDownEvent(PreviewField, (KeyCode) keyaction[0], keyaction[0]);

            Debug.Log($"Keypress: {keyaction}");
            if(!current_modeLock) current_modeIndex = ShowModeChange(0);
        }

        void SynthesizeAndSendKeyDownEvent(TMP_InputField panel, KeyCode code, 
            char character = '\0', EventModifiers modifiers = EventModifiers.None)
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

            panel.Select();
            panel.ProcessEvent(evt);
        }
    }
}
