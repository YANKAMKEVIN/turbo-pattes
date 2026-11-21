using System;
using System.Collections.Generic;
using TurboPattes.Core;
using TurboPattes.Game.Kart;
using UnityEngine;
using static TurboPattes.Game.Visuals.Sdf;

namespace TurboPattes.Game.Visuals
{
    /// <summary>
    /// Icônes d'objets, portraits des pilotes, emblème et plaques de numéro, dessinés par code au premier usage (fond transparent).
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
                case ItemKind.Carrot: Nitro(c); break;
                case ItemKind.Coconut: Tire(c); break;
                case ItemKind.Honey: Oil(c); break;
                case ItemKind.Feather: Aileron(c); break;
                case ItemKind.Yarn: Drone(c); break;
                case ItemKind.Comet: Slipstream(c); break;
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

        // ---------- Objets (habillage sport mécanique, docs/design/da-arcade-sportif.md § 7) ----------

        static readonly Color Metal = new(0.72f, 0.75f, 0.8f);

        /// <summary>Nitro (ex-carotte) : bouteille bleu télémétrie penchée, bande blanche, robinet rouge, éclair jaune.</summary>
        static void Nitro(IconCanvas c)
        {
            var body = Capsule(0.36f, 0.78f, 0.62f, 0.3f, 0.15f);
            c.Fill(body, UiColor(Palette.Telemetry), IconCanvas.Line, 0.25f);
            c.Fill(Intersect(Capsule(0.43f, 0.68f, 0.53f, 0.5f, 0.2f), body), UiColor(Palette.Chalk), 0f, 0f);
            // Éclair sur la bande.
            c.Fill(Polygon(0.005f, 0.5f, 0.52f, 0.43f, 0.62f, 0.48f, 0.62f, 0.45f, 0.69f, 0.53f, 0.58f, 0.48f, 0.58f), UiColor(Palette.Signal), IconCanvas.Line * 0.5f, 0f);
            // Col, robinet et volant de vanne.
            c.Fill(Capsule(0.66f, 0.22f, 0.71f, 0.13f, 0.05f), Metal, IconCanvas.Line * 0.8f);
            c.Fill(Circle(0.75f, 0.1f, 0.06f), UiColor(Palette.Red), IconCanvas.Line * 0.8f);
            c.Fill(Capsule(0.34f, 0.66f, 0.5f, 0.36f, 0.018f), new Color(1f, 1f, 1f, 0.5f), 0f, 0f);
        }

        /// <summary>Pneu fou (ex-noix de coco) : pneu de face, sculptures sur la bande de roulement, jante métal.</summary>
        static void Tire(IconCanvas c)
        {
            var rubber = Circle(0.5f, 0.52f, 0.36f);
            c.Fill(rubber, new Color(0.12f, 0.12f, 0.13f), IconCanvas.Line, 0.3f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f, x = 0.5f + Mathf.Cos(a) * 0.33f, y = 0.52f + Mathf.Sin(a) * 0.33f;
                c.Fill(Intersect(Circle(x, y, 0.035f), rubber), new Color(0.04f, 0.04f, 0.05f), 0f, 0f);
            }
            c.Fill(Circle(0.5f, 0.52f, 0.19f), Metal, IconCanvas.Line * 0.8f, 0.3f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 0.4f - Mathf.PI * 0.5f;
                c.Fill(Capsule(0.5f, 0.52f, 0.5f + Mathf.Cos(a) * 0.15f, 0.52f + Mathf.Sin(a) * 0.15f, 0.018f), new Color(0.35f, 0.37f, 0.4f), 0f, 0f);
            }
            c.Fill(Circle(0.5f, 0.52f, 0.05f), Dark, 0f, 0f);
            c.Fill(Intersect(Ring(0.5f, 0.52f, 0.29f, 0.02f), Circle(0.3f, 0.3f, 0.2f)), new Color(1f, 1f, 1f, 0.35f), 0f, 0f);
        }

        /// <summary>Flaque d'huile (ex-pot de miel) : bidon rouge renversé et sa flaque noire luisante.</summary>
        static void Oil(IconCanvas c)
        {
            c.Fill(Union(Ellipse(0.5f, 0.76f, 0.38f, 0.13f), Ellipse(0.3f, 0.8f, 0.14f, 0.07f)), new Color(0.06f, 0.06f, 0.08f), IconCanvas.Line * 0.8f, 0f);
            c.Fill(Ellipse(0.42f, 0.74f, 0.1f, 0.025f), new Color(0.55f, 0.6f, 0.75f, 0.6f), 0f, 0f);
            // Bidon penché à droite, bec verseur vers la flaque.
            var can = Polygon(0.03f, 0.48f, 0.22f, 0.8f, 0.3f, 0.74f, 0.62f, 0.42f, 0.54f);
            c.Fill(can, UiColor(Palette.Red), IconCanvas.Line, 0.25f);
            c.Fill(Intersect(Capsule(0.46f, 0.38f, 0.78f, 0.46f, 0.045f), can), UiColor(Palette.Chalk), 0f, 0f);
            c.Fill(Capsule(0.46f, 0.3f, 0.32f, 0.52f, 0.03f), Metal, IconCanvas.Line * 0.7f);
            c.Fill(Capsule(0.6f, 0.2f, 0.72f, 0.24f, 0.03f), Dark, IconCanvas.Line * 0.5f, 0f);
            c.Fill(Capsule(0.31f, 0.55f, 0.3f, 0.66f, 0.022f), new Color(0.06f, 0.06f, 0.08f), 0f, 0f);
        }

        /// <summary>Aileron (ex-plume planante) : profil d'aile blanc piste à dérives rouges, filets d'air.</summary>
        static void Aileron(IconCanvas c)
        {
            var air = new Color(1f, 1f, 1f, 0.75f);
            c.Fill(Capsule(0.08f, 0.66f, 0.3f, 0.66f, 0.018f), air, 0f, 0f);
            c.Fill(Capsule(0.12f, 0.76f, 0.36f, 0.76f, 0.018f), air, 0f, 0f);
            // Plan d'aile en biais.
            c.Fill(Polygon(0.03f, 0.2f, 0.5f, 0.82f, 0.34f, 0.86f, 0.42f, 0.24f, 0.6f), UiColor(Palette.Chalk), IconCanvas.Line, 0.3f);
            c.Fill(Polygon(0.02f, 0.22f, 0.56f, 0.84f, 0.39f, 0.86f, 0.42f, 0.24f, 0.6f), UiColor(Palette.Steel), 0f, 0f);
            // Dérives et mâts.
            c.Fill(Polygon(0.02f, 0.16f, 0.4f, 0.26f, 0.38f, 0.28f, 0.68f, 0.18f, 0.7f), UiColor(Palette.Red), IconCanvas.Line * 0.8f);
            c.Fill(Polygon(0.02f, 0.78f, 0.24f, 0.88f, 0.22f, 0.9f, 0.52f, 0.8f, 0.54f), UiColor(Palette.Red), IconCanvas.Line * 0.8f);
            c.Fill(Capsule(0.42f, 0.56f, 0.42f, 0.84f, 0.02f), Dark, IconCanvas.Line * 0.5f, 0f);
            c.Fill(Capsule(0.62f, 0.5f, 0.62f, 0.8f, 0.02f), Dark, IconCanvas.Line * 0.5f, 0f);
        }

        /// <summary>Drone traqueur (ex-pelote de laine) : quadrirotor carbone vu de dessus, œil rouge.</summary>
        static void Drone(IconCanvas c)
        {
            var rotor = new Color(0.75f, 0.8f, 0.88f, 0.55f);
            float[] xs = { 0.24f, 0.76f, 0.24f, 0.76f }, ys = { 0.26f, 0.26f, 0.78f, 0.78f };
            for (int i = 0; i < 4; i++) c.Fill(Capsule(0.5f, 0.52f, xs[i], ys[i], 0.035f), Dark, IconCanvas.Line * 0.6f, 0f);
            for (int i = 0; i < 4; i++)
            {
                c.Fill(Circle(xs[i], ys[i], 0.15f), rotor, IconCanvas.Line * 0.6f, 0f);
                c.Fill(Circle(xs[i], ys[i], 0.04f), Metal, IconCanvas.Line * 0.4f, 0f);
            }
            c.Fill(Ellipse(0.5f, 0.52f, 0.17f, 0.13f), new Color(0.16f, 0.17f, 0.2f), IconCanvas.Line, 0.3f);
            c.Fill(Capsule(0.42f, 0.5f, 0.58f, 0.5f, 0.02f), UiColor(Palette.Signal), 0f, 0f);
            c.Fill(Circle(0.5f, 0.6f, 0.045f), UiColor(Palette.Red), IconCanvas.Line * 0.5f, 0f);
        }

        /// <summary>Aspiration (ex-comète, objet rare) : trois chevrons jaune puis rouge qui filent, sillage d'air.</summary>
        static void Slipstream(IconCanvas c)
        {
            var air = new Color(1f, 1f, 1f, 0.7f);
            c.Fill(Capsule(0.06f, 0.36f, 0.3f, 0.36f, 0.02f), air, 0f, 0f);
            c.Fill(Capsule(0.04f, 0.52f, 0.26f, 0.52f, 0.02f), air, 0f, 0f);
            c.Fill(Capsule(0.06f, 0.68f, 0.3f, 0.68f, 0.02f), air, 0f, 0f);
            Color[] cols = { UiColor(Palette.Signal), UiColor(Palette.Signal), UiColor(Palette.Red) };
            for (int i = 0; i < 3; i++)
            {
                float x = 0.26f + i * 0.17f;
                c.Fill(Polygon(0.012f, x, 0.22f, x + 0.12f, 0.22f, x + 0.3f, 0.52f, x + 0.12f, 0.82f, x, 0.82f, x + 0.18f, 0.52f), cols[i], IconCanvas.Line * 0.8f, 0.2f);
            }
        }

        /// <summary>Bouclier carbone (ex-bulle) : écu graphite cerclé d'acier, chevron blanc au centre.</summary>
        static void Shield(IconCanvas c)
        {
            var shield = Polygon(0.04f, 0.2f, 0.18f, 0.8f, 0.18f, 0.78f, 0.56f, 0.5f, 0.86f, 0.22f, 0.56f);
            c.Fill(shield, UiColor(Palette.Steel), IconCanvas.Line, 0.25f);
            var inner = Polygon(0.03f, 0.27f, 0.25f, 0.73f, 0.25f, 0.715f, 0.535f, 0.5f, 0.77f, 0.285f, 0.535f);
            c.Fill(inner, UiColor(Palette.Graphite), 0f, 0.2f);
            // Trame carbone : fines diagonales.
            for (int i = 0; i < 7; i++)
            {
                float x = 0.16f + i * 0.1f;
                c.Fill(Intersect(Capsule(x, 0.25f, x + 0.25f, 0.8f, 0.008f), inner), new Color(0.24f, 0.26f, 0.3f), 0f, 0f);
            }
            c.Fill(Polygon(0.01f, 0.36f, 0.38f, 0.5f, 0.5f, 0.64f, 0.38f, 0.64f, 0.48f, 0.5f, 0.6f, 0.36f, 0.48f), UiColor(Palette.Chalk), 0f, 0f);
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
