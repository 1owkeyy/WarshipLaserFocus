// Editor-only procedural art generator.
// Creates a cohesive flat-vector symbol set + UI chrome so the prototype has a
// deliberate, consistent look without shipping a pile of hand-made assets.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FleetBattle.EditorTools
{
    public static class ArtGenerator
    {
        const string Root = "Assets/Art";
        const int IconSize = 256;

        // ---------- Palette (shared by icons + UI so everything reads as one set) ----------
        static readonly Color Gold      = new Color32(0xFF, 0xC9, 0x3C, 0xFF); // XP
        static readonly Color CannonRed = new Color32(0xE2, 0x53, 0x2E, 0xFF); // Cannon
        static readonly Color ShieldBlue= new Color32(0x3E, 0x9B, 0xE8, 0xFF); // Shield
        static readonly Color TorpTeal  = new Color32(0x25, 0xD0, 0xC8, 0xFF); // Torpedo
        static readonly Color EnergyLime= new Color32(0xAE, 0xE2, 0x35, 0xFF); // Energy
        static readonly Color Ink       = new Color32(0x08, 0x14, 0x22, 0xFF); // shared outline

        [MenuItem("Fleet Battle/Generate Art")]
        public static string GenerateAll()
        {
            EnsureFolder(Root);
            EnsureFolder(Root + "/Symbols");
            EnsureFolder(Root + "/UI");

            WriteIcon("Symbols/icon_xp",      Gold,       Star5());
            WriteIcon("Symbols/icon_cannon",  CannonRed,  Cannon());
            WriteIcon("Symbols/icon_shield",  ShieldBlue, Shield());
            WriteIcon("Symbols/icon_torpedo", TorpTeal,   Torpedo());
            WriteIcon("Symbols/icon_energy",  EnergyLime, Bolt());

            WriteMono("UI/icon_crosshair", Crosshair());

            WritePanel("UI/panel_round", 96, 28, 0);
            WritePanel("UI/panel_stroke", 96, 28, 5);
            WriteGlow("UI/glow_radial", 256);
            WriteFade("UI/fade_vert", 32, 128);
            WriteSolid("UI/px_white", 8);
            WriteBackground("UI/bg_ocean", 512, 1024);

            AssetDatabase.Refresh();
            return "Art generated at " + Root;
        }

        // ================= shape definitions (normalised 0..1 space, y up) =================
        // Each icon is a list of layers; layer = (inside-test, colour tweak vs base).

        struct Layer
        {
            public Func<Vector2, bool> Inside;
            public float Shade; // -1 darker .. 0 base .. +1 lighter
            public Layer(Func<Vector2, bool> inside, float shade = 0f) { Inside = inside; Shade = shade; }
        }

        static List<Layer> Star5()
        {
            var pts = new List<Vector2>();
            for (int i = 0; i < 10; i++)
            {
                float r = (i % 2 == 0) ? 0.42f : 0.175f;
                float a = Mathf.Deg2Rad * (90f + i * 36f);
                pts.Add(new Vector2(0.5f + Mathf.Cos(a) * r, 0.5f + Mathf.Sin(a) * r));
            }
            return new List<Layer> { new Layer(Poly(pts)) };
        }

        static List<Layer> Cannon()
        {
            // barrel angled up-right, carriage block, wheel, cannonball leaving the muzzle
            var barrel = Capsule(new Vector2(0.26f, 0.36f), new Vector2(0.66f, 0.62f), 0.115f);
            var carriage = Poly(new List<Vector2> {
                new Vector2(0.18f,0.20f), new Vector2(0.52f,0.20f),
                new Vector2(0.46f,0.40f), new Vector2(0.22f,0.40f) });
            var wheel = Circle(new Vector2(0.31f, 0.245f), 0.115f);
            var hub = Circle(new Vector2(0.31f, 0.245f), 0.042f);
            var ball = Circle(new Vector2(0.79f, 0.735f), 0.115f);
            return new List<Layer> {
                new Layer(carriage, -0.35f),
                new Layer(barrel, 0f),
                new Layer(wheel, -0.35f),
                new Layer(hub, 0.45f),
                new Layer(ball, 0.2f),
            };
        }

        static List<Layer> Shield()
        {
            // Flat top, bezier flanks sweeping down to a point.
            var tip = new Vector2(0.5f, 0.09f);
            var pts = new List<Vector2> { new Vector2(0.16f, 0.88f), new Vector2(0.84f, 0.88f) };
            Bezier(pts, new Vector2(0.84f, 0.88f), new Vector2(0.88f, 0.34f), tip, 20);
            Bezier(pts, tip, new Vector2(0.12f, 0.34f), new Vector2(0.16f, 0.88f), 20);
            var body = Poly(pts);
            var chevron = Poly(new List<Vector2> {
                new Vector2(0.5f,0.66f), new Vector2(0.68f,0.55f), new Vector2(0.68f,0.44f),
                new Vector2(0.5f,0.55f), new Vector2(0.32f,0.44f), new Vector2(0.32f,0.55f) });
            return new List<Layer> { new Layer(body), new Layer(chevron, 0.55f) };
        }

        static List<Layer> Torpedo()
        {
            var body = Capsule(new Vector2(0.36f, 0.5f), new Vector2(0.62f, 0.5f), 0.16f);
            var nose = Poly(new List<Vector2> {
                new Vector2(0.62f,0.66f), new Vector2(0.90f,0.5f), new Vector2(0.62f,0.34f) });
            var finTop = Poly(new List<Vector2> {
                new Vector2(0.34f,0.62f), new Vector2(0.20f,0.80f), new Vector2(0.16f,0.56f) });
            var finBot = Poly(new List<Vector2> {
                new Vector2(0.34f,0.38f), new Vector2(0.20f,0.20f), new Vector2(0.16f,0.44f) });
            var band = Poly(new List<Vector2> {
                new Vector2(0.50f,0.66f), new Vector2(0.565f,0.66f),
                new Vector2(0.565f,0.34f), new Vector2(0.50f,0.34f) });
            return new List<Layer> {
                new Layer(finTop, -0.3f), new Layer(finBot, -0.3f),
                new Layer(body), new Layer(nose, 0.2f), new Layer(band, -0.45f),
            };
        }

        static List<Layer> Bolt()
        {
            var pts = new List<Vector2> {
                new Vector2(0.60f,0.92f), new Vector2(0.30f,0.50f), new Vector2(0.47f,0.50f),
                new Vector2(0.40f,0.08f), new Vector2(0.72f,0.53f), new Vector2(0.54f,0.53f) };
            return new List<Layer> { new Layer(Poly(pts)) };
        }

        // Targeting reticle for the torpedo zones. Drawn flat white so the UI can tint it
        // per state (idle / clear / shielded) without fighting a baked outline.
        static List<Layer> Crosshair()
        {
            var c = new Vector2(0.5f, 0.5f);
            Func<Vector2, bool> ring = p =>
            {
                float d = (p - c).magnitude;
                return d <= 0.42f && d >= 0.335f;
            };
            var layers = new List<Layer> { new Layer(ring), new Layer(Circle(c, 0.075f)) };
            layers.Add(new Layer(Capsule(new Vector2(0.5f, 0.96f), new Vector2(0.5f, 0.63f), 0.028f)));
            layers.Add(new Layer(Capsule(new Vector2(0.5f, 0.04f), new Vector2(0.5f, 0.37f), 0.028f)));
            layers.Add(new Layer(Capsule(new Vector2(0.04f, 0.5f), new Vector2(0.37f, 0.5f), 0.028f)));
            layers.Add(new Layer(Capsule(new Vector2(0.96f, 0.5f), new Vector2(0.63f, 0.5f), 0.028f)));
            return layers;
        }

        // ================= rasteriser =================

        static void WriteIcon(string relPath, Color baseColor, List<Layer> layers)
        {
            const int Over = 2;                          // render at 2x, box down - clean edges everywhere
            int N = IconSize * Over, SS = 2;
            var cov = new float[N * N];                 // union coverage (for the outline)
            var layerCov = new float[layers.Count][];
            for (int l = 0; l < layers.Count; l++) layerCov[l] = new float[N * N];

            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    for (int sy = 0; sy < SS; sy++)
                        for (int sx = 0; sx < SS; sx++)
                        {
                            var p = new Vector2((x + (sx + 0.5f) / SS) / N, (y + (sy + 0.5f) / SS) / N);
                            for (int l = 0; l < layers.Count; l++)
                                if (layers[l].Inside(p)) layerCov[l][y * N + x] += 1f / (SS * SS);
                        }
                    float u = 0f;
                    for (int l = 0; l < layers.Count; l++) u = Mathf.Max(u, layerCov[l][y * N + x]);
                    cov[y * N + x] = u;
                }

            var outline = Dilate(cov, N, Mathf.RoundToInt(N * 0.035f));
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    int i = y * N + x;
                    // subtle vertical gradient keeps the flat icons from looking dead
                    float g = Mathf.Lerp(-0.10f, 0.16f, y / (float)N);
                    Color c = Ink;
                    float a = outline[i];
                    for (int l = 0; l < layers.Count; l++)
                    {
                        float la = layerCov[l][i];
                        if (la <= 0.001f) continue;
                        Color lc = Shade(baseColor, layers[l].Shade + g);
                        c = Color.Lerp(c, lc, la);
                        a = Mathf.Max(a, la);
                    }
                    c.a = a;
                    px[i] = c;
                }
            SavePng(relPath, IconSize, IconSize, Downsample(px, N, Over), 0);
        }

        /// <summary>Alpha-weighted box downsample so edges don't pick up a dark fringe.</summary>
        static Color[] Downsample(Color[] src, int srcN, int factor)
        {
            int dstN = srcN / factor;
            var dst = new Color[dstN * dstN];
            float inv = 1f / (factor * factor);
            for (int y = 0; y < dstN; y++)
                for (int x = 0; x < dstN; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (int oy = 0; oy < factor; oy++)
                        for (int ox = 0; ox < factor; ox++)
                        {
                            var s = src[(y * factor + oy) * srcN + (x * factor + ox)];
                            r += s.r * s.a; g += s.g * s.a; b += s.b * s.a; a += s.a;
                        }
                    dst[y * dstN + x] = a > 0.0001f
                        ? new Color(r / a, g / a, b / a, a * inv)
                        : new Color(0f, 0f, 0f, 0f);
                }
            return dst;
        }

        /// <summary>Flat white silhouette, no outline - meant to be tinted by the UI.</summary>
        static void WriteMono(string relPath, List<Layer> layers)
        {
            const int Over = 2;
            int N = IconSize * Over, SS = 2;
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float a = 0f;
                    for (int sy = 0; sy < SS; sy++)
                        for (int sx = 0; sx < SS; sx++)
                        {
                            var p = new Vector2((x + (sx + 0.5f) / SS) / N, (y + (sy + 0.5f) / SS) / N);
                            for (int l = 0; l < layers.Count; l++)
                                if (layers[l].Inside(p)) { a += 1f / (SS * SS); break; }
                        }
                    px[y * N + x] = new Color(1f, 1f, 1f, a);
                }
            SavePng(relPath, IconSize, IconSize, Downsample(px, N, Over), 0);
        }

        static void WritePanel(string relPath, int size, int radius, int stroke)
        {
            int N = size, SS = 3;
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float a = 0f;
                    for (int sy = 0; sy < SS; sy++)
                        for (int sx = 0; sx < SS; sx++)
                        {
                            float px2 = x + (sx + 0.5f) / SS, py2 = y + (sy + 0.5f) / SS;
                            float d = RoundRectSdf(px2, py2, N, N, radius);
                            bool inside = stroke > 0 ? (d < 0f && d > -stroke) : d < 0f;
                            if (inside) a += 1f / (SS * SS);
                        }
                    px[y * N + x] = new Color(1f, 1f, 1f, a);
                }
            SavePng(relPath, N, N, px, radius + 4);
        }

        static void WriteGlow(string relPath, int N)
        {
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(N * 0.5f, N * 0.5f)) / (N * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    px[y * N + x] = new Color(1f, 1f, 1f, a * a * a);
                }
            SavePng(relPath, N, N, px, 0);
        }

        // Vertical white->transparent ramp; tinted dark and placed at the top/bottom of a
        // reel it sells the "symbols passing behind glass" look.
        static void WriteFade(string relPath, int W, int H)
        {
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            {
                float a = Mathf.Pow(y / (float)(H - 1), 1.4f); // opaque at top
                for (int x = 0; x < W; x++) px[y * W + x] = new Color(1f, 1f, 1f, a);
            }
            SavePng(relPath, W, H, px, 0);
        }

        static void WriteSolid(string relPath, int N)
        {
            var px = new Color[N * N];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            SavePng(relPath, N, N, px, 0);
        }

        static void WriteBackground(string relPath, int W, int H)
        {
            var deep = new Color32(0x05, 0x0C, 0x16, 0xFF);
            var mid = new Color32(0x0D, 0x22, 0x39, 0xFF);
            var horizon = new Color32(0x1B, 0x47, 0x6E, 0xFF);
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float t = y / (float)H;                       // 0 bottom -> 1 top
                    Color c = t < 0.55f ? Color.Lerp(deep, mid, t / 0.55f)
                                        : Color.Lerp(mid, horizon, (t - 0.55f) / 0.45f);
                    // soft light band around the mid horizon, plus corner vignette
                    float band = Mathf.Exp(-Mathf.Pow((t - 0.62f) / 0.10f, 2f)) * 0.16f;
                    c = Color.Lerp(c, new Color(0.45f, 0.75f, 1f), band);
                    float vx = Mathf.Abs(x / (float)W - 0.5f) * 2f, vy = Mathf.Abs(t - 0.5f) * 2f;
                    float vig = Mathf.Clamp01(1f - 0.55f * Mathf.Pow(Mathf.Max(vx, vy * 0.8f), 2.2f));
                    c *= vig; c.a = 1f;
                    px[y * W + x] = c;
                }
            SavePng(relPath, W, H, px, 0);
        }

        // ================= primitives =================

        static void Bezier(List<Vector2> into, Vector2 a, Vector2 c, Vector2 b, int steps)
        {
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps, u = 1f - t;
                into.Add(u * u * a + 2f * u * t * c + t * t * b);
            }
        }

        static Func<Vector2, bool> Circle(Vector2 c, float r) => p => (p - c).sqrMagnitude <= r * r;

        static Func<Vector2, bool> Capsule(Vector2 a, Vector2 b, float r) => p =>
        {
            Vector2 ab = b - a, ap = p - a;
            float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab));
            return (ap - ab * t).sqrMagnitude <= r * r;
        };

        static Func<Vector2, bool> Poly(List<Vector2> pts) => p =>
        {
            bool inside = false;
            for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++)
                if ((pts[i].y > p.y) != (pts[j].y > p.y) &&
                    p.x < (pts[j].x - pts[i].x) * (p.y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                    inside = !inside;
            return inside;
        };

        static float RoundRectSdf(float x, float y, float w, float h, float r)
        {
            float dx = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - r);
            float dy = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - r);
            float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
            return outside + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
        }

        // Separable max filter (fast). Two passes of a half-radius square approximate a
        // round dilation closely enough for a 4%-of-width outline.
        static float[] Dilate(float[] src, int N, int radius)
        {
            var pass = MaxPass(src, N, radius, true);
            return MaxPass(pass, N, radius, false);
        }

        static float[] MaxPass(float[] src, int N, int radius, bool horizontal)
        {
            var dst = new float[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float m = 0f;
                    for (int o = -radius; o <= radius; o++)
                    {
                        int nx = horizontal ? x + o : x;
                        int ny = horizontal ? y : y + o;
                        if (nx < 0 || ny < 0 || nx >= N || ny >= N) continue;
                        float v = src[ny * N + nx];
                        if (v > m) { m = v; if (m >= 1f) break; }
                    }
                    dst[y * N + x] = m;
                }
            return dst;
        }

        static Color Shade(Color c, float amount) =>
            amount >= 0f ? Color.Lerp(c, Color.white, Mathf.Clamp01(amount))
                         : Color.Lerp(c, new Color(0.05f, 0.09f, 0.14f), Mathf.Clamp01(-amount));

        // ================= io =================

        static void SavePng(string relPath, int w, int h, Color[] px, int border)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            string full = Root + "/" + relPath + ".png";
            File.WriteAllBytes(full, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(full, ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(full);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            if (border > 0) imp.spriteBorder = new Vector4(border, border, border, border);
            imp.SaveAndReimport();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
