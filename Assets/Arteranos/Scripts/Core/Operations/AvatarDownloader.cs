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

namespace Arteranos.Core.Operations
{
    /// <summary>
    /// Cut down GameObjectInstantiator to create a single game object, not a full scene
    /// along with external lights and camera.
    /// </summary>
    internal class GltFastGameObjectInstantiator : GameObjectInstantiator
    {
        public GltFastGameObjectInstantiator(
            IGltfReadable gltf,
            Transform parent,
            InstantiationSettings settings = null
        )
            : base(gltf, parent, null, settings)
        {
        }

        public override void AddPrimitive(
            uint nodeIndex,
            string meshName,
            Mesh mesh,
            int[] materialIndices,
            uint[] joints = null,
            uint? rootJoint = null,
            float[] morphTargetWeights = null,
            int primitiveNumeration = 0
        )
        {
            if ((m_Settings.Mask & ComponentType.Mesh) == 0)
            {
                return;
            }

            GameObject meshGo;
            if (primitiveNumeration == 0)
            {
                // Use Node GameObject for first Primitive
                meshGo = m_Nodes[nodeIndex];
                // Ready Player Me - Parent mesh to Avatar root game object
                meshGo.transform.SetParent(m_Parent.transform);
            }
            else
            {
                meshGo = new GameObject(meshName);
                meshGo.transform.SetParent(m_Nodes[nodeIndex].transform, false);
                meshGo.layer = m_Settings.Layer;
            }

            Renderer renderer;

            bool hasMorphTargets = mesh.blendShapeCount > 0;
            if (joints == null && !hasMorphTargets)
            {
                MeshFilter meshFilter = meshGo.AddComponent<MeshFilter>();
                meshFilter.mesh = mesh;
                MeshRenderer meshRenderer = meshGo.AddComponent<MeshRenderer>();
                renderer = meshRenderer;
            }
            else
            {
                SkinnedMeshRenderer skinnedMeshRenderer = meshGo.AddComponent<SkinnedMeshRenderer>();
                skinnedMeshRenderer.updateWhenOffscreen = m_Settings.SkinUpdateWhenOffscreen;
                if (joints != null)
                {
                    Transform[] bones = new Transform[joints.Length];
                    for (int j = 0; j < bones.Length; j++)
                    {
                        uint jointIndex = joints[j];
                        bones[j] = m_Nodes[jointIndex].transform;
                    }
                    skinnedMeshRenderer.bones = bones;
                    if (rootJoint.HasValue)
                    {
                        skinnedMeshRenderer.rootBone = m_Nodes[rootJoint.Value].transform;
                    }
                }
                skinnedMeshRenderer.sharedMesh = mesh;
                if (morphTargetWeights != null)
                {
                    for (int i = 0; i < morphTargetWeights.Length; i++)
                    {
                        float weight = morphTargetWeights[i];
                        skinnedMeshRenderer.SetBlendShapeWeight(i, weight);
                    }
                }
                renderer = skinnedMeshRenderer;
            }

            Material[] materials = new Material[materialIndices.Length];
            for (int index = 0; index < materials.Length; index++)
            {
                Material material = m_Gltf.GetMaterial(materialIndices[index]) ?? m_Gltf.GetDefaultMaterial();
                materials[index] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }

    internal class FixupSkeletonOp : IAsyncOperation<Context>
    {
        private const string BONE_HIPS = "Hips";
        private const string BONE_ARMATURE = "Armature";

        public int Timeout { get; set; }
        public float Weight { get; set; } = 0.01f;
        public string Caption { get; set; } = "Fixup avatar skeleton";

        public Action<float> ProgressChanged { get; set; }

        // Synced task -- needs to be in the main thread !
        public Task<Context> ExecuteAsync(Context _context, CancellationToken token)
        {
            AvatarDownloaderContext context = _context as AvatarDownloaderContext;

            // Add the armature (root) bone, if it isn't already present
            Transform avatarTransform = context.Avatar.transform;
            AddArmatureBone(avatarTransform);

            // Try to find the IK relevant limbs
            context.LeftFoot = AvatarDownloader.TrySidedLimb(context, "Foot", false);
            context.RightFoot = AvatarDownloader.TrySidedLimb(context, "Foot", true);
            context.LeftHand = AvatarDownloader.TrySidedLimb(context, "Hand", false);
            context.RightHand = AvatarDownloader.TrySidedLimb(context, "Hand", true);

            return Task.FromResult<Context>(context);
        }

        private static void AddArmatureBone(Transform avatarTransform)
        {
            if (!avatarTransform.Find(BONE_ARMATURE))
            {
                GameObject armature = new(BONE_ARMATURE);
                armature.transform.parent = avatarTransform;

                Transform hips = avatarTransform.Find(BONE_HIPS);
                if (hips) hips.parent = armature.transform;
            }
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
                GltFastGameObjectInstantiator customInstantiator = new(gltf, context.Avatar.transform);

                await gltf.InstantiateMainSceneAsync(customInstantiator);
            }


            return context;
        }
    }
    
    public static class AvatarDownloader
    {
        public static (AsyncOperationExecutor<Context>, Context) PrepareDownloadAvatar(Cid cid, int timeout = 600)
        {
            AvatarDownloaderContext context = new()
            {
                Cid = cid,
                TargetFile = GetAvatarCacheFile(cid),
            };

            AsyncOperationExecutor<Context> executor = new(new IAsyncOperation<Context>[]
            {
                new AssetDownloadOp(),
                new SetupAvatarObjOp(),
                new FixupSkeletonOp(),
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
        {
            AvatarDownloaderContext context = _context as AvatarDownloaderContext;
            return context.Avatar;
        }

    }
}