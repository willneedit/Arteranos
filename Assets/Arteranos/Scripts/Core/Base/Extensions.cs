/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using System.Threading.Tasks;
using System.Threading;

namespace Arteranos.Core
{
    public static class Extensions
    {
        /// <summary>
        /// Returns a relevance index for the comparison.
        /// </summary>
        /// <param name="setting">The server settings</param>
        /// <param name="user">The user's search filter</param>
        /// <returns>5 for an exact determinate match, 1 for an inexact match, 0 for a mismatch</returns>
        public static int FuzzyEq(this bool? setting, bool? user)
        {
            if(setting == null) return 1;

            return !setting != user ? 5 : 0;
        }

        /// <summary>
        /// Finds the transform in the hierarchy tree by name, including searching the
        /// entire subtree below.
        /// </summary>
        /// <param name="t">The transform to begin searching</param>
        /// <param name="name">The transform's name to search for</param>
        /// <returns>The first found transform, otherwise null</returns>
        public static Transform FindRecursive(this Transform t, string name)
        {
            if(t.name == name) return t;

            for(int i = 0, c = t.childCount; i < c; i++)
            {
                Transform res = FindRecursive(t.GetChild(i), name);
                if(res != null) return res;
            }

            return null;
        }

        /// <summary>
        /// Performs a movement over time to a dedicated transform, like with a camera movement
        /// in a cutscene.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="targetTransform"></param>
        /// <param name="duration"></param>
        /// <param name="token"></param>
        public static async Task LerpTransform(this Transform transform,
            Transform targetTransform, float duration, CancellationToken token)
        {
            // ctx?.Cancel();
            // ctx = new CancellationTokenSource();

            float time = 0f;
            Vector3 startPosition = transform.localPosition;
            Quaternion startRotation = transform.localRotation;
            Vector3 startScale = transform.localScale;

            while(time < duration && !token.IsCancellationRequested)
            {
                float t = time / duration;

                t = 0.5f - (float) Mathf.Cos(t * Mathf.PI) * 0.5f;
                transform.localPosition = Vector3.Lerp(startPosition, targetTransform.localPosition, t);
                transform.localRotation = Quaternion.Lerp(startRotation, targetTransform.localRotation, t);
                transform.localScale = Vector3.Lerp(startScale, targetTransform.localScale, t);
                time += Time.deltaTime;
                await Task.Yield();
            }

            if(!token.IsCancellationRequested)
            {
                transform.localPosition = targetTransform.localPosition;
                transform.localRotation = targetTransform.localRotation;
                transform.localScale = targetTransform.localScale;
            }
        }
    }
}
