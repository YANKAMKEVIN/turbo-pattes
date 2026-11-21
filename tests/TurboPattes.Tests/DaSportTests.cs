using System;
using System.Linq;
using NUnit.Framework;
using TurboPattes.Core;

namespace TurboPattes.Tests
{
    /// <summary>Données de la direction « arcade sportif » (docs/design/da-arcade-sportif.md).</summary>
    public class RosterTests
    {
        [Test]
        public void HuitPilotesHumains_AlignesSurLesCaracteresDeJeu()
        {
            Assert.That(Roster.All.Length, Is.EqualTo(8));
            Assert.That(Roster.All.Length, Is.EqualTo(Drivers.Stats.Length));
        }

        [Test]
        public void Pilotes_NomsCodesNumerosEtCasquesTousDifferents()
        {
            Assert.That(Roster.All.Select(p => p.FirstName).Distinct().Count(), Is.EqualTo(8), "prénoms");
            Assert.That(Roster.All.Select(p => p.FullName).Distinct().Count(), Is.EqualTo(8), "noms complets");
            Assert.That(Roster.All.Select(p => p.Code).Distinct().Count(), Is.EqualTo(8), "codes");
            Assert.That(Roster.All.Select(p => p.Number).Distinct().Count(), Is.EqualTo(8), "numéros");
            for (int i = 0; i < 8; i++)
            for (int j = i + 1; j < 8; j++)
            {
                Rgb a = Roster.All[i].Primary, b = Roster.All[j].Primary;
                float d = (float)Math.Sqrt((a.R - b.R) * (a.R - b.R) + (a.G - b.G) * (a.G - b.G) + (a.B - b.B) * (a.B - b.B));
                Assert.That(d, Is.GreaterThan(0.2f), $"casques de {Roster.All[i].FirstName} et {Roster.All[j].FirstName} trop proches");
            }
        }

        [Test]
        public void Pilotes_DonneesCompletesEtLisibles()
        {
            foreach (var p in Roster.All)
            {
                Assert.That(p.FirstName, Is.Not.Empty);
                Assert.That(p.LastName, Is.Not.Empty);
                Assert.That(p.Trait, Is.Not.Empty);
                Assert.That(p.Code, Does.Match("^[A-Z]{3}$"));
                Assert.That(p.Number, Is.InRange(1, 99));
                // Prénom court : il tient dans la colonne du classement et sur la carte du choix du pilote.
                Assert.That(p.FirstName.Length, Is.LessThanOrEqualTo(7));
                // La bande du casque se détache de la coque.
                Assert.That(Rgb.Contrast(p.Primary, p.Secondary), Is.GreaterThan(1.8f), p.FirstName);
            }
        }

        [Test]
        public void Pilotes_PlusAucunAnimalNiAncienNom()
        {
            string[] old = { "Roxy", "Bambou", "Flip", "Lili", "Bruno", "Mimi", "Pingo", "Rosa" };
            foreach (var p in Roster.All)
                Assert.That(old, Does.Not.Contain(p.FirstName));
        }

        [Test]
        public void CaracteresDeJeu_InchangesParLaRefonte()
        {
            // Même valeurs, même ordre qu'avant la refonte (seule l'apparence change).
            int[,] expected = { { 3, 3, 3, 3 }, { 3, 2, 2, 5 }, { 2, 4, 4, 2 }, { 2, 5, 3, 2 }, { 4, 1, 2, 5 }, { 3, 3, 4, 2 }, { 4, 3, 2, 3 }, { 3, 2, 3, 4 } };
            for (int i = 0; i < 8; i++)
            {
                var st = Drivers.Stats[i];
                Assert.That(new[] { st.Speed, st.Accel, st.Grip, st.Weight },
                    Is.EqualTo(new[] { expected[i, 0], expected[i, 1], expected[i, 2], expected[i, 3] }), Roster.All[i].FirstName);
            }
        }

        [Test]
        public void For_HorsBornesRendLePilotePrincipal()
        {
            Assert.That(Roster.For(-1), Is.SameAs(Roster.All[0]));
            Assert.That(Roster.For(42), Is.SameAs(Roster.All[0]));
            Assert.That(Roster.For(3), Is.SameAs(Roster.All[3]));
        }
    }
}

namespace TurboPattes.Tests
{
    public class NumberPlateTests
    {
        [TestCase('0', 6)]
        [TestCase('1', 2)]
        [TestCase('4', 4)]
        [TestCase('7', 3)]
        [TestCase('8', 7)]
        public void SeptSegments_NombreDeSegmentsAllumes(char digit, int lit)
        {
            Assert.That(System.Linq.Enumerable.Count(SevenSegment.Segments(digit), on => on), Is.EqualTo(lit));
        }

        [Test]
        public void SeptSegments_ChiffresTousDifferentsEtNumerosDesPilotesDessinables()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            for (char c = '0'; c <= '9'; c++) Assert.That(seen.Add(string.Join(",", SevenSegment.Segments(c))), Is.True, c.ToString());
            Assert.That(SevenSegment.Segments('x'), Has.All.False);
            foreach (var p in Roster.All)
                foreach (char c in p.Number.ToString())
                    Assert.That(SevenSegment.Segments(c), Has.Some.True);
        }

        [Test]
        public void SeptSegments_TraitsDansLaCase()
        {
            for (int i = 0; i < 7; i++)
            {
                var (x0, y0, x1, y1) = SevenSegment.Stroke(i);
                foreach (float x in new[] { x0, x1 }) Assert.That(x, Is.InRange(0f, 1f));
                foreach (float y in new[] { y0, y1 }) Assert.That(y, Is.InRange(0f, 2f));
                Assert.That(x0 == x1 || y0 == y1, Is.True, "segment droit");
            }
        }
    }
}

namespace TurboPattes.Tests
{
    public class ItemCatalogTests
    {
        static readonly ItemKind[] Kinds = { ItemKind.Carrot, ItemKind.Coconut, ItemKind.Honey, ItemKind.Shield, ItemKind.Feather, ItemKind.Yarn, ItemKind.Comet };

        [Test]
        public void ChaqueObjet_UnNomUniqueEtUneDescription()
        {
            Assert.That(Kinds.Length, Is.EqualTo(Enum.GetValues(typeof(ItemKind)).Length - 1), "tous les objets sauf None");
            foreach (var k in Kinds)
            {
                Assert.That(ItemCatalog.Name(k), Is.Not.Empty, k.ToString());
                Assert.That(ItemCatalog.Description(k), Is.Not.Empty, k.ToString());
            }
            Assert.That(Kinds.Select(ItemCatalog.Name).Distinct().Count(), Is.EqualTo(Kinds.Length));
            Assert.That(ItemCatalog.Name(ItemKind.None), Is.Empty);
        }

        [Test]
        public void ChaqueObjet_GardeSaMecanique()
        {
            // Avant la refonte : carotte = turbo, coco = projectile qui rebondit, miel = piège posé, bulle = bouclier,
            // plume = bond plané, pelote = projectile guidé par la piste, comète = objet rare en pilote automatique.
            Assert.That(ItemCatalog.Mechanic(ItemKind.Carrot), Is.EqualTo(ItemMechanic.InstantBoost));
            Assert.That(ItemCatalog.Mechanic(ItemKind.Coconut), Is.EqualTo(ItemMechanic.StraightBouncer));
            Assert.That(ItemCatalog.Mechanic(ItemKind.Honey), Is.EqualTo(ItemMechanic.DroppedTrap));
            Assert.That(ItemCatalog.Mechanic(ItemKind.Shield), Is.EqualTo(ItemMechanic.TimedShield));
            Assert.That(ItemCatalog.Mechanic(ItemKind.Feather), Is.EqualTo(ItemMechanic.GlideLeap));
            Assert.That(ItemCatalog.Mechanic(ItemKind.Yarn), Is.EqualTo(ItemMechanic.TrackHoming));
            Assert.That(ItemCatalog.Mechanic(ItemKind.Comet), Is.EqualTo(ItemMechanic.RareAutopilot));
            Assert.That(Kinds.Select(ItemCatalog.Mechanic).Distinct().Count(), Is.EqualTo(Kinds.Length), "une mécanique par objet");
        }

        [Test]
        public void Tirage_InchangeParLaRefonte()
        {
            // Mêmes réglages qu'avant : la comète (aspiration) reste réservée au dernier quart, hors roulette.
            Assert.That(ItemRoll.KindCount, Is.EqualTo(6));
            Assert.That(CometRun.Duration, Is.EqualTo(3.5f));
            Assert.That(ItemRoll.CometWeight(7, 8), Is.EqualTo(ItemRoll.CometMaxWeight));
            Assert.That(ItemRoll.CometWeight(0, 8), Is.EqualTo(0f));
            Assert.That(ItemRoll.Pick(0, 8, 0.0), Is.EqualTo(ItemKind.Carrot));
            Assert.That(ItemRoll.Pick(0, 8, 0.999), Is.EqualTo(ItemKind.Feather), "le premier ne tire jamais le drone");
        }

        [Test]
        public void Noms_PlusAucunObjetEnfantin()
        {
            string[] old = { "carotte", "coco", "miel", "bulle", "plume", "pelote", "comète" };
            foreach (var k in Kinds)
                foreach (var o in old)
                    Assert.That(ItemCatalog.Name(k).ToLowerInvariant(), Does.Not.Contain(o));
        }
    }
}
