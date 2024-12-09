/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using System;
using System.IO;
using UnityEngine.UI;

namespace Arteranos.UI
{
    public class FileBrowser : UIBehaviour
    {
        public Button btn_Cancel = null;

        public event Action<string> OnSelection;

        public string CurrentDirectory
        {
            get => _currentDirectory;
            set
            {
                string old = _currentDirectory;
                _currentDirectory = value;

                if (old != value && Chooser) Chooser.ShowPage(0);
            }
        }

        private ObjectChooser Chooser = null;

        private string _currentDirectory = null;

        protected override void Awake()
        {
            base.Awake();

            Chooser = GetComponent<ObjectChooser>();

            Chooser.OnShowingPage += PreparePage;
            Chooser.OnPopulateTile += PopulateTile;

            btn_Cancel.onClick.AddListener(GotCancel);
        }

        protected override void OnDestroy()
        {
            Chooser.OnPopulateTile -= PopulateTile;
            Chooser.OnShowingPage -= PreparePage;


            base.OnDestroy();
        }

        protected override void Start()
        {
            base.Start();

            Chooser.ShowPage(0);
        }

        struct FileListItem : IComparable<FileListItem>
        {
            public string FullPath;
            public string Name;
            public bool IsDirectory;

            public int CompareTo(FileListItem other) => Name.CompareTo(other.Name);
        }

        private readonly List<FileListItem> DirectoryItems = new();

        private void PreparePage(int _ /* page index */)
        {
            FileListItem SimpleGetDir(string d, bool full = true) => new() { IsDirectory = true, Name = full ? d : Path.GetFileName(d), FullPath = d };

            FileListItem GetWKD(Environment.SpecialFolder sf) => SimpleGetDir(Environment.GetFolderPath(sf), false);

            DirectoryItems.Clear();

            List<FileListItem> tmp = new();

            if (!string.IsNullOrEmpty(CurrentDirectory))
            {
                DirectoryItems.Add(new() { IsDirectory = true, Name = "..", FullPath = Path.GetDirectoryName(CurrentDirectory) });

                // First directories, then items
                foreach (string item in Directory.EnumerateDirectories(CurrentDirectory))
                    tmp.Add(new() { IsDirectory = true, Name = Path.GetFileName(item), FullPath = item });

                tmp.Sort();
                DirectoryItems.AddRange(tmp);

                tmp.Clear();
                foreach (string item in Directory.EnumerateFiles(CurrentDirectory))
                    tmp.Add(new() { IsDirectory = false, Name = Path.GetFileName(item), FullPath = item });

                tmp.Sort();
                DirectoryItems.AddRange(tmp);
            }
            else
            {
                // No "Downloads" well-known folder in C#. WHY?!
                DirectoryItems.Add(GetWKD(Environment.SpecialFolder.Desktop));
                DirectoryItems.Add(GetWKD(Environment.SpecialFolder.MyDocuments));
                DirectoryItems.Add(GetWKD(Environment.SpecialFolder.MyPictures));
                DirectoryItems.Add(GetWKD(Environment.SpecialFolder.MyVideos));
                DirectoryItems.Add(GetWKD(Environment.SpecialFolder.MyMusic));

                // Drives
                foreach (string str in Directory.GetLogicalDrives())
                    tmp.Add(SimpleGetDir(str));

                tmp.Sort();
                DirectoryItems.AddRange(tmp);
                tmp.Clear();

            }

            Chooser.UpdateItemCount(DirectoryItems.Count);
        }

        private void PopulateTile(int index, GameObject @object)
        {
            if (!@object.TryGetComponent(out FileBrowserItem fbi)) return;

            fbi.Name = DirectoryItems[index].Name;
            fbi.FullPath = DirectoryItems[index].FullPath;
            fbi.IsDirectory = DirectoryItems[index].IsDirectory;

            fbi.Container = this;
        }

        public void RequestUpdateList()
        {
            Chooser.ShowPage(Chooser.CurrentPage);
        }

        public void GotSelectItem(FileBrowserItem item)
        {
            if(item.IsDirectory)
                CurrentDirectory = item.FullPath; // Including the UI refresh
            else
                OnSelection?.Invoke(item.FullPath);
        }

        public void GotCancel() => OnSelection?.Invoke(null);
    }
}
