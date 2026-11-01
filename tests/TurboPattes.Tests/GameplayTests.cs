using NUnit.Framework;
using TurboPattes.Core;

namespace TurboPattes.Tests
{
    public class QualityGovernorTests
    {
        [Test]
        public void Baisse_DesQueCaSaccade()
        {
            var g = new QualityGovernor(0, 4);
            Assert.That(g.Feed(40f), Is.EqualTo(1));
            Assert.That(g.Level, Is.EqualTo(1));
            Assert.That(g.Feed(50f), Is.EqualTo(0));
        }

        [Test]
        public void Remonte_ApresTroisFenetresFluides()
        {
            var g = new QualityGovernor(3, 4);
            Assert.That(g.Feed(60f), Is.EqualTo(0));
            Assert.That(g.Feed(60f), Is.EqualTo(0));
            Assert.That(g.Feed(60f), Is.EqualTo(-1));
            Assert.That(g.Level, Is.EqualTo(2));
            // Une fenêtre moyenne remet le compteur à zéro.
            g.Feed(60f);
            g.Feed(50f);
            g.Feed(60f);
            Assert.That(g.Feed(60f), Is.EqualTo(0));
        }

        [Test]
        public void PasDeVaEtVient_VersUnNiveauDejaRate()
        {
            var g = new QualityGovernor(1, 4);
            for (int round = 0; round < 2; round++)
            {
                Assert.That(g.Feed(30f), Is.EqualTo(1), "le niveau 1 saccade");
                g.Feed(60f); g.Feed(60f);
                Assert.That(g.Feed(60f), Is.EqualTo(round == 0 ? -1 : 0), "tour " + round);
            }
            Assert.That(g.Level, Is.EqualTo(2), "le niveau 1 a échoué deux fois : on reste au 2");
            Assert.That(g.Failures(1), Is.EqualTo(2));
        }

        [Test]
        public void Bornes()
        {
            var top = new QualityGovernor(0, 4);
            for (int i = 0; i < 5; i++) top.Feed(60f);
            Assert.That(top.Level, Is.EqualTo(0));
            var bottom = new QualityGovernor(9, 4);
            Assert.That(bottom.Level, Is.EqualTo(4));
            Assert.That(bottom.Feed(10f), Is.EqualTo(0));
        }
    }

    public class DriverStatsTests
    {
        [Test]
        public void Pilotes_EquilibresEtDifferents()
        {
            Assert.That(Drivers.Stats.Length, Is.EqualTo(8));
            var seen = new System.Collections.Generic.HashSet<(int, int, int, int)>();
            foreach (var st in Drivers.Stats)
            {
                Assert.That(st.Sum, Is.EqualTo(DriverStats.Total));
                foreach (int v in new[] { st.Speed, st.Accel, st.Grip, st.Weight }) Assert.That(v, Is.InRange(1, 5));
                Assert.That(seen.Add((st.Speed, st.Accel, st.Grip, st.Weight)), "deux pilotes identiques");
            }
        }

        [Test]
        public void Ecarts_Petits()
        {
            foreach (var st in Drivers.Stats)
            {
                Assert.That(st.TopSpeedFactor, Is.InRange(0.96f, 1.04f));
                Assert.That(st.AccelFactor, Is.InRange(0.8f, 1.2f));
                Assert.That(st.GripFactor, Is.InRange(0.85f, 1.15f));
            }
            Assert.That(Drivers.For(-1).Sum, Is.EqualTo(12));
            Assert.That(Drivers.For(99).Sum, Is.EqualTo(12));
        }

        [Test]
        public void Choc_LePlusLourdEstMoinsBouscule()
        {
            var heavy = new DriverStats(3, 2, 2, 5);
            var light = new DriverStats(2, 4, 4, 2);
            float lightShare = DriverStats.PushShare(light.Mass, heavy.Mass);
            float heavyShare = DriverStats.PushShare(heavy.Mass, light.Mass);
            Assert.That(lightShare, Is.GreaterThan(heavyShare));
            Assert.That(lightShare + heavyShare, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(DriverStats.PushShare(1f, 1f), Is.EqualTo(0.5f));
        }
    }

    public class AirTrickTests
    {
        [Test]
        public void FigureFinie_DonneUnTurbo()
        {
            var t = new AirTrick();
            Assert.That(t.TryStart(true, 0.3f), Is.True);
            for (int i = 0; i < 30; i++) t.Tick(1f / 60f);
            Assert.That(t.Done, Is.True);
            Assert.That(t.Land(), Is.EqualTo(AirTrick.Boost));
            Assert.That(t.Done, Is.False, "remis à zéro");
        }

        [Test]
        public void FigurePasFinie_NeDonneRien()
        {
            var t = new AirTrick();
            t.TryStart(true, 0.3f);
            t.Tick(0.1f);
            Assert.That(t.Land(), Is.EqualTo(0f));
        }

        [Test]
        public void Figure_SeulementEnVraiSaut()
        {
            var t = new AirTrick();
            Assert.That(t.TryStart(false, 1f), Is.False, "au sol");
            Assert.That(t.TryStart(true, 0.1f), Is.False, "saut trop court");
            Assert.That(t.TryStart(true, 0.3f), Is.True);
            Assert.That(t.TryStart(true, 0.4f), Is.False, "déjà en cours");
        }
    }

    public class NewItemsTests
    {
        [Test]
        public void Pelote_JamaisPourLePremierNiSeul()
        {
            for (double r = 0; r < 1; r += 0.001)
            {
                Assert.That(ItemRoll.Pick(0, 8, r), Is.Not.EqualTo(ItemKind.Yarn));
                Assert.That(ItemRoll.Pick(0, 1, r), Is.Not.EqualTo(ItemKind.Yarn));
            }
        }

        [Test]
        public void NouveauxObjets_Tires()
        {
            int feather = 0, yarn = 0;
            for (double r = 0; r < 1; r += 0.001)
            {
                for (int rank = 0; rank < 8; rank++)
                {
                    var k = ItemRoll.Pick(rank, 8, r);
                    if (k == ItemKind.Feather) feather++;
                    if (k == ItemKind.Yarn) yarn++;
                }
            }
            Assert.That(feather, Is.GreaterThan(0));
            Assert.That(yarn, Is.GreaterThan(0));
            Assert.That(ItemRoll.KindCount, Is.EqualTo(6));
        }

        [Test]
        public void Plume_BondPuisReception()
        {
            var v = new VerticalMotion();
            v.Reset(0f);
            v.Launch(9f);
            Assert.That(v.Grounded, Is.False);
            var ev = VerticalEvent.None;
            float peak = 0f;
            int steps = 0;
            while (ev != VerticalEvent.Landed && steps++ < 600)
            {
                ev = v.Step(1f / 60f, 0f, 0f);
                peak = System.Math.Max(peak, v.Y);
            }
            Assert.That(ev, Is.EqualTo(VerticalEvent.Landed));
            Assert.That(peak, Is.EqualTo(81f / (2f * VerticalMotion.Gravity)).Within(0.15f));
            Assert.That(v.AirTime, Is.EqualTo(2f * 9f / VerticalMotion.Gravity).Within(0.05f));
        }
    }
}
