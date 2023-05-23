/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ReadyPlayerMe
{
    public class CreatedAvatarAnimator : MonoBehaviour
    {
        public Transform target = null;
        public List<Transform> transforms = new();
        public float duration = 0.25f;

        private CancellationTokenSource ctx;

        private void OnDestroy() => ctx?.Cancel();

        public void AnimateTo(int index)
        {
            ctx?.Cancel();
            ctx = new CancellationTokenSource();

            _ = target.LerpTransform(transforms[index], duration, ctx.Token);
        }
    }
}
