using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using UnityEngine.UI;
using TMPro;

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

        public Vector2 TopLeft = new(1, -1);
        public Vector2 GridSize = new(10, 10);

        private string mode = "default";
        private Keymap[] current_map;


        protected override void Awake()
        {
            base.Awake();

            TextAsset json = Resources.Load<TextAsset>(PATH_KEYBOARDLAYOUTS + layout);
            current_map = JsonConvert.DeserializeObject<Keymap[]>(json.text);

            LayoutKeyboard();

            Debug.Log($"Loaded keyboard layout: {layout}");
        }

        public void LayoutKeyboard()
        {
            Keymap mmap = current_map[0];

            float y = TopLeft.y;
            for(int row = 0, rows = mmap.map.Length; row < rows; row++)
            {
                Keycap[] keyrow = mmap.map[row];
                float x = TopLeft.x;
                for(int col = 0, cols = keyrow.Length; col < cols; col++)
                {
                    string name = keyrow[col].name ?? keyrow[col].key;
                    float keywidth = ((keyrow[col].width == 0) ? 1.0f : keyrow[col].width);

                    Button btn = Instantiate(KeyCap, KeyboardBackplate.transform);
                    btn.name = $"Key_{name}";
                    RectTransform rt = btn.GetComponent<RectTransform>();
                    rt.localPosition = new Vector2(x, y);
                    rt.sizeDelta = new(rt.sizeDelta.x * keywidth, rt.sizeDelta.y);
                    btn.GetComponentInChildren<TextMeshProUGUI>().text = name;

                    x += keywidth * GridSize.x;
                }

                y -= GridSize.y;
            }
        }
    }
}
