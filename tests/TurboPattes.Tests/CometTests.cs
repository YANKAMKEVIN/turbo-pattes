using NUnit.Framework;
using TurboPattes.Core;

namespace TurboPattes.Tests
{
    public class CometTests
    {
        static int Count(int rank, int racers)
        {
            int n = 0;
            for (double r = 0; r < 1; r += 0.0005)
                if (ItemRoll.Pick(rank, racers, r) == ItemKind.Comet) n++;
            return n;
        }

        [Test]
        public void ReserveeAuDernierQuart()
        {
            for (int rank = 0; rank <= 5; rank++) Assert.That(Count(rank, 8), Is.Zero, $"place {rank + 1}");
            int seventh = Count(6, 8), last = Count(7, 8);
            Assert.That(seventh, Is.GreaterThan(0));
            Assert.That(last, Is.GreaterThan(seventh));
        }

        [Test]
        public void Rare_MoinsDeHuitPourCentPourLeDernier()
        {
            float share = Count(7, 8) / 2000f;
            Assert.That(share, Is.InRange(0.06f, 0.08f));
        }

        [Test]
        public void JamaisDansUnePetiteCourseNiSeul()
        {
            Assert.That(Count(2, 3), Is.Zero);
            Assert.That(Count(0, 1), Is.Zero);
            Assert.That(ItemRoll.CometWeight(3, 4), Is.EqualTo(ItemRoll.CometMaxWeight));
        }

        [Test]
        public void HorsDeLaRoulette()
        {
            for (float t = 0f; t < 2f; t += 0.01f) Assert.That(ItemRoll.Reel(t), Is.Not.EqualTo(ItemKind.Comet));
        }
    }
}
