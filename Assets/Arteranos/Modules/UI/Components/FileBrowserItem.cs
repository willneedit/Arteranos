/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using TMPro;
using UnityEngine.EventSystems;

namespace Arteranos.UI
{
    public class FileBrowserItem : UIBehaviour
    {
        public TMP_InputField txt_Name;

        public string Name {  get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public FileBrowser Container { get; set; }


        protected override void Awake()
        {
            base.Awake();

            txt_Name.onSelect.AddListener(GotSelection);
        }

        protected override void Start()
        {
            base.Start();

            Populate();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void Populate()
        {
            txt_Name.text = IsDirectory
                ? $"{Name}/"
                : Name;
        }

        private void GotSelection(string arg0) => Container.GotSelectItem(this);
    }
}
