/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Arteranos.Avatar;

namespace Arteranos.UI
{
    public class HitBox : MonoBehaviour, IHitBox
    {
        public IAvatarBrain Brain { get; set; } = null;
        private IAvatarLoader Body = null;

        float fullHeight = -1;
        private void Start() => Body = Brain.Body;

        private void Update()
        {
            if(fullHeight != Body.FullHeight)
                UpdateAvatarHeight();
        }

        private void UpdateAvatarHeight()
        {
            fullHeight = Body.FullHeight;

            CapsuleCollider cc = GetComponent<CapsuleCollider>();
            cc.height = fullHeight;
            transform.localPosition = new Vector3(0, fullHeight / 2, 0);
        }

        public void OnTargeted(bool inSight)
        {
            if(inSight)
                NameplateUIFactory.New(Brain.gameObject);
            Debug.Log($"Ping? {inSight}");
        }
    }
}
