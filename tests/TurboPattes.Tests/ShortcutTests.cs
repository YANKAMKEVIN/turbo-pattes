using System;
using NUnit.Framework;
using TurboPattes.Core;

namespace TurboPattes.Tests
{
    public class ShortcutTests
    {
        // Comme sur la Prairie : virage à gauche centré à 663 m, tremplin dans le sable 5 m avant le centre.
        static Shortcut Prairie() => new(663f, 35f, -1, 9f, 1133f, 658f, 8f, 1.2f, 10.2f, 15.2f);

        [Test]
        public void LeMuretReculeSeulementDuCoteDuVirage_EtRevientEnDouceur()
        {
            var sc = Prairie();
            Assert.That(sc.BarrierOffset(663f, -1, 12f), Is.EqualTo(21f).Within(1e-4));
            Assert.That(sc.BarrierOffset(663f, 1, 12f), Is.EqualTo(12f));
            Assert.That(sc.BarrierOffset(663f + 35f + Shortcut.Fade + 1f, -1, 12f), Is.EqualTo(12f));
            float prev = sc.Amount(663f + 35f);
            for (float d = 35f; d <= 35f + Shortcut.Fade; d += 0.5f)
            {
                float a = sc.Amount(663f + d);
                Assert.That(a, Is.LessThanOrEqualTo(prev + 1e-5f), $"d={d}");
                Assert.That(sc.Amount(663f - d), Is.EqualTo(a).Within(1e-5), "symétrique");
                prev = a;
            }
        }

        [Test]
        public void ZoneAuBouclageDuTour()
        {
            var sc = new Shortcut(5f, 10f, 1, 6f, 1000f);
            Assert.That(sc.Amount(998f), Is.EqualTo(1f));
            Assert.That(sc.Amount(14f), Is.EqualTo(1f));
            Assert.That(sc.Amount(500f), Is.EqualTo(0f));
        }

        [Test]
        public void SableAuDelaDuBord_CoteInterieurSeulement()
        {
            var sc = Prairie();
            Assert.That(sc.InSand(663f, -12f, 8.2f), Is.True);
            Assert.That(sc.InSand(663f, -6f, 8.2f), Is.False, "sur le bitume");
            Assert.That(sc.InSand(663f, 12f, 8.2f), Is.False, "côté extérieur : de l'herbe");
            Assert.That(sc.InSand(300f, -12f, 8.2f), Is.False, "loin du virage");
        }

        [Test]
        public void TremplinDuSable_DansSaBandeEtAvantSonBord()
        {
            var sc = Prairie();
            Assert.That(sc.Kick(657.9f, -12f, out float slope), Is.EqualTo(1.2f).Within(0.05f));
            Assert.That(slope, Is.GreaterThan(0f));
            Assert.That(sc.Kick(658.1f, -12f, out _), Is.EqualTo(0f), "après le bord : le sol se dérobe");
            Assert.That(sc.Kick(649f, -12f, out _), Is.EqualTo(0f), "avant la rampe");
            Assert.That(sc.Kick(657f, -8f, out _), Is.EqualTo(0f), "hors de la bande");
            Assert.That(sc.Kick(657f, 12f, out _), Is.EqualTo(0f), "autre côté");
            // Pente cohérente avec la hauteur.
            float h0 = sc.Kick(654f, -12f, out float k), h1 = sc.Kick(654.02f, -12f, out _);
            Assert.That((h1 - h0) / 0.02f, Is.EqualTo(k).Within(0.02f));
        }

        [Test]
        public void RaccourciRisque_PerdantSansTurbo_GagnantAvecTurbo()
        {
            // Virage de 34 m de rayon : corde à 6 m de l'axe, coupe à 15 m dans le sable.
            const float r = 34f, line = 6f, cut = 15f;
            // Même le pilote le plus adhérent (+0,06 hors piste) perd du temps dans le sable sans turbo.
            Assert.That(Shortcut.TimeRatio(r, line, cut, Shortcut.SandFactor + 0.06f), Is.GreaterThan(1.1f));
            Assert.That(Shortcut.TimeRatio(r, line, cut, 1f), Is.LessThan(0.75f));
        }

        [Test]
        public void ParametresInvalidesRefuses()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Shortcut(0f, 10f, 0, 5f, 100f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Shortcut(0f, 10f, 1, 0f, 100f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Shortcut.TimeRatio(10f, 2f, 12f, 1f));
        }
    }
}
