using UnityEngine;

namespace TurboPattes.Game.Visuals
{
    /// <summary>Textures générées au lancement (aucun fichier image dans le projet).</summary>
    public static class ProceduralTextures
    {
        static Texture2D _softDot, _circle, _asphalt, _curb, _checker, _grass, _chevrons, _barrier, _white, _sand, _strata, _canyonBarrier;

        static Texture2D New(string name, int w, int h, TextureWrapMode wrap = TextureWrapMode.Repeat, bool mips = true)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, mips)
            {
                name = name,
                wrapMode = wrap,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4,
            };
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        /// <summary>Point flou blanc (particules).</summary>
        public static Texture2D SoftDot
        {
            get
            {
                if (_softDot != null) return _softDot;
                const int n = 64;
                _softDot = New("SoftDot", n, n, TextureWrapMode.Clamp);
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    _softDot.SetPixel(x, y, new Color(1, 1, 1, a * a));
                }
                _softDot.Apply();
                return _softDot;
            }
        }

        /// <summary>Disque net blanc (interface).</summary>
        public static Texture2D Circle
        {
            get
            {
                if (_circle != null) return _circle;
                const int n = 128;
                _circle = New("Circle", n, n, TextureWrapMode.Clamp);
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) - n * 0.5f, dy = (y + 0.5f) - n * 0.5f;
                    float a = Mathf.Clamp01(n * 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                    _circle.SetPixel(x, y, new Color(1, 1, 1, a));
                }
                _circle.Apply();
                return _circle;
            }
        }

        public static Texture2D White
        {
            get
            {
                if (_white != null) return _white;
                _white = New("White", 4, 4, TextureWrapMode.Clamp, false);
                var px = new Color[16];
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                _white.SetPixels(px);
                _white.Apply();
                return _white;
            }
        }

        /// <summary>
        /// Bitume : u = travers de la piste (0..1), v = 1 texture tous les 16 m.
        /// Lignes blanches de bord et pointillés au centre.
        /// </summary>
        public static Texture2D Asphalt
        {
            get
            {
                if (_asphalt != null) return _asphalt;
                const int w = 128, h = 256;
                _asphalt = New("Asphalt", w, h);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float n = Hash(x, y) * 0.08f + Hash(x / 4, y / 4) * 0.06f;
                    float g = 0.36f + n;
                    var c = new Color(g, g, g * 1.04f, 1);
                    float u = (x + 0.5f) / w;
                    if ((u > 0.035f && u < 0.06f) || (u > 0.94f && u < 0.965f)) c = new Color(0.95f, 0.95f, 0.92f);
                    if (u > 0.49f && u < 0.51f && (y % 128) < 64) c = new Color(0.93f, 0.9f, 0.75f);
                    _asphalt.SetPixel(x, y, c);
                }
                _asphalt.Apply();
                return _asphalt;
            }
        }

        /// <summary>Vibreur rouge et blanc (bandes dans le sens de la piste).</summary>
        public static Texture2D Curb
        {
            get
            {
                if (_curb != null) return _curb;
                const int w = 8, h = 64;
                _curb = New("Curb", w, h);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    _curb.SetPixel(x, y, y < h / 2 ? new Color(0.92f, 0.16f, 0.18f) : new Color(0.97f, 0.97f, 0.97f));
                _curb.Apply();
                return _curb;
            }
        }

        public static Texture2D Checker
        {
            get
            {
                if (_checker != null) return _checker;
                const int n = 64;
                _checker = New("Checker", n, n);
                _checker.filterMode = FilterMode.Point;
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    _checker.SetPixel(x, y, ((x / 16 + y / 16) & 1) == 0 ? Color.white : new Color(0.08f, 0.08f, 0.1f));
                _checker.Apply();
                return _checker;
            }
        }

        public static Texture2D Grass
        {
            get
            {
                if (_grass != null) return _grass;
                const int n = 128;
                _grass = New("Grass", n, n);
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float v = Hash(x, y) * 0.07f + Hash(x / 8, y / 8) * 0.08f;
                    bool stripe = ((x + y) / 32 & 1) == 0;
                    float s = stripe ? 0.03f : 0f;
                    _grass.SetPixel(x, y, new Color(0.30f + v + s, 0.66f + v + s, 0.22f + v * 0.5f, 1));
                }
                _grass.Apply();
                return _grass;
            }
        }

        /// <summary>Chevrons du tapis turbo (pointent vers v croissant).</summary>
        public static Texture2D Chevrons
        {
            get
            {
                if (_chevrons != null) return _chevrons;
                const int w = 64, h = 64;
                _chevrons = New("Chevrons", w, h);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / w - 0.5f);
                    float v = (y + 0.5f) / h;
                    float band = Mathf.Repeat(v - u * 0.9f, 0.5f);
                    bool on = band < 0.22f;
                    _chevrons.SetPixel(x, y, on ? new Color(1f, 0.95f, 0.35f) : new Color(1f, 0.45f, 0.1f));
                }
                _chevrons.Apply();
                return _chevrons;
            }
        }

        /// <summary>Barrière : bandes bleues et blanches, liseré jaune en haut.</summary>
        public static Texture2D Barrier
        {
            get
            {
                if (_barrier != null) return _barrier;
                const int w = 64, h = 16;
                _barrier = New("Barrier", w, h);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = (x / 16 & 1) == 0 ? new Color(0.18f, 0.45f, 0.95f) : new Color(0.97f, 0.97f, 0.97f);
                    if (y >= h - 3) c = new Color(1f, 0.82f, 0.2f);
                    _barrier.SetPixel(x, y, c);
                }
                _barrier.Apply();
                return _barrier;
            }
        }
    
        /// <summary>Sable du canyon : grain fin et rides douces.</summary>
        public static Texture2D Sand
        {
            get
            {
                if (_sand != null) return _sand;
                const int n = 128;
                _sand = New("Sand", n, n);
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float v = Hash(x, y) * 0.06f + Hash(x / 8, y / 8) * 0.05f;
                    float ripple = Mathf.Sin((x * 0.35f + y * 0.1f) / n * Mathf.PI * 8f) * 0.025f;
                    _sand.SetPixel(x, y, new Color(0.86f + v + ripple, 0.64f + v + ripple, 0.42f + v * 0.6f, 1));
                }
                _sand.Apply();
                return _sand;
            }
        }

        /// <summary>Strates de roche (bandes horizontales selon v) pour les mesas et les rochers.</summary>
        public static Texture2D Strata
        {
            get
            {
                if (_strata != null) return _strata;
                const int w = 16, h = 64;
                _strata = New("Strata", w, h);
                Color[] bands =
                {
                    new(0.78f, 0.36f, 0.2f), new(0.88f, 0.5f, 0.26f), new(0.95f, 0.72f, 0.48f), new(0.7f, 0.3f, 0.2f),
                    new(0.9f, 0.6f, 0.36f), new(0.82f, 0.42f, 0.24f),
                };
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int band = (int)(y / (float)h * 9f + Mathf.Sin(x * 0.4f) * 0.15f) % bands.Length;
                    float v = Hash(x, y) * 0.05f;
                    var c = bands[band];
                    _strata.SetPixel(x, y, new Color(c.r + v, c.g + v, c.b + v, 1));
                }
                _strata.Apply();
                return _strata;
            }
        }

        /// <summary>Muret du canyon : bandes terre cuite et crème, liseré turquoise en haut.</summary>
        public static Texture2D CanyonBarrier
        {
            get
            {
                if (_canyonBarrier != null) return _canyonBarrier;
                const int w = 64, h = 16;
                _canyonBarrier = New("CanyonBarrier", w, h);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = (x / 16 & 1) == 0 ? new Color(0.85f, 0.35f, 0.2f) : new Color(0.98f, 0.92f, 0.8f);
                    if (y >= h - 3) c = new Color(0.2f, 0.75f, 0.75f);
                    _canyonBarrier.SetPixel(x, y, c);
                }
                _canyonBarrier.Apply();
                return _canyonBarrier;
            }
        }
    }
}
