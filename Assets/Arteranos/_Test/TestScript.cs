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
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}
