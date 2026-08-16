// Renders the UI canvas to a 1080x1920 PNG so the portrait layout can be checked
// without fiddling with Game View aspect ratios.
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FleetBattle.EditorTools
{
    public static class PreviewCapture
    {
        public static string Capture(string outPath, int width = 1080, int height = 1920)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return "no canvas";

            var prevMode = canvas.renderMode;
            var prevCam = canvas.worldCamera;
            var prevOrder = canvas.sortingOrder;

            var camGo = new GameObject("__PreviewCam", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.05f, 0.09f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;
            camGo.transform.position = new Vector3(0f, 0f, -20f);

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            rt.Create();
            cam.targetTexture = rt;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
            Canvas.ForceUpdateCanvases();

            cam.Render();

            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;

            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllBytes(outPath, tex.EncodeToPNG());

            // restore
            cam.targetTexture = null;
            canvas.renderMode = prevMode;
            canvas.worldCamera = prevCam;
            canvas.sortingOrder = prevOrder;
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Canvas.ForceUpdateCanvases();

            return outPath;
        }
    }
}
