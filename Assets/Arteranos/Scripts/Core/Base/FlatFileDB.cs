/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace Arteranos.Core
{
    public class FlatFileDB<T> where T : class
    {
        protected static string _KnownPeersRoot = null;
        protected static Func<string, string> _GetFileName = null;
        protected static string _SearchPattern = null;

        protected static Func<Stream, T> _Deserialize = null;
        protected Action<Stream> _Serialize = null;


        public bool _DBUpdate(string key, Func<T, bool> updateCondition)
        {
            T old = _DBLookup(key);

            if (old != null && !updateCondition(old)) return false;

            _DBInsert(key);

            return true;
        }


        public void _DBInsert(string key)
        {
            string fn = _GetFileName(key);
            string dir = Path.GetDirectoryName(fn);

            if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }

            using Stream stream = File.Create(fn);
            _Serialize(stream);
        }

        public T _DBLookup(string key)
        {
            string fn = _GetFileName(key);

            if (!File.Exists(fn)) return null;

            using Stream stream = File.OpenRead(fn);
            return _Deserialize(stream);
        }

        public void _DBDelete(string key)
        {
            string fn = _GetFileName(key);

            if (!File.Exists(fn)) return;

            File.Delete(fn);
        }

        public IEnumerable<T> _DBList()
        {
            IEnumerable<string> files = Directory.EnumerateFiles(_KnownPeersRoot, _SearchPattern, SearchOption.AllDirectories);

            foreach (string file in files)
            {
                T sd = null;
                using Stream stream = File.OpenRead(file);
                sd = _Deserialize(stream);

                if (sd != null) yield return sd;
            }
        }



    }
}
