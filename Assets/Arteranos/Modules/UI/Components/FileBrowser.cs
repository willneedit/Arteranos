/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;
using UnityEngine.UI;
using TMPro;
using Arteranos.Core;
using System.Text.RegularExpressions;

namespace Arteranos.UI
{
    public class FileBrowser : Core.ActionPage
    {
        public Button btn_Cancel = null;
        public TMP_Text lbl_Path = null;

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

        public string Pattern 
        {
            get => _pattern;
            set
            {
                string old = _pattern;
                _pattern = value != null ? $"^{value}$" : null; // Match whoie string

                if (old != value && Chooser) Chooser.ShowPage(0);
            }
        }

        private ObjectChooser Chooser = null;

        private string _currentDirectory = null;
        private string _pattern = null;

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

        public override void Called(object data)
        {
            base.Called(data);

            if (data == null) return;

            if (data is not FileBrowserData fbd)
                throw new ArgumentException("FileBrowserData expected");

            CurrentDirectory = fbd.Directory;
            Pattern = fbd.Pattern;
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

            lbl_Path.text = CurrentDirectory ?? string.Empty;

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
                {
                    string fileName = Path.GetFileName(item);
                    if (Pattern == null || Regex.IsMatch(fileName, Pattern, RegexOptions.IgnoreCase))
                        tmp.Add(new() { IsDirectory = false, Name = fileName, FullPath = item });
                }

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
                BackOut(item.FullPath);
        }

        public void GotCancel() => BackOut(null);
    }
}
