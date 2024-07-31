/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using System;
using System.IO;
using UnityEngine.Experimental.Rendering;
using System.Threading.Tasks;

namespace Arteranos.UI
{
    public class CameraDroneUI : MonoBehaviour
    {
        [SerializeField] private Camera DroneCamera;

        public void TakePhoto()
        {
            string name = $"Arteranos-Photo-{DateTime.Now:yyyyMMddHHmmss}.png";
            // FIXME Windows only?
            string picpath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            string path = Path.Combine(picpath, name);

            RenderTexture rt = DroneCamera.targetTexture;

            RenderTexture mRt = new(rt.width, rt.height, rt.depth, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                antiAliasing = rt.antiAliasing
            };

            Texture2D tex = new(rt.width, rt.height, TextureFormat.ARGB32, false);
            DroneCamera.targetTexture = mRt;
            DroneCamera.Render();
            RenderTexture.active = mRt;

            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            DroneCamera.targetTexture = rt;
            //DroneCamera.Render();
            RenderTexture.active = rt;

            byte[] imgBytes = tex.GetRawTextureData();
            GraphicsFormat graphicsFormat = tex.graphicsFormat;
            uint width = (uint) rt.width;
            uint height = (uint) rt.height;

            Task.Run(() => WriteScreenshot(path, imgBytes, graphicsFormat, width, height));

            Destroy(tex);

            Destroy(mRt);
        }

        private static void WriteScreenshot(string path, byte[] imgBytes, GraphicsFormat graphicsFormat, uint width, uint height)
        {
            byte[] Bytes = ImageConversion.EncodeArrayToPNG(imgBytes, graphicsFormat, width, height);
            Debug.Log($"Writing screenshot to {path}");
            File.WriteAllBytes(path, Bytes);
        }
    }
}
