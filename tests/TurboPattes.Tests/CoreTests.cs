using NUnit.Framework;
using TurboPattes.Core;

namespace TurboPattes.Tests
{
    public class LapTrackerTests
    {
        [Test]
        public void DepartDerriereLaLigne_CommenceAuTourMoinsUn()
        {
            var t = new LapTracker(1000f, 990f);
            Assert.That(t.Lap, Is.EqualTo(-1));
            Assert.That(t.Progress, Is.EqualTo(-10f).Within(1e-3));
            Assert.That(t.DisplayLap(3), Is.EqualTo(1));
        }

        [Test]
        public void FranchirLaLigne_AjouteUnTour()
        {
            var t = new LapTracker(1000f, 990f);
            t.Update(999f);
            t.Update(2f);
            Assert.That(t.Lap, Is.EqualTo(0));
            Assert.That(t.Progress, Is.EqualTo(2f).Within(1e-3));
        }

        [Test]
        public void ReculerAtraversLaLigne_RetireUnTour()
        {
            var t = new LapTracker(1000f, 5f);
            t.Update(995f);
            Assert.That(t.Lap, Is.EqualTo(-1));
        }

        [Test]
        public void TroisPassagesApresLeDepart_Termine()
        {
            var t = new LapTracker(1000f, 990f);
            for (int lap = 0; lap < 4; lap++)
                for (float s = 0f; s < 1000f; s += 50f) t.Update(s);
            Assert.That(t.HasFinished(3), Is.True);
            Assert.That(t.DisplayLap(3), Is.EqualTo(3));
        }

        [Test]
        public void TroisPassagesSeulement_NestPasFini()
        {
            var t = new LapTracker(1000f, 990f);
            for (int lap = 0; lap < 3; lap++)
                for (float s = 0f; s < 1000f; s += 50f) t.Update(s);
            Assert.That(t.HasFinished(3), Is.False);
        }
    }

    public class DriftChargeTests
    {
        [Test]
        public void SansCharge_PasDeTurbo()
        {
            var d = new DriftCharge();
            d.Start(1);
            d.Tick(0.2f, 0f);
            Assert.That(d.Release(), Is.EqualTo(0f));
            Assert.That(d.Active, Is.False);
        }

        [Test]
        public void BraquerVersLInterieur_ChargePlusVite()
        {
            var into = new DriftCharge();
            var outward = new DriftCharge();
            into.Start(1);
            outward.Start(-1);
            for (int i = 0; i < 60; i++) { into.Tick(1 / 60f, 1f); outward.Tick(1 / 60f, -1f); }
            Assert.That(into.Charge, Is.GreaterThan(outward.Charge));
        }

        [TestCase(0.8f, 1)]
        [TestCase(1.7f, 2)]
        [TestCase(2.7f, 3)]
        public void NiveauxSelonLaDuree(float seconds, int level)
        {
            var d = new DriftCharge();
            d.Start(-1);
            for (float t = 0f; t < seconds; t += 0.01f) d.Tick(0.01f, 1f);
            Assert.That(d.Level, Is.EqualTo(level));
            Assert.That(d.Release(), Is.EqualTo(DriftCharge.BoostDurations[level]));
        }

        [Test]
        public void HorsDrift_LacherNeDonneRien()
        {
            var d = new DriftCharge();
            d.Tick(5f, 1f);
            Assert.That(d.Release(), Is.EqualTo(0f));
        }
    }

    public class ItemRollTests
    {
        [Test]
        public void ToujoursUnObjet()
        {
            for (int rank = 0; rank < 8; rank++)
                for (double r = 0; r < 1; r += 0.01)
                    Assert.That(ItemRoll.Pick(rank, 8, r), Is.Not.EqualTo(ItemKind.None));
        }

        [Test]
        public void LeDernierAPlusDeCarottesQueLePremier()
        {
            int first = 0, last = 0;
            for (double r = 0; r < 1; r += 0.001)
            {
                if (ItemRoll.Pick(0, 8, r) == ItemKind.Carrot) first++;
                if (ItemRoll.Pick(7, 8, r) == ItemKind.Carrot) last++;
            }
            Assert.That(last, Is.GreaterThan(first * 3));
        }

        [Test]
        public void ValeursHorsBornes_Tolerees()
        {
            Assert.That(ItemRoll.Pick(0, 1, 1.0), Is.Not.EqualTo(ItemKind.None));
            Assert.That(ItemRoll.Pick(12, 8, -0.5), Is.Not.EqualTo(ItemKind.None));
        }
    }

    public class RaceRankingTests
    {
        [Test]
        public void ArrivesDabordPuisDistance()
        {
            var order = RaceRanking.Order(new[]
            {
                new RacerStanding(false, 0, 500),
                new RacerStanding(true, 92.5f, 3000),
                new RacerStanding(false, 0, 900),
                new RacerStanding(true, 90.1f, 3000),
            });
            Assert.That(order, Is.EqualTo(new[] { 3, 1, 2, 0 }));
        }

        [Test]
        public void EgaliteConserveLOrdre()
        {
            var order = RaceRanking.Order(new[]
            {
                new RacerStanding(false, 0, 10),
                new RacerStanding(false, 0, 10),
            });
            Assert.That(order, Is.EqualTo(new[] { 0, 1 }));
        }
    }
}
