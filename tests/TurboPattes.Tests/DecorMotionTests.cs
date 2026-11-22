using System;
using NUnit.Framework;
using TurboPattes.Core;

namespace TurboPattes.Tests
{
    /// <summary>Houle du lagon et nuages qui dérivent (<see cref="DecorWaves"/>).</summary>
    public class DecorMotionTests
    {
        [Test]
        public void Houle_BorneeEtCentreeSurLeNiveauDeRepos()
        {
            double sum = 0;
            int n = 0;
            var rng = new Random(3);
            for (int i = 0; i < 4000; i++, n++)
            {
                float x = (float)(rng.NextDouble() * 600 - 300), z = (float)(rng.NextDouble() * 600 - 300), t = (float)(rng.NextDouble() * 100);
                float h = DecorWaves.WaterHeight(x, z, t);
                Assert.That(Math.Abs(h), Is.LessThanOrEqualTo(DecorWaves.WaterAmplitude + 1e-5f));
                sum += h;
            }
            Assert.That(sum / n, Is.EqualTo(0.0).Within(0.01), "en moyenne, l'eau reste à son niveau");
        }

        [Test]
        public void Houle_PenteEgaleALaDeriveeNumerique_EtDouce()
        {
            const float e = 0.01f;
            for (float x = -90f; x < 90f; x += 7.3f)
            for (float z = -60f; z < 60f; z += 9.1f)
            {
                const float t = 12.7f;
                var (dx, dz) = DecorWaves.WaterSlope(x, z, t);
                float nx = (DecorWaves.WaterHeight(x + e, z, t) - DecorWaves.WaterHeight(x - e, z, t)) / (2f * e);
                float nz = (DecorWaves.WaterHeight(x, z + e, t) - DecorWaves.WaterHeight(x, z - e, t)) / (2f * e);
                Assert.That(dx, Is.EqualTo(nx).Within(2e-3f));
                Assert.That(dz, Is.EqualTo(nz).Within(2e-3f));
                Assert.That(Math.Sqrt(dx * dx + dz * dz), Is.LessThan(0.03), "une houle, pas des vagues déferlantes");
            }
        }

        [Test]
        public void Houle_BougeDoucementDUneImageAlAutre()
        {
            // Mise à jour toutes les deux images à 30 i/s : 1/15 s entre deux positions.
            for (float t = 0f; t < 20f; t += 1f / 15f)
            {
                float a = DecorWaves.WaterHeight(10f, 20f, t), b = DecorWaves.WaterHeight(10f, 20f, t + 1f / 15f);
                Assert.That(Math.Abs(b - a), Is.LessThan(0.02f));
            }
        }

        [Test]
        public void Houle_PasDeCrenelageSurLaMaille()
        {
            // Une crête par maille au pire serait du bruit : sur un pas de maille, la hauteur varie peu.
            for (float x = 0f; x < 300f; x += DecorWaves.WaterCell)
            {
                float a = DecorWaves.WaterHeight(x, 0f, 3f), b = DecorWaves.WaterHeight(x + DecorWaves.WaterCell, 0f, 3f);
                Assert.That(Math.Abs(b - a), Is.LessThan(DecorWaves.WaterAmplitude), $"x={x}");
            }
        }

        [Test]
        public void BordDuPlanAnime_HouleEteinteAuBord_PleineAuMilieu()
        {
            Assert.That(DecorWaves.WaterEdge(0f), Is.EqualTo(0f));
            Assert.That(DecorWaves.WaterEdge(-5f), Is.EqualTo(0f));
            Assert.That(DecorWaves.WaterEdge(DecorWaves.WaterFade), Is.EqualTo(1f));
            Assert.That(DecorWaves.WaterEdge(500f), Is.EqualTo(1f));
            float prev = 0f;
            for (float d = 0f; d <= DecorWaves.WaterFade; d += 1f)
            {
                float w = DecorWaves.WaterEdge(d);
                Assert.That(w, Is.GreaterThanOrEqualTo(prev));
                prev = w;
            }
        }

        [Test]
        public void Nuages_NApprochentJamaisLaPiste()
        {
            for (float d = 0f; d < 200f; d += 0.5f)
            {
                float reach = DecorWaves.CloudDriftReach(d);
                Assert.That(reach, Is.InRange(0f, DecorWaves.CloudReach));
                // Au pire, la dérive rapproche le groupe de toute sa portée.
                if (d >= DecorWaves.CloudClearance) Assert.That(d - reach, Is.GreaterThanOrEqualTo(DecorWaves.CloudClearance - 1e-4f), $"d={d}");
                else Assert.That(reach, Is.EqualTo(0f), "un nuage déjà proche de la piste reste immobile");
            }
            Assert.That(DecorWaves.CloudDriftReach(150f), Is.EqualTo(DecorWaves.CloudReach));
        }

        [Test]
        public void Nuages_AllerRetourLentEtBorne()
        {
            float prev = DecorWaves.CloudDrift(0f, 0.4f);
            for (float t = 0f; t < DecorWaves.CloudPeriod; t += 0.1f)
            {
                float u = DecorWaves.CloudDrift(t, 0.4f);
                Assert.That(u, Is.InRange(-1f, 1f));
                Assert.That(Math.Abs(u - prev) * DecorWaves.CloudReach, Is.LessThan(0.1f), "moins de 1 m/s");
                prev = u;
                Assert.That(Math.Abs(DecorWaves.CloudBob(t, 0.4f)), Is.LessThanOrEqualTo(0.6f + 1e-5f));
            }
            Assert.That(DecorWaves.CloudDrift(DecorWaves.CloudPeriod, 0.4f), Is.EqualTo(DecorWaves.CloudDrift(0f, 0.4f)).Within(1e-4f));
            Assert.That(DecorWaves.CloudDrift(5f, 0f), Is.Not.EqualTo(DecorWaves.CloudDrift(5f, 2f)).Within(0.05f), "groupes déphasés");
        }
    }
}
