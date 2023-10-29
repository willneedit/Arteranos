/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arteranos.User
{
    public class TestScript : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            foreach(Component ob in GetComponents<Component>())
            {
                System.Type type = ob.GetType();
                if(type != null)
                {
                    Assembly asm = type.Assembly;
                    Debug.Log($"{type.FullName}, {asm.GetName().Name}");

                    foreach(Module mod in asm.GetModules())
                        Debug.Log($"   {mod.Name}");
                }
            }

            Debug.Log(Core.Utils.Magnitude(50)); // 50 B
            Debug.Log(Core.Utils.Magnitude(5000)); // 5 KB
            Debug.Log(Core.Utils.Magnitude(5000000)); // 5 MB
            Debug.Log(Core.Utils.Magnitude(5000000000)); // 5 GB
            Debug.Log(Core.Utils.Magnitude(900000000000)); // 0.9 TB 
            Debug.Log(Core.Utils.Magnitude(5000000000000)); // 5 TB
            Debug.Log(Core.Utils.Magnitude(5000000000000000)); // 5 EB
            Debug.Log(Core.Utils.Magnitude(5000000000000000000)); // 5000 EB
            // ... damn cat napping on my keyboard....
        }
    }
}
