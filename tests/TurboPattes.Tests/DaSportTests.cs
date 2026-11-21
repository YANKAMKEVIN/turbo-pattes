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
