using System;
using NUnit.Framework;
using TurboPattes.Core;

namespace TurboPattes.Tests
{
    public class BankingTests
    {
        // Comme sur la Route des Nuages : virage à gauche centré à 793 m, relevé de 12°.
        static Banking Sky() => new(793f, 55f, 12f, 1329f);

        [Test]
        public void ExterieurReleve_InterieurAbaisse_MedianeImmobile()
        {
            var b = Sky();
            float tan = (float)Math.Tan(12.0 * Math.PI / 180.0);
            Assert.That(b.Lift(793f, 7f), Is.EqualTo(7f * tan).Within(1e-4));
            Assert.That(b.Lift(793f, -7f), Is.EqualTo(-7f * tan).Within(1e-4));
            Assert.That(b.Lift(793f, 0f), Is.EqualTo(0f));
            Assert.That(b.Lift(500f, 7f), Is.EqualTo(0f), "hors du virage : à plat");
        }

        [Test]
        public void EntreeEtSortieDouces_PenteCoherente()
        {
            var b = Sky();
            float prev = 0f;
            for (float s = 793f - 55f - Banking.Fade; s <= 793f - 55f; s += 0.5f)
            {
                float a = b.Angle(s);
                Assert.That(a, Is.GreaterThanOrEqualTo(prev - 1e-5f));
                prev = a;
            }
            Assert.That(prev, Is.EqualTo(12f).Within(1e-4));
            // La pente le long de la piste (au bord extérieur) colle à la dérivée numérique et reste douce.
            for (float s = 700f; s < 890f; s += 1f)
            {
                float slope = b.LiftSlope(s, 12f);
                Assert.That(slope, Is.EqualTo((b.Lift(s + 0.01f, 12f) - b.Lift(s - 0.01f, 12f)) / 0.02f).Within(0.01f));
                Assert.That(Math.Abs(slope), Is.LessThan(0.2f), $"s={s}");
            }
        }

        [Test]
        public void ReleveAuBouclageDuTour()
        {
            var b = new Banking(10f, 20f, -8f, 1000f);
            Assert.That(b.Angle(995f), Is.EqualTo(-8f));
            Assert.That(b.Lift(995f, 5f), Is.LessThan(0f), "angle négatif : côté droit abaissé");
        }

        [Test]
        public void MoinsDeGlisse_EtPassagePlusRapide()
        {
            Assert.That(Banking.SlideFactor(0f), Is.EqualTo(1f));
            Assert.That(Banking.SlideFactor(15f), Is.EqualTo(0.5f).Within(1e-5));
            Assert.That(Banking.SlideFactor(-15f), Is.EqualTo(0.5f).Within(1e-5));
            Assert.That(Banking.CornerSpeedRatio(0f), Is.EqualTo(1f).Within(1e-5));
            float r12 = Banking.CornerSpeedRatio(12f);
            Assert.That(r12, Is.InRange(1.15f, 1.3f));
            Assert.That(Banking.CornerSpeedRatio(20f), Is.GreaterThan(r12));
        }

        [Test]
        public void AngleTropFortRefuse() => Assert.Throws<ArgumentOutOfRangeException>(() => new Banking(0f, 10f, 35f, 100f));
    }

    public class DecorWavesTests
    {
        [Test]
        public void LueurDeLaveBorneeEtContinue()
        {
            double sum = 0;
            int n = 0;
            float prev = DecorWaves.LavaGlow(0f);
            for (float t = 0f; t < 23.4f; t += 0.01f, n++)
            {
                float g = DecorWaves.LavaGlow(t);
                Assert.That(g, Is.InRange(0.65f, 1.35f));
                Assert.That(Math.Abs(g - prev), Is.LessThan(0.02f), "pas de saut d'une image à l'autre");
                prev = g;
                sum += g;
            }
            Assert.That(sum / n, Is.EqualTo(1.0).Within(0.02));
        }
    }
}
