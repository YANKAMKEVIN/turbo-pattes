using System;
using NUnit.Framework;
using TurboPattes.Core;

namespace TurboPattes.Tests
{
    public class HeightProfileTests
    {
        static HeightProfile Hills(params Kicker[] kickers) =>
            new(new[] { 0f, 100f, 250f, 400f, 600f, 800f }, new[] { 0f, 0f, 6f, 8f, 8f, 3f }, 1000f, kickers);

        [Test]
        public void PasseParLesCles()
        {
            var p = Hills();
            Assert.That(p.Height(250f), Is.EqualTo(6f).Within(1e-4));
            Assert.That(p.Height(600f), Is.EqualTo(8f).Within(1e-4));
            Assert.That(p.Height(800f), Is.EqualTo(3f).Within(1e-4));
        }

        [Test]
        public void JamaisAuDelaDesCles_NiDeBosseEntreDeuxClesEgales()
        {
            var p = Hills();
            for (float s = 0f; s < 1000f; s += 0.5f)
            {
                float h = p.Height(s);
                Assert.That(h, Is.InRange(-1e-4f, 8f + 1e-4f), $"s={s}");
                if (s > 400f && s < 600f) Assert.That(h, Is.EqualTo(8f).Within(1e-4), "plateau");
                if (s < 100f) Assert.That(h, Is.EqualTo(0f).Within(1e-4), "ligne droite plate");
            }
        }

        [Test]
        public void ContinuEtPenteCoherente_YComprisAuBouclage()
        {
            var p = Hills();
            for (float s = 0.5f; s < 1000f; s += 0.5f)
            {
                float numeric = (p.Height(s + 0.01f) - p.Height(s - 0.01f)) / 0.02f;
                Assert.That(p.Slope(s), Is.EqualTo(numeric).Within(2e-3), $"s={s}");
                Assert.That(Math.Abs(p.Height(s) - p.Height(s - 0.5f)), Is.LessThan(0.1f), $"saut de hauteur à s={s}");
            }
            Assert.That(p.Height(999.99f), Is.EqualTo(p.Height(0f)).Within(1e-2));
            Assert.That(p.Height(-10f), Is.EqualTo(p.Height(990f)).Within(1e-5));
        }

        [Test]
        public void Tremplin_MonteJusquAuBordPuisRetombe()
        {
            var p = Hills(new Kicker(500f, 10f, 1.5f));
            Assert.That(p.Height(489f), Is.EqualTo(8f).Within(1e-4), "avant le tremplin");
            Assert.That(p.Height(495f), Is.InRange(8.2f, 8.8f), "sur la rampe");
            Assert.That(p.Height(499.99f), Is.EqualTo(9.5f).Within(0.01), "au bord");
            Assert.That(p.Height(500.01f), Is.EqualTo(8f).Within(1e-3), "après le bord");
            Assert.That(p.Slope(499.9f), Is.GreaterThan(0.25f));
            Assert.That(p.Height(499.99f, onRamp: false), Is.EqualTo(8f).Within(1e-3), "à côté du tremplin");
            Assert.That(p.BaseHeight(499.99f), Is.EqualTo(8f).Within(1e-3));
        }

        [Test]
        public void ClesInvalides_Refusees()
        {
            Assert.Throws<ArgumentException>(() => new HeightProfile(new[] { 0f, 50f, 40f }, new[] { 0f, 1f, 2f }, 100f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HeightProfile(new[] { 0f, 120f }, new[] { 0f, 1f }, 100f));
        }
    }

    public class VerticalMotionTests
    {
        const float Dt = 1f / 60f;

        /// <summary>Kart qui roule tout droit à vitesse constante sur le profil ; renvoie la distance de vol la plus longue.</summary>
        static (int takeOffs, int landings, float longestFlight, float maxAboveGround) Drive(HeightProfile p, float speed, float from, float to)
        {
            var v = new VerticalMotion();
            v.Reset(p.Height(from));
            int takeOffs = 0, landings = 0;
            float flightStart = 0f, longest = 0f, above = 0f;
            for (float s = from; s < to; s += speed * Dt)
            {
                float next = s + speed * Dt;
                var e = v.Step(Dt, p.Height(next), p.Slope(next) * speed);
                if (e == VerticalEvent.TookOff) { takeOffs++; flightStart = next; }
                if (e == VerticalEvent.Landed) { landings++; longest = Math.Max(longest, next - flightStart); }
                above = Math.Max(above, v.Y - p.Height(next));
                Assert.That(v.Y, Is.GreaterThanOrEqualTo(p.Height(next) - 1e-3f), "sous le sol");
            }
            return (takeOffs, landings, longest, above);
        }

        [Test]
        public void Tremplin_LeKartDecollePuisRetombe()
        {
            var p = new HeightProfile(new[] { 0f, 450f, 550f, 700f }, new[] { 8f, 8f, 4f, 4f }, 1000f, new[] { new Kicker(500f, 10f, 1.5f) });
            var r = Drive(p, 26f, 400f, 650f);
            Assert.That(r.takeOffs, Is.EqualTo(1));
            Assert.That(r.landings, Is.EqualTo(1));
            Assert.That(r.longestFlight, Is.InRange(12f, 40f), "longueur du saut");
            Assert.That(r.maxAboveGround, Is.GreaterThan(1.5f));
        }

        [Test]
        public void CotesEtDescentesDouces_RestentAuSol()
        {
            var p = new HeightProfile(new[] { 0f, 100f, 250f, 400f, 600f, 800f }, new[] { 0f, 0f, 6f, 8f, 8f, 3f }, 1000f);
            var r = Drive(p, 35f, 0f, 2000f);
            Assert.That(r.takeOffs, Is.EqualTo(0));
            Assert.That(r.maxAboveGround, Is.LessThan(1e-3f));
        }

        [Test]
        public void Reception_DonneLaVitesseDImpact()
        {
            var v = new VerticalMotion();
            v.Reset(5f);
            Assert.That(v.Step(Dt, 3f, 0f), Is.EqualTo(VerticalEvent.TookOff), "le sol disparaît sous le kart");
            var e = VerticalEvent.None;
            for (int i = 0; i < 200 && e != VerticalEvent.Landed; i++) e = v.Step(Dt, 3f, 0f);
            Assert.That(e, Is.EqualTo(VerticalEvent.Landed));
            Assert.That(v.Grounded, Is.True);
            Assert.That(v.Y, Is.EqualTo(3f));
            // Chute de 2 m : v = sqrt(2 g h) ≈ 9,8 m/s.
            Assert.That(v.LandingSpeed, Is.EqualTo(Math.Sqrt(2 * VerticalMotion.Gravity * 2f)).Within(0.6));
        }
    }

    public class TrackProjectionTests
    {
        const int N = 800;

        /// <summary>
        /// Huit (lemniscate de Gerono, ~140 m de large) qui se croise à l'origine : le premier passage (t = π/2)
        /// est sur un pont à 8 m, le second (t = 3π/2) au sol.
        /// </summary>
        static TrackProjection FigureEight(out float[] dist, out float length)
        {
            float[] x = new float[N], y = new float[N], z = new float[N];
            for (int i = 0; i < N; i++)
            {
                (x[i], y[i], z[i]) = Point(i * 2 * Math.PI / N);
            }
            dist = new float[N];
            float d = 0f;
            for (int i = 0; i < N; i++)
            {
                dist[i] = d;
                int j = (i + 1) % N;
                d += (float)Math.Sqrt((x[j] - x[i]) * (x[j] - x[i]) + (y[j] - y[i]) * (y[j] - y[i]) + (z[j] - z[i]) * (z[j] - z[i]));
            }
            length = d;
            return new TrackProjection(x, y, z);
        }

        [Test]
        public void AuCroisement_LaHauteurChoisitLeBonTroncon()
        {
            var p = FigureEight(out _, out _);
            int bridge = N / 4, under = 3 * N / 4;
            Assert.That(Math.Abs(p.Nearest(0.3f, 8.4f, 0.2f, -1) - bridge), Is.LessThanOrEqualTo(2), "kart sur le pont");
            Assert.That(Math.Abs(p.Nearest(0.3f, 0.4f, 0.2f, -1) - under), Is.LessThanOrEqualTo(2), "kart dessous");
            // En plein saut (3 m au-dessus de la piste basse), il reste sur son tronçon.
            Assert.That(Math.Abs(p.Nearest(0.3f, 3f, 0.2f, under + 5) - under), Is.LessThanOrEqualTo(3));
        }

        [Test]
        public void TourCompletSurLeHuit_UnSeulTourCompte()
        {
            var proj = FigureEight(out var dist, out float length);
            var laps = new LapTracker(length, dist[N - 10]);
            int hint = N - 10;
            for (int step = 0; step <= N + 20; step++)
            {
                int i = (N - 10 + step) % N;
                // Position à mi-chemin entre deux échantillons, un peu décalée de la ligne médiane.
                var (px, py, pz) = Point((i + 0.5) * 2 * Math.PI / N);
                hint = proj.Nearest(px + 0.8f, py + 0.3f, pz - 0.5f, hint);
                int gap = Math.Abs(hint - i);
                Assert.That(Math.Min(gap, N - gap), Is.LessThanOrEqualTo(4), $"pas {step} : mauvais tronçon");
                laps.Update(dist[hint]);
            }
            Assert.That(laps.Lap, Is.EqualTo(1));
        }

        static (float x, float y, float z) Point(double t) =>
            ((float)(70 * Math.Cos(t)), (float)(4 + 4 * Math.Sin(t)), (float)(70 * Math.Sin(t) * Math.Cos(t) * 2));
    }
}
