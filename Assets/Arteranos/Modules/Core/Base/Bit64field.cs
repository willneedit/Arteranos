/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

namespace Arteranos.Core
{
    /// <summary>
    /// Cut-down instance of BitArray, limited to 64 bits for optimization
    /// </summary>
    public class Bit64field
    {
        public static void Set(ref ulong field, ulong bits, bool desired)
        {
            if(desired)
            {
                field |= bits;
            }
            else
            {
                field &= ~bits;
            }
        }

        public static bool IsAll(ulong field, ulong bits) => (field & bits) == bits;

        public static bool IsAny(ulong field, ulong bits) => (field & bits) != 0;
    }
}
