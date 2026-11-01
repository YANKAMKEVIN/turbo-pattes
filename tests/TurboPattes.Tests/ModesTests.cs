using System.Collections.Generic;
using NUnit.Framework;
using TurboPattes.Core;

namespace TurboPattes.Tests
{
    public class CupTests
    {
        [Test]
        public void Coupes_QuatreCircuitsEtTousLesCircuitsCouverts()
        {
            var covered = new HashSet<int>();
            var names = new HashSet<string>();
            foreach (var cup in Cups.All)
            {
                Assert.That(cup.Circuits.Length, Is.EqualTo(Cups.RacesPerCup), cup.Name);
                Assert.That(new HashSet<int>(cup.Circuits).Count, Is.EqualTo(Cups.RacesPerCup), cup.Name + " : circuit en double");
                Assert.That(names.Add(cup.Name), cup.Name);
                foreach (int c in cup.Circuits)
                {
                    Assert.That(c, Is.InRange(0, 10), cup.Name);
                    covered.Add(c);
                }
            }
            Assert.That(covered.Count, Is.EqualTo(11), "chaque circuit est dans au moins une coupe");
        }

        [Test]
        public void Points_DecroissentAvecLaPlace()
        {
            Assert.That(Cups.PointsFor(0), Is.EqualTo(10));
            Assert.That(Cups.PointsFor(7), Is.EqualTo(1));
            Assert.That(Cups.PointsFor(8), Is.EqualTo(0));
            Assert.That(Cups.PointsFor(-1), Is.EqualTo(0));
            for (int p = 1; p < Cups.Points.Length; p++) Assert.That(Cups.Points[p], Is.LessThan(Cups.Points[p - 1]));
        }

        [Test]
        public void Classement_CumuleLesPoints()
        {
            var cup = new CupStandings(4);
            cup.AddRace(new[] { 2, 0, 1, 3 }); // 2 : 10, 0 : 8, 1 : 6, 3 : 5
            cup.AddRace(new[] { 0, 2, 3, 1 }); // 0 : 10, 2 : 8, 3 : 6, 1 : 5
            Assert.That(cup.Races, Is.EqualTo(2));
            Assert.That(cup.Total(0), Is.EqualTo(18));
            Assert.That(cup.Total(2), Is.EqualTo(18));
            Assert.That(cup.Total(1), Is.EqualTo(11));
            Assert.That(cup.Total(3), Is.EqualTo(11));
            Assert.That(cup.LastPoints(3), Is.EqualTo(6));
            Assert.That(cup.LastPlace(1), Is.EqualTo(3));
        }

        [Test]
        public void Classement_EgaliteDepartageeParLesVictoiresPuisLaDerniereCourse()
        {
            var cup = new CupStandings(4);
            cup.AddRace(new[] { 2, 0, 1, 3 });
            cup.AddRace(new[] { 0, 2, 3, 1 });
            // 0 et 2 : 18 points, une victoire et une 2e place chacun -> 0 a fini devant à la dernière course.
            // 1 et 3 : 11 points, une 3e et une 4e place chacun -> 3 a fini devant à la dernière course.
            Assert.That(cup.Ranking(), Is.EqualTo(new[] { 0, 2, 3, 1 }));

            var wins = new CupStandings(3);
            wins.AddRace(new[] { 0, 1, 2 }); // 0 : 10, 1 : 8, 2 : 6
            wins.AddRace(new[] { 2, 1, 0 }); // 2 : 16, 1 : 16, 0 : 16
            // Égalité parfaite aux points : 0 et 2 ont une victoire, 1 aucune ; 2 a gagné la dernière.
            Assert.That(wins.Ranking(), Is.EqualTo(new[] { 2, 0, 1 }));
        }

        [Test]
        public void Coupe_HuitPilotesQuatreCourses()
        {
            var cup = new CupStandings(8);
            for (int race = 0; race < Cups.RacesPerCup; race++)
            {
                var order = new int[8];
                for (int p = 0; p < 8; p++) order[p] = (p + race) % 8;
                cup.AddRace(order);
            }
            int sum = 0;
            for (int r = 0; r < 8; r++) sum += cup.Total(r);
            Assert.That(sum, Is.EqualTo(4 * (10 + 8 + 6 + 5 + 4 + 3 + 2 + 1)));
            // Pilote 0 : places 1, 8, 7, 6 -> 10+1+2+3 = 16 ; pilote 1 : 2, 1, 8, 7 -> 8+10+1+2 = 21.
            Assert.That(cup.Total(0), Is.EqualTo(16));
            Assert.That(cup.Total(1), Is.EqualTo(21));
            var ranking = cup.Ranking();
            Assert.That(ranking.Length, Is.EqualTo(8));
            for (int i = 1; i < 8; i++) Assert.That(cup.Total(ranking[i]), Is.LessThanOrEqualTo(cup.Total(ranking[i - 1])));
        }

        [Test]
        public void AddRace_RefuseUnOrdreInvalide()
        {
            var cup = new CupStandings(3);
            Assert.Throws<System.ArgumentException>(() => cup.AddRace(new[] { 0, 1 }));
            Assert.Throws<System.ArgumentException>(() => cup.AddRace(new[] { 0, 1, 1 }));
            Assert.Throws<System.ArgumentException>(() => cup.AddRace(new[] { 0, 1, 5 }));
            Assert.That(cup.Races, Is.EqualTo(0));
        }
    }

    public class GhostTests
    {
        static GhostLap Straight()
        {
            // Un kart qui avance de 10 m/s le long de z pendant 2 s, enregistré à 60 i/s.
            var g = new GhostLap();
            for (int f = 0; f <= 120; f++)
            {
                float t = f / 60f;
                g.Record(t, new GhostPose(0f, 1f, 10f * t, 0f, 0f));
            }
            return g;
        }

        [Test]
        public void Record_UnePoseParIntervalle()
        {
            var g = Straight();
            Assert.That(g.Count, Is.EqualTo(21));
            Assert.That(g.Duration, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void Sample_Interpole()
        {
            var g = Straight();
            Assert.That(g.Sample(1.05f, out var p), Is.True);
            Assert.That(p.Z, Is.EqualTo(10.5f).Within(0.2f));
            Assert.That(p.Y, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(g.Sample(2.5f, out _), Is.False, "tour fini");
            Assert.That(g.Sample(-0.1f, out _), Is.False);
            Assert.That(new GhostLap().Sample(0f, out _), Is.False, "rien d'enregistré");
        }

        [Test]
        public void Record_ImagesLentesRemplissentLesTrous()
        {
            var g = new GhostLap();
            g.Record(0f, new GhostPose(0, 0, 0, 0, 0));
            g.Record(0.35f, new GhostPose(0, 0, 3.5f, 0, 0)); // une image de 350 ms
            Assert.That(g.Count, Is.EqualTo(4));
        }

        [TestCase(350f, 10f, 0.5f, 360f)]
        [TestCase(10f, 350f, 0.5f, 0f)]
        [TestCase(90f, 180f, 0.5f, 135f)]
        [TestCase(-170f, 170f, 0.5f, -180f)]
        public void LerpAngle_PrendLePlusCourtChemin(float a, float b, float u, float expected) =>
            Assert.That(GhostLap.LerpAngle(a, b, u), Is.EqualTo(expected).Within(1e-3f));

        [Test]
        public void Sample_VirageQuiPasseParLeNord()
        {
            var g = new GhostLap();
            g.Record(0f, new GhostPose(0, 0, 0, 350f, 0));
            g.Record(0.1f, new GhostPose(0, 0, 0, 10f, 0));
            g.Sample(0.05f, out var p);
            Assert.That(((p.Yaw % 360f) + 360f) % 360f, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Sauvegarde_AllerRetour()
        {
            var g = Straight();
            g.LapTime = 41.237f;
            var back = GhostLap.Parse(g.Serialize());
            Assert.That(back, Is.Not.Null);
            Assert.That(back.Count, Is.EqualTo(g.Count));
            Assert.That(back.LapTime, Is.EqualTo(41.237f).Within(1e-3f));
            g.Sample(1.37f, out var a);
            back.Sample(1.37f, out var b);
            Assert.That(b.Z, Is.EqualTo(a.Z).Within(0.01f));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("autre;1;0;")]
        [TestCase("fantome1;12.5;2;1,2,3")]
        [TestCase("fantome1;abc;0;")]
        public void Parse_RefuseUnTexteAbime(string text) => Assert.That(GhostLap.Parse(text), Is.Null);

        [Test]
        public void Records_ComparaisonDesTemps()
        {
            Assert.That(Records.IsBetter(40f, 0f), Is.True, "premier record");
            Assert.That(Records.IsBetter(40f, 41f), Is.True);
            Assert.That(Records.IsBetter(42f, 41f), Is.False);
            Assert.That(Records.IsBetter(0f, 41f), Is.False, "temps nul ignoré");
        }
    }

    public class ProgressionTests
    {
        [Test]
        public void Medailles_SeuilsOrdonnes()
        {
            for (int c = 0; c < 10; c++)
            {
                float gold = Medals.Target(c, Medal.Gold), silver = Medals.Target(c, Medal.Silver), bronze = Medals.Target(c, Medal.Bronze);
                Assert.That(gold, Is.LessThan(silver));
                Assert.That(silver, Is.LessThan(bronze));
                Assert.That(Medals.For(c, gold), Is.EqualTo(Medal.Gold));
                Assert.That(Medals.For(c, silver), Is.EqualTo(Medal.Silver));
                Assert.That(Medals.For(c, bronze), Is.EqualTo(Medal.Bronze));
                Assert.That(Medals.For(c, bronze + 0.2f), Is.EqualTo(Medal.None));
            }
            Assert.That(Medals.For(0, 0f), Is.EqualTo(Medal.None), "pas de temps");
            Assert.That(Medals.Reference.Length, Is.EqualTo(11));
        }

        [Test]
        public void Intermediaires_UnParQuartDeTour()
        {
            var sp = new LapSplits();
            Assert.That(sp.Update(0.99f, 0.1f), Is.EqualTo(-1), "départ derrière la ligne");
            Assert.That(sp.Update(0.1f, 2f), Is.EqualTo(-1));
            Assert.That(sp.Update(0.26f, 10f), Is.EqualTo(0));
            Assert.That(sp.Update(0.3f, 11f), Is.EqualTo(-1), "déjà compté");
            Assert.That(sp.Update(0.51f, 20f), Is.EqualTo(1));
            Assert.That(sp.Update(0.76f, 30f), Is.EqualTo(2));
            Assert.That(sp.Count, Is.EqualTo(3));
            Assert.That(sp.Times, Is.EqualTo(new[] { 10f, 20f, 30f }));
            sp.Reset();
            Assert.That(sp.Count, Is.EqualTo(0));
        }

        [Test]
        public void Intermediaires_SauteUnPointRateNePlusCompter()
        {
            var sp = new LapSplits();
            // Un saut de plume au-dessus du quart : on arrive directement à 0,5 ; le quart est perdu pour ce tour.
            Assert.That(sp.Update(0.5f, 20f), Is.EqualTo(-1));
            Assert.That(sp.Count, Is.EqualTo(0));
        }

        [Test]
        public void Fantome_AvecIntermediaires_EtAncienFormat()
        {
            var g = new GhostLap { LapTime = 40f, Splits = new[] { 10f, 20.5f, 30.25f } };
            g.Record(0f, new GhostPose(1, 2, 3, 4, 5));
            var back = GhostLap.Parse(g.Serialize());
            Assert.That(back.Splits, Is.EqualTo(new[] { 10f, 20.5f, 30.25f }));
            var old = GhostLap.Parse("fantome1;41.5;1;1,2,3,4,5");
            Assert.That(old, Is.Not.Null, "les fantômes déjà sauvegardés restent lisibles");
            Assert.That(old.Splits.Length, Is.EqualTo(0));
            Assert.That(old.LapTime, Is.EqualTo(41.5f).Within(1e-3f));
        }

        [Test]
        public void Trophees_EtDeblocage()
        {
            Assert.That(Cups.TrophyFor(0), Is.EqualTo(3));
            Assert.That(Cups.TrophyFor(2), Is.EqualTo(1));
            Assert.That(Cups.TrophyFor(3), Is.EqualTo(0));
            var trophies = new int[3];
            Assert.That(Cups.IsUnlocked(0, c => trophies[c]), Is.True);
            Assert.That(Cups.IsUnlocked(1, c => trophies[c]), Is.False);
            trophies[0] = 1;
            Assert.That(Cups.IsUnlocked(1, c => trophies[c]), Is.True);
            Assert.That(Cups.IsUnlocked(2, c => trophies[c]), Is.False);
        }

        [Test]
        public void Grille_OrdreInverseDuClassement()
        {
            Assert.That(Cups.Grid(new[] { 4, 0, 7, 1 }), Is.EqualTo(new[] { 1, 7, 0, 4 }));
        }
    }
}
