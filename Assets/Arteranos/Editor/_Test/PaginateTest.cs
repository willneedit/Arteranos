/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using Arteranos.Core;
using System.Text;

namespace Arteranos
{
    public class PaginateTest : MonoBehaviour
    {
        private readonly int[] ints = new int[]
        {
            10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180
        };

        private void DumpInts(int[] ints)
        {
            if(ints == null)
            {
                Debug.Log("(null)");
                return;
            }

            StringBuilder sb = new();
            bool first = true;
            foreach (int i in ints)
            {
                if(!first) sb.Append(", ");
                first = false;
                sb.Append(i);
            }

            Debug.Log(sb.ToString());
        }

        private void Start()
        {
            Utils.Paginated<int> page;

            page = Utils.Paginate(ints, 0, 3);
            Debug.Log($"maxPage={page.maxPage} page={page.page}");
            DumpInts(page.payload);

            page = Utils.Paginate(ints, 1, 3);
            Debug.Log($"maxPage={page.maxPage} page={page.page}");
            DumpInts(page.payload);

            page = Utils.Paginate(ints, 2, 3);
            Debug.Log($"maxPage={page.maxPage} page={page.page}");
            DumpInts(page.payload);

            page = Utils.Paginate(ints, 6, 3);
            Debug.Log($"maxPage={page.maxPage} page={page.page}");
            DumpInts(page.payload);

            page = Utils.Paginate(ints, 7, 3);
            Debug.Log($"maxPage={page.maxPage} page={page.page}");
            DumpInts(page.payload);
        }
    }
}
