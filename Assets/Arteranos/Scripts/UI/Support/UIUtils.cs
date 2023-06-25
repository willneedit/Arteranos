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
    public class UIUtils
    {
        public static void FillSpinnerValues<T>(Spinner target, int defValue) where T : Enum
        {
            IEnumerable<string> q = from T entry in Enum.GetValues(typeof(T))
                                    select Core.Utils.GetEnumDescription(entry);

            target.Options = q.ToArray();
            target.value = (target.Options.Length > defValue) ? defValue : 0;
        }

    }
}
