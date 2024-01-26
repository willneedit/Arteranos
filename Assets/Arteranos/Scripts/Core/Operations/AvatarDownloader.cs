/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

using Arteranos.Core;
using System.Threading;
using Utils = Arteranos.Core.Utils;
using Ipfs;
using GLTFast;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Arteranos.Avatar;

namespace Arteranos.Core.Operations
{
    internal class ObjectStats : IObjectStats
    {
        public int Count { get; set; }
        public int Vertices { get; set; }
        public int Triangles { get; set; }
        public int Materials { get; set; }
        public float Rating { get; set; }
    }

    internal class InstallEyeAnimationOp : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 0.01f;

        public string Caption { get; set; } = "Installing the eye animation handler";

        public Action<float> ProgressChanged { get; set; }

        public Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            AvatarDownloaderContext context = _context as AvatarDownloaderContext;

            if(context.InstallEyeAnimation)
            {
                AvatarEyeAnimator a = context.Avatar.AddComponent<AvatarEyeAnimator>();
                a.AvatarMeasures = context;
            }

            return Task.FromResult<Context>(context);
        }
    }

    internal class FindBlendShapesOp : IAsyncOperation<Context>
    {
        private const string MOUTH_OPEN_BLEND_SHAPE_NAME = "mouthOpen";
        private const string EYE_BLINK_LEFT_BLEND_SHAPE_NAME = "eyeBlinkLeft";
        private const string EYE_BLINK_RIGHT_BLEND_SHAPE_NAME = "eyeBlinkRight";


        public int Timeout { get; set; }
        public float Weight { get; set; } = 0.01f;
        public string Caption { get; set; } = "Getting Blend Shapes";

        public Action<float> ProgressChanged { get; set; }

        public Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            AvatarDownloaderContext context = _context as AvatarDownloaderContext;

            Transform avatarTransform = context.Avatar.transform;

            context.MouthOpen = new();
            FindBlendShapedMeshes(avatarTransform, MOUTH_OPEN_BLEND_SHAPE_NAME, context.MouthOpen);

            context.EyeBlinkLeft = new();
            FindBlendShapedMeshes(avatarTransform, EYE_BLINK_LEFT_BLEND_SHAPE_NAME, context.EyeBlinkLeft);

            context.EyeBlinkRight = new();
            FindBlendShapedMeshes(avatarTransform, EYE_BLINK_RIGHT_BLEND_SHAPE_NAME, context.EyeBlinkRight);

            return Task.FromResult<Context>(context);
        }

        public void FindBlendShapedMeshes(Transform t, string whichBlendShape, List<MeshBlendShapeIndex> collected)
        {
            SkinnedMeshRenderer[] meshes = t.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach(SkinnedMeshRenderer mesh in meshes)
            {
                int index = mesh.sharedMesh.GetBlendShapeIndex(whichBlendShape);
                if (index >= 0)
                    collected.Add(new()
                    {
                        Index = index,
                        Renderer = mesh,
                    });
            }
        }
    }

    internal class MeasureSkeletonOp : IAsyncOperation<Context>
    {
        private const string BONE_ARMATURE = "Armature";

        private readonly ObjectStats warningLevels = new ObjectStats()
        {
            Count = 2,
            Vertices = 12000,
            Triangles = 60000,
            Materials = 2,
        };

        private readonly ObjectStats errorLevels = new ObjectStats()
        {
            Count = 4,
            Vertices = 16000,
            Triangles = 90000,
            Materials = 4,
        };

        public int Timeout { get; set; }
        public float Weight { get; set; } = 0.01f;
        public string Caption { get; set; } = "Fixup avatar skeleton";

        public Action<float> ProgressChanged { get; set; }

        // Synced task -- needs to be in the main thread !
        public Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            AvatarDownloaderContext context = _context as AvatarDownloaderContext;

            Transform avatarTransform = context.Avatar.transform;

            // Pull up the other children besides the bones in the hierarchy root
            PullupMeshes(avatarTransform);

            // Try to find the IK relevant limbs
            context.LeftFoot = AvatarDownloader.TrySidedLimb(context, "Foot", false);
            context.RightFoot = AvatarDownloader.TrySidedLimb(context, "Foot", true);
            context.LeftHand = AvatarDownloader.TrySidedLimb(context, "Hand", false);
            context.RightHand = AvatarDownloader.TrySidedLimb(context, "Hand", true);

            // Find the eyes (usually two ... :)
            context.Eyes = new();
            FindLimbsPattern(avatarTransform, "(.*Eye|Eye.*)", context.Eyes);

            // How far the feet joints are lifted from the ground (like, the soles)
            float FootElevation = 0.0f;
            int FeetCount = 0;

            if(context.LeftFoot)
            {
                FootElevation += context.LeftFoot.position.y;
                FeetCount++;
            }

            if(context.RightFoot)
            {
                FootElevation += context.RightFoot.position.y;
                FeetCount++;
            }

            if(FeetCount > 0) FootElevation = FootElevation / FeetCount;
            context.FootElevation = FootElevation;

            // Maybe I had to look for the way to get the bounding box.
            context.FullHeight = FoldTransformHierarchy(avatarTransform, 0.0f, 
                (t, f) => t.position.y > f ? t.position.y : f);

            // The Avatar Point Of View. Between the eyes, but not if the avatar breakdancing...
            Vector3 centerEyePos = Vector3.zero;
            foreach (Transform t in context.Eyes) centerEyePos += t.position;
            GameObject cEyeGO = new("Avatar_POV");
            if (context.Eyes.Count > 0)
                centerEyePos = centerEyePos / context.Eyes.Count;
            cEyeGO.transform.parent = avatarTransform;
            cEyeGO.transform.position = centerEyePos;
            context.CenterEye = cEyeGO.transform;
            context.EyeHeight = context.CenterEye.position.y;

            // The hand rotation offset. T-pose needs to palms down.
            context.LhrOffset = Quaternion.Euler(0, 90, 90);
            context.RhrOffset = Quaternion.Euler(0, -90, -90);

            // Get the overall rating about the avatar
            Utils.RateGameObject(context.Avatar, warningLevels, errorLevels, context);

            return Task.FromResult<Context>(context);
        }

        private static void PullupMeshes(Transform avatarTransform)
        {
            Transform armature = avatarTransform.Find(BONE_ARMATURE);
            if (!armature) return;

            for (int i = 0; i < armature.childCount; i++)
            {
                Transform transform = armature.GetChild(i);
                if (transform.gameObject.GetComponent<Renderer>())
                {
                    transform.parent = avatarTransform;
                    i--;
                }
            }
        }

        public static void FindLimbsPattern(Transform t, string pattern, List<Transform> collected)
        {
            Match m = Regex.Match(t.name, pattern, RegexOptions.IgnoreCase);
            if (m.Success) collected.Add(t);

            foreach(Transform transform in t)
                FindLimbsPattern(transform, pattern, collected);
        }

        public static T FoldTransformHierarchy<T>(Transform t, T start, Func<Transform, T, T> progress)
        {
            start = progress(t, start);
            foreach(Transform tc in t)
                start = FoldTransformHierarchy(tc, start, progress);

            return start;
        }
    }

    internal class SetupAvatarObjOp : IAsyncOperation<Context>
    {
        public int Timeout { get; set; }
        public float Weight { get; set; } = 0.5f;
        public string Caption { get; set; } = "Setting up game object";
        public Action<float> ProgressChanged { get; set; }

        public async Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            AvatarDownloaderContext context = _context as AvatarDownloaderContext;

            byte[] data = await File.ReadAllBytesAsync(context.TargetFile);

            GltfImport gltf = new(deferAgent: new UninterruptedDeferAgent());

            var success = await gltf.LoadGltfBinary(data, cancellationToken: token);

            if (success)
            {
                context.Avatar = new GameObject();
                context.Avatar.SetActive(false);
                GameObjectInstantiator customInstantiator = new(gltf, context.Avatar.transform);

                await gltf.InstantiateMainSceneAsync(customInstantiator);
            }


            return context;
        }
    }
    
    public static class AvatarDownloader
    {
        public static (AsyncOperationExecutor<Context>, Context) PrepareDownloadAvatar(Cid cid, IAvatarDownloaderOptions options = null, int timeout = 600)
        {
            AvatarDownloaderContext context = new()
            {
                Cid = cid,
                TargetFile = GetAvatarCacheFile(cid),

                InstallAvatarController = options?.InstallAvatarController ?? false,
                InstallEyeAnimation = options?.InstallEyeAnimation ?? false,
            };

            AsyncOperationExecutor<Context> executor = new(new IAsyncOperation<Context>[]
            {
                new AssetDownloadOp(),
                new SetupAvatarObjOp(),
                new MeasureSkeletonOp(),
                new FindBlendShapesOp(),
                new InstallEyeAnimationOp(),
            })
            { Timeout = timeout };

            return (executor, context);
        }

        /// <summary>
        /// Try to find a particular limb, like left foot or the right hand. Uses some
        /// heuristics to determine the naming pattern, like lHand, Left_Hand, leftHand
        /// and so on.
        /// </summary>
        /// <param name="context">The avatar downloader context</param>
        /// <param name="limb">Hand, Arm, Leg, ...</param>
        /// <param name="right">true if the right one</param>
        /// <returns>The limb transform, or null if it's missing (Ouch..!)</returns>
        /// <remarks>Updates the context once the naming pattern has been found.</remarks>
        internal static Transform TrySidedLimb(AvatarDownloaderContext context, string limb, bool right)
        {
            /// <summary>
            /// 0 - Limb name
            /// 1 - Side name letter ('l' or 'r')
            /// 2 - Side name word ('left' or 'right')
            /// </summary>
            string[] SidedLimbPattern = new string[]
            {
            "{1}{0}",       // lHand
            "{0}{1}",       // Handl
            "{1}_{0}",      // l_Hand
            "{0}_{1}",      // Hand_l
            "{2}{0}",       // leftHand
            "{0}{2}",       // Handleft
            "{2}_{0}",      // left_Hand
            "{0}_{2}",      // Hand_left
            };

            static void GetSideWord(bool right, out string letter, out string word, bool sidedCapitalized)
            {
                if (sidedCapitalized == true)
                {
                    letter = right ? "R" : "L";
                    word = right ? "Right" : "Left";
                }
                else
                {
                    letter = right ? "r" : "l";
                    word = right ? "right" : "left";
                }
            }

            Transform avatarTransform = context.Avatar.transform;

            // We've already got something, stick to the pattern.
            if (context.SidedCapitalized != null)
            {
                bool sidedCapitalized = context.SidedCapitalized.Value;
                GetSideWord(right, out string letter, out string word, sidedCapitalized);

                string toFind = string.Format(SidedLimbPattern[context.SidedPatternIndex], limb, letter, word);
                return avatarTransform.FindRecursive(toFind);
            }

            foreach (bool tryCap in new bool[] { false, true })
            {
                GetSideWord(right, out string letter, out string word, tryCap);
                for (int i = 0; i < SidedLimbPattern.Length; i++)
                {
                    string toFind = string.Format(SidedLimbPattern[i], limb, letter, word);
                    Transform found = avatarTransform.FindRecursive(toFind);
                    if (found != null)
                    {
                        context.SidedCapitalized = tryCap;
                        context.SidedPatternIndex = i;
                        return found;
                    }
                }
            }

            return null;
        }

        public static string GetAvatarCacheFile(Cid cid)
            => $"{FileUtils.temporaryCachePath}/AvatarCache/{Utils.GetURLHash(cid)}.glb";

        public static GameObject GetLoadedAvatar(Context _context) 
            => (_context as AvatarDownloaderContext).Avatar;

        public static IObjectStats GetAvatarRating(Context _context)
            => _context as IObjectStats;

        public static IAvatarMeasures GetAvatarMeasures(Context _context) 
            => _context as IAvatarMeasures;
    }
}