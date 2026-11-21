using System;
using System.Collections.Generic;
using TurboPattes.Core;
using TurboPattes.Game.Kart;
using UnityEngine;
using static TurboPattes.Game.Visuals.Sdf;

namespace TurboPattes.Game.Visuals
{
    /// <summary>
    /// Icônes d'objets et portraits des pilotes, dessinés par code au premier usage (fond transparent).
    /// </summary>
    public static class Icons
    {
        static readonly Dictionary<ItemKind, Texture2D> ItemCache = new();
        static readonly Dictionary<DriverSpec, Texture2D> PortraitCache = new();

        static readonly Color Dark = new(0.08f, 0.09f, 0.1f);

        public static Texture2D Item(ItemKind kind)
        {
            if (kind == ItemKind.None) return null;
            if (ItemCache.TryGetValue(kind, out var tex)) return tex;
            var c = new IconCanvas();
            switch (kind)
            {
                case ItemKind.Carrot: Carrot(c); break;
                case ItemKind.Coconut: Coconut(c); break;
                case ItemKind.Honey: Honey(c); break;
                case ItemKind.Feather: Feather(c); break;
                case ItemKind.Yarn: Yarn(c); break;
                case ItemKind.Comet: Comet(c); break;
                default: Shield(c); break;
            }
            return ItemCache[kind] = c.ToTexture("Item" + kind);
        }

        static Texture2D _emblem;

        /// <summary>
        /// Emblème de l'écran titre (même motif que Editor/AppIcon) : drapeau à damier penché et deux chevrons rouge
        /// course qui filent vers la droite, sur fond transparent.
        /// </summary>
        public static Texture2D Emblem()
        {
            if (_emblem != null) return _emblem;
            var c = new IconCanvas();
            var chalk = UiColor(Palette.Chalk);
            var carbon = UiColor(Palette.Carbon);
            var red = UiColor(Palette.Red);
            // Drapeau : parallélogramme penché, damier 4 × 3 (cases carbone découpées dans le blanc).
            var flag = Polygon(0.015f, 0.16f, 0.26f, 0.62f, 0.26f, 0.52f, 0.66f, 0.06f, 0.66f);
            c.Fill(flag, chalk, IconCanvas.Line, 0f);
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 4; col++)
            {
                if ((row + col) % 2 == 0) continue;
                float y0 = 0.26f + row * 0.1333f, y1 = y0 + 0.1333f;
                // Bords penchés comme le drapeau : x recule de 0,25 par unité de hauteur.
                float x0 = 0.16f + col * 0.115f - (y0 - 0.26f) * 0.25f, x1 = x0 + 0.115f;
                c.Fill(Intersect(Polygon(0f, x0, y0, x1, y0, x1 - 0.0333f, y1, x0 - 0.0333f, y1), flag), carbon, 0f, 0f);
            }
            // Hampe.
            c.Fill(Capsule(0.15f, 0.22f, 0.04f, 0.84f, 0.022f), UiColor(Palette.Steel), IconCanvas.Line * 0.6f, 0f);
            // Deux chevrons « > » rouges.
            foreach (float x in new[] { 0.62f, 0.76f })
                c.Fill(Polygon(0.01f, x, 0.3f, x + 0.08f, 0.3f, x + 0.2f, 0.46f, x + 0.08f, 0.62f, x, 0.62f, x + 0.12f, 0.46f), red, IconCanvas.Line * 0.7f, 0f);
            return _emblem = c.ToTexture("Emblem");
        }

        static Color UiColor(Rgb c) => new(c.R, c.G, c.B);

        static readonly Dictionary<(int, Color, Color), Texture2D> PlateCache = new();

        /// <summary>
        /// Plaque de numéro de course carrée (fond <paramref name="plate"/> sur toute la texture, sans transparence,
        /// chiffres <paramref name="ink"/> à sept segments, <see cref="SevenSegment"/>), pour le nez, les pontons et l'arrière du kart.
        /// </summary>
        public static Texture2D NumberPlate(int number, Color plate, Color ink)
        {
            var key = (number, plate, ink);
            if (PlateCache.TryGetValue(key, out var tex)) return tex;
            var c = new IconCanvas(128);
            c.Fill(Polygon(0f, -0.1f, -0.1f, 1.1f, -0.1f, 1.1f, 1.1f, -0.1f, 1.1f), plate, 0f, 0f);
            string text = Mathf.Clamp(number, 0, 99).ToString();
            // Case d'un chiffre : 0,26 de large, 0,56 de haut, chiffres légèrement penchés (italique de course).
            const float cw = 0.26f, ch = 0.56f, gap = 0.1f, slant = 0.1f, stroke = 0.045f;
            float total = text.Length * cw + (text.Length - 1) * gap;
            float x0 = 0.5f - total * 0.5f, y0 = 0.5f - ch * 0.5f;
            for (int k = 0; k < text.Length; k++)
            {
                var on = SevenSegment.Segments(text[k]);
                float left = x0 + k * (cw + gap);
                for (int i = 0; i < 7; i++)
                {
                    if (!on[i]) continue;
                    var (ax, ay, bx, by) = SevenSegment.Stroke(i);
                    float pay = y0 + ay * ch * 0.5f, pby = y0 + by * ch * 0.5f;
                    float pax = left + ax * cw + (0.5f - (pay - y0) / ch) * slant, pbx = left + bx * cw + (0.5f - (pby - y0) / ch) * slant;
                    c.Fill(Capsule(pax, pay, pbx, pby, stroke), ink, 0f, 0f);
                }
            }
            return PlateCache[key] = c.ToTexture("Plate" + number);
        }

        public static Texture2D Portrait(DriverSpec spec)
        {
            if (PortraitCache.TryGetValue(spec, out var tex)) return tex;
            var c = new IconCanvas();
            DrawPortrait(c, spec);
            return PortraitCache[spec] = c.ToTexture("Portrait" + spec.Name);
        }

        // ---------- Objets ----------

        static void Carrot(IconCanvas c)
        {
            var leaf = new Color(0.3f, 0.8f, 0.25f);
            c.Fill(Ellipse(0.72f, 0.2f, 0.07f, 0.16f, 20f), leaf);
            c.Fill(Ellipse(0.8f, 0.3f, 0.07f, 0.15f, 75f), leaf);
            c.Fill(Ellipse(0.66f, 0.24f, 0.06f, 0.13f, -25f), new Color(0.38f, 0.88f, 0.3f));
            var body = Cone(0.63f, 0.37f, 0.17f, 0.2f, 0.84f, 0.035f);
            c.Fill(body, new Color(1f, 0.55f, 0.12f));
            // Stries et reflet, coupés au contour de la carotte.
            var groove = new Color(0.85f, 0.38f, 0.05f);
            c.Fill(Intersect(Capsule(0.46f, 0.43f, 0.56f, 0.53f, 0.014f), body), groove, 0f, 0f);
            c.Fill(Intersect(Capsule(0.38f, 0.58f, 0.46f, 0.66f, 0.013f), body), groove, 0f, 0f);
            c.Fill(Intersect(Capsule(0.62f, 0.5f, 0.68f, 0.56f, 0.012f), body), groove, 0f, 0f);
            c.Fill(Capsule(0.5f, 0.36f, 0.34f, 0.56f, 0.025f), new Color(1f, 0.85f, 0.6f, 0.7f), 0f, 0f);
        }

        static void Coconut(IconCanvas c)
        {
            var shell = Circle(0.5f, 0.53f, 0.34f);
            c.Fill(shell, new Color(0.55f, 0.33f, 0.17f), IconCanvas.Line, 0.3f);
            // Fibres.
            var fibre = new Color(0.4f, 0.22f, 0.1f);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 0.71f + 0.4f, x = 0.5f + Mathf.Cos(a) * (0.12f + (i % 3) * 0.07f), y = 0.55f + Mathf.Sin(a) * (0.1f + (i % 2) * 0.1f);
                c.Fill(Intersect(Capsule(x - 0.04f, y - 0.02f, x + 0.04f, y + 0.02f, 0.008f), shell), fibre, 0f, 0f);
            }
            // Les trois « yeux » de la noix.
            c.Fill(Circle(0.41f, 0.4f, 0.042f), Dark, 0f, 0f);
            c.Fill(Circle(0.59f, 0.4f, 0.042f), Dark, 0f, 0f);
            c.Fill(Circle(0.5f, 0.53f, 0.042f), Dark, 0f, 0f);
            c.Fill(Intersect(Ring(0.5f, 0.53f, 0.27f, 0.025f), Circle(0.3f, 0.3f, 0.2f)), new Color(1f, 0.85f, 0.65f, 0.6f), 0f, 0f);
        }

        static void Honey(IconCanvas c)
        {
            var clay = new Color(0.86f, 0.5f, 0.22f);
            var honey = new Color(1f, 0.76f, 0.1f);
            c.Fill(Ellipse(0.5f, 0.62f, 0.32f, 0.27f), clay);
            c.Fill(Polygon(0.03f, 0.31f, 0.27f, 0.69f, 0.27f, 0.66f, 0.4f, 0.34f, 0.4f), clay);
            c.Fill(Capsule(0.27f, 0.25f, 0.73f, 0.25f, 0.06f), new Color(0.72f, 0.38f, 0.16f));
            // Miel qui déborde du col et coule.
            c.Fill(Union(Ellipse(0.5f, 0.22f, 0.2f, 0.07f), Capsule(0.36f, 0.24f, 0.36f, 0.4f, 0.04f),
                Capsule(0.6f, 0.24f, 0.6f, 0.48f, 0.045f), Capsule(0.69f, 0.24f, 0.69f, 0.33f, 0.03f)), honey);
            // Étiquette avec une alvéole.
            c.Fill(Ellipse(0.5f, 0.65f, 0.16f, 0.12f), new Color(1f, 0.95f, 0.8f), IconCanvas.Line * 0.7f, 0f);
            c.Fill(Polygon(0f, 0.5f, 0.57f, 0.565f, 0.61f, 0.565f, 0.69f, 0.5f, 0.73f, 0.435f, 0.69f, 0.435f, 0.61f), honey,
                IconCanvas.Line * 0.6f, 0f);
            c.Fill(Capsule(0.3f, 0.55f, 0.33f, 0.72f, 0.022f), new Color(1f, 0.9f, 0.75f, 0.55f), 0f, 0f);
        }

        static void Feather(IconCanvas c)
        {
            // Plume blanche à bout turquoise, en diagonale, avec deux encoches dans les barbes.
            var vane = Ellipse(0.52f, 0.46f, 0.15f, 0.36f, 40f);
            c.Fill(vane, new Color(0.97f, 0.97f, 1f));
            c.Fill(Intersect(Ellipse(0.66f, 0.3f, 0.12f, 0.19f, 40f), vane), new Color(0.35f, 0.85f, 0.85f), 0f, 0.1f);
            var notch = new Color(0.82f, 0.86f, 0.95f);
            c.Fill(Intersect(Capsule(0.3f, 0.5f, 0.46f, 0.56f, 0.012f), vane), notch, 0f, 0f);
            c.Fill(Intersect(Capsule(0.55f, 0.62f, 0.62f, 0.5f, 0.012f), vane), notch, 0f, 0f);
            // Tige.
            c.Fill(Capsule(0.27f, 0.8f, 0.74f, 0.17f, 0.018f), new Color(0.75f, 0.6f, 0.45f), IconCanvas.Line * 0.6f, 0f);
            c.Fill(Capsule(0.36f, 0.32f, 0.42f, 0.24f, 0.02f), new Color(1f, 1f, 1f, 0.7f), 0f, 0f);
        }

        static void Yarn(IconCanvas c)
        {
            // Pelote rose avec ses fils enroulés et un brin qui s'échappe.
            var ball = Circle(0.47f, 0.52f, 0.31f);
            c.Fill(Capsule(0.7f, 0.7f, 0.86f, 0.84f, 0.022f), new Color(0.95f, 0.42f, 0.62f), IconCanvas.Line * 0.6f, 0f);
            c.Fill(ball, new Color(0.95f, 0.42f, 0.62f));
            var thread = new Color(1f, 0.72f, 0.82f);
            c.Fill(Intersect(Ring(0.3f, 0.75f, 0.36f, 0.018f), ball), thread, 0f, 0f);
            c.Fill(Intersect(Ring(0.3f, 0.75f, 0.27f, 0.018f), ball), thread, 0f, 0f);
            c.Fill(Intersect(Ring(0.75f, 0.35f, 0.3f, 0.018f), ball), thread, 0f, 0f);
            c.Fill(Intersect(Ring(0.75f, 0.35f, 0.21f, 0.018f), ball), thread, 0f, 0f);
            c.Fill(Intersect(Ring(0.47f, 0.52f, 0.24f, 0.025f), Circle(0.28f, 0.3f, 0.2f)), new Color(1f, 1f, 1f, 0.55f), 0f, 0f);
        }

        static void Comet(IconCanvas c)
        {
            // Étoile dorée qui file vers le haut à droite, traînée de trois flammèches vermillon et ocre.
            c.Fill(Capsule(0.18f, 0.84f, 0.5f, 0.52f, 0.07f), new Color(0.85f, 0.3f, 0.25f, 0.85f), 0f, 0.4f);
            c.Fill(Capsule(0.12f, 0.66f, 0.46f, 0.46f, 0.045f), new Color(0.91f, 0.73f, 0.29f, 0.85f), 0f, 0.4f);
            c.Fill(Capsule(0.36f, 0.9f, 0.56f, 0.58f, 0.045f), new Color(0.91f, 0.73f, 0.29f, 0.85f), 0f, 0.4f);
            var star = new float[20];
            for (int i = 0; i < 10; i++)
            {
                float a = -Mathf.PI * 0.5f + i * Mathf.PI / 5f, r = i % 2 == 0 ? 0.26f : 0.115f;
                star[2 * i] = 0.62f + Mathf.Cos(a) * r;
                star[2 * i + 1] = 0.4f + Mathf.Sin(a) * r;
            }
            c.Fill(Polygon(0.015f, star), new Color(1f, 0.85f, 0.25f));
            c.Fill(Circle(0.56f, 0.34f, 0.05f), new Color(1f, 1f, 1f, 0.75f), 0f, 0f);
        }

        static void Shield(IconCanvas c)
        {
            var bubble = new Color(0.45f, 0.85f, 1f, 0.55f);
            c.Fill(Circle(0.5f, 0.5f, 0.38f), bubble, IconCanvas.Line, 0f);
            c.Fill(Ring(0.5f, 0.5f, 0.345f, 0.02f), new Color(0.75f, 0.95f, 1f, 0.9f), 0f, 0f);
            c.Fill(Intersect(Ring(0.5f, 0.5f, 0.27f, 0.035f), Circle(0.28f, 0.28f, 0.24f)), new Color(1f, 1f, 1f, 0.9f), 0f, 0f);
            c.Fill(Circle(0.66f, 0.3f, 0.035f), new Color(1f, 1f, 1f, 0.9f), 0f, 0f);
            // Étoile au centre.
            var star = new float[20];
            for (int i = 0; i < 10; i++)
            {
                float a = -Mathf.PI * 0.5f + i * Mathf.PI / 5f, r = i % 2 == 0 ? 0.19f : 0.085f;
                star[2 * i] = 0.5f + Mathf.Cos(a) * r;
                star[2 * i + 1] = 0.53f + Mathf.Sin(a) * r;
            }
            c.Fill(Polygon(0.01f, star), new Color(1f, 0.88f, 0.25f), IconCanvas.Line * 0.8f);
        }

        // ---------- Portraits ----------

        /// <summary>
        /// Portrait d'un pilote humain : casque intégral de trois quarts à la couleur de l'équipe, bande de la couleur
        /// secondaire, visière fumée avec un reflet, épaules de la combinaison et numéro de course sur le col.
        /// </summary>
        static void DrawPortrait(IconCanvas c, DriverSpec s)
        {
            var helmet = s.Kart;
            var stripe = s.KartTrim;
            var visor = new Color(0.07f, 0.08f, 0.1f);
            // Épaules et col de la combinaison.
            c.Fill(Ellipse(0.5f, 0.98f, 0.4f, 0.2f), s.Suit);
            c.Fill(Intersect(Ellipse(0.5f, 0.98f, 0.4f, 0.2f), Capsule(0.5f, 0.78f, 0.5f, 1.1f, 0.035f)), stripe, 0f, 0f);
            c.Fill(Ellipse(0.5f, 0.82f, 0.13f, 0.05f), Dark, IconCanvas.Line * 0.6f, 0f);
            // Coque du casque, légèrement allongée vers l'avant (à droite).
            var shell = Union(Ellipse(0.5f, 0.47f, 0.3f, 0.29f), Ellipse(0.56f, 0.6f, 0.25f, 0.2f, -10f));
            c.Fill(shell, helmet, IconCanvas.Line, 0.25f);
            // Bande d'avant en arrière, suivant la courbure de la coque.
            c.Fill(Intersect(Ring(0.66f, 0.62f, 0.4f, 0.04f), shell), stripe, 0f, 0.1f);
            // Visière : ouverture sombre sur l'avant, reflet clair en biais.
            var shield = Polygon(0.04f, 0.42f, 0.42f, 0.84f, 0.4f, 0.86f, 0.56f, 0.46f, 0.6f);
            c.Fill(Intersect(shield, shell), visor, IconCanvas.Line * 0.7f, 0f);
            c.Fill(Intersect(Capsule(0.55f, 0.47f, 0.72f, 0.45f, 0.018f), shield), new Color(1f, 1f, 1f, 0.45f), 0f, 0f);
            // Charnière de visière et aération du menton.
            c.Fill(Circle(0.38f, 0.5f, 0.035f), Dark, IconCanvas.Line * 0.5f, 0f);
            c.Fill(Capsule(0.6f, 0.7f, 0.72f, 0.69f, 0.012f), Dark, 0f, 0f);
            c.Fill(Capsule(0.6f, 0.74f, 0.7f, 0.735f, 0.012f), Dark, 0f, 0f);
        }
    }
}
