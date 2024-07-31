/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Arteranos.UI
{
    public static class UIUtils
    {
        //public static void FillSpinnerValues<T>(Spinner target, int defValue) where T : Enum
        //{
        //    IEnumerable<string> q = from T entry in Enum.GetValues(typeof(T))
        //                            select Core.Utils.GetEnumDescription(entry);

        //    target.Options = q.ToArray();
        //    target.value = (target.Options.Length > defValue) ? defValue : 0;
        //}

        public static void CreateEnumValues<T>(out Dictionary<string, T> mapping) where T : Enum
        {
            mapping = new Dictionary<string, T>();
            foreach(T entry in Enum.GetValues(typeof(T)))
            {
                string v = Core.Utils.GetEnumDescription(entry);
                if(v != null) mapping[v] = entry;
            }
        }

        public static T GetEnumValue<T>(this Spinner spinner, Dictionary<string, T> mapping) where T : Enum 
            => mapping[spinner.Options[spinner.value]];

        public static void SetEnumValue<T>(this Spinner spinner, T v) where T : Enum
            => spinner.value = Array.IndexOf(spinner.Options, Core.Utils.GetEnumDescription(v));

        public static void FillSpinnerEnum<T>(this Spinner spinner, out Dictionary<string, T> mapping, T v) where T : Enum
        {
            CreateEnumValues(out mapping);
            spinner.Options = mapping.Keys.ToArray();
            spinner.SetEnumValue(v);
        }
    }
}
