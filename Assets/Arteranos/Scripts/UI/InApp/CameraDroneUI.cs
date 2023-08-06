/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.IO;

namespace Arteranos.UI
{
    public class CameraDroneUI : MonoBehaviour
    {
        [SerializeField] private Camera DroneCamera;

        public void TakePhoto()
        {
            string name = $"Arteranos-Photo-{DateTime.Now.ToString("yyyyMMddHHmmss")}.png";

            // FIXME Windows only?
            string picpath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            RenderTexture rt = DroneCamera.targetTexture;

            RenderTexture mRt = new RenderTexture(rt.width, rt.height, rt.depth, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            mRt.antiAliasing = rt.antiAliasing;

            Texture2D Image = new(rt.width, rt.height, TextureFormat.ARGB32, false);
            DroneCamera.targetTexture = mRt;
            DroneCamera.Render();
            RenderTexture.active = mRt;

            Image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            Image.Apply();

            DroneCamera.targetTexture = rt;
            DroneCamera.Render();
            RenderTexture.active = rt;

            byte[] Bytes = Image.EncodeToPNG();
            string path = Path.Combine(picpath, name);
            Debug.Log($"Writing screenshot to {path}");
            File.WriteAllBytes(path, Bytes);

            Destroy(Image);

            Destroy(mRt);
        }
    }
}
