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

    public class TuneTests
    {
        [TestCase("C4", 60)]
        [TestCase("A4", 69)]
        [TestCase("Bb3", 58)]
        [TestCase("C#5", 73)]
        [TestCase("G2", 43)]
        public void ParseNote_DonneLaHauteurMidi(string note, int midi) => Assert.That(Tune.ParseNote(note), Is.EqualTo(midi));

        [Test]
        public void ParseNote_RefuseLesNotesInvalides()
        {
            Assert.Throws<System.FormatException>(() => Tune.ParseNote("H4"));
            Assert.Throws<System.FormatException>(() => Tune.ParseNote("C"));
        }

        [Test]
        public void ParseMelody_TiretProlongePointCoupe()
        {
            var e = Tune.ParseMelody("C4 - . E4 - - G4 .");
            Assert.That(e.Count, Is.EqualTo(3));
            Assert.That((e[0].Midi, e[0].Start, e[0].Length), Is.EqualTo((60, 0, 2)));
            Assert.That((e[1].Midi, e[1].Start, e[1].Length), Is.EqualTo((64, 3, 3)));
            Assert.That((e[2].Midi, e[2].Start, e[2].Length), Is.EqualTo((67, 6, 1)));
        }

        [Test]
        public void ParseMelody_NoteTenueAuDelaDeLaBarre()
        {
            var e = Tune.ParseMelody("C4 - - - - - - - | - - . . . . . .");
            Assert.That(e.Count, Is.EqualTo(1));
            Assert.That(e[0].Length, Is.EqualTo(10));
        }

        [Test]
        public void ParseMelody_RefuseUneMesureIncomplete() =>
            Assert.Throws<System.FormatException>(() => Tune.ParseMelody("C4 - - -"));

        [Test]
        public void ChordNotes_MajeurEtMineur()
        {
            Assert.That(Tune.ChordNotes("C"), Is.EqualTo(new[] { 48, 52, 55 }));
            Assert.That(Tune.ChordNotes("Am"), Is.EqualTo(new[] { 57, 60, 64 }));
        }

        [Test]
        public void Morceaux_UneMesureDeMelodieParAccord()
        {
            foreach (var song in Songs.All)
            {
                var notes = Tune.ParseMelody(song.Melody);
                bool long16 = System.Array.IndexOf(Songs.Circuits, song) >= 0 || song == Songs.Menu;
                if (long16)
                {
                    Assert.That(notes.Count, Is.GreaterThan(20), song.Name);
                    Assert.That(song.Bars, Is.EqualTo(16), song.Name);
                }
                else
                {
                    Assert.That(notes.Count, Is.GreaterThanOrEqualTo(4), song.Name);
                    Assert.That(song.Bars, Is.GreaterThanOrEqualTo(2), song.Name);
                }
                Assert.That(song.Melody.Split('|').Length, Is.EqualTo(song.Bars), song.Name);
                foreach (var n in notes) Assert.That(n.Start + n.Length, Is.LessThanOrEqualTo(song.Steps), song.Name);
                foreach (var c in song.Chords) Assert.That(Tune.ChordNotes(c).Length, Is.EqualTo(3), song.Name + " " + c);
            }
        }

        [Test]
        public void PodiumEtArrivee_Courts()
        {
            Assert.That(Songs.Podium.Bars, Is.EqualTo(8));
            Assert.That(Songs.Finish.Bars, Is.EqualTo(2));
            Assert.That(Songs.Circuits, Does.Not.Contain(Songs.Podium));
            Assert.That(Songs.Circuits, Does.Not.Contain(Songs.Finish));
        }

        [TestCase(67, "G", true)]
        [TestCase(66, "G", true)]   // fa dièse
        [TestCase(65, "G", false)]  // fa naturel
        [TestCase(63, "Cm", true)]  // mi bémol
        [TestCase(64, "Cm", false)]
        [TestCase(68, "F#m", true)] // sol dièse
        [TestCase(70, "Bb", true)]
        public void InKey_ReconnaitLaGamme(int midi, string key, bool expected) =>
            Assert.That(Tune.InKey(midi, key), Is.EqualTo(expected));

        [Test]
        public void Morceaux_RestentDansLeurTonalite()
        {
            foreach (var song in Songs.All)
            {
                foreach (var n in Tune.ParseMelody(song.Melody))
                    Assert.That(Tune.InKey(n.Midi, song.Key), song.Name + " : note hors gamme " + n.Midi + " au pas " + n.Start);
                foreach (var c in song.Chords)
                foreach (int m in Tune.ChordNotes(c))
                    Assert.That(Tune.InKey(m, song.Key), song.Name + " : accord hors gamme " + c);
            }
        }

        [Test]
        public void Morceaux_TessitureChantable()
        {
            foreach (var song in Songs.All)
            foreach (var n in Tune.ParseMelody(song.Melody))
                Assert.That(n.Midi, Is.InRange(Tune.ParseNote("G4"), Tune.ParseNote("G6")), song.Name);
        }

        [Test]
        public void Circuits_UnMorceauDifferentChacun()
        {
            Assert.That(Songs.Circuits.Length, Is.EqualTo(10));
            var names = new System.Collections.Generic.HashSet<string>();
            var melodies = new System.Collections.Generic.HashSet<string>();
            var styles = new System.Collections.Generic.HashSet<SongStyle>();
            foreach (var song in Songs.Circuits)
            {
                Assert.That(names.Add(song.Name), "nom en double : " + song.Name);
                Assert.That(melodies.Add(song.Melody), "mélodie en double : " + song.Name);
                styles.Add(song.Style);
                Assert.That(song.Bpm, Is.InRange(115f, 170f), song.Name);
                Assert.That(song.Style, Is.Not.EqualTo(SongStyle.Calm), song.Name + " : accompagnement de menu");
            }
            Assert.That(styles.Count, Is.EqualTo(10), "un accompagnement propre à chaque circuit");
            Assert.That(Songs.ForCircuit(0), Is.SameAs(Songs.Race));
            Assert.That(Songs.ForCircuit(9), Is.SameAs(Songs.City));
            Assert.That(Songs.ForCircuit(42), Is.SameAs(Songs.Race));
        }

        [Test]
        public void Morceaux_FinissentSurLaToniqueOuSaQuinte()
        {
            // La boucle retombe sur ses pieds : dernier accord = tonique, ou dominante qui relance la tonique.
            foreach (var song in Songs.All)
            {
                string tonic = song.Key;
                string last = song.Chords[song.Bars - 1];
                int lastRoot = Tune.ChordNotes(last)[0] % 12, tonicRoot = Tune.ChordNotes(tonic)[0] % 12;
                int interval = ((lastRoot - tonicRoot) % 12 + 12) % 12;
                Assert.That(interval == 0 || interval == 7, song.Name + " finit sur " + last);
            }
        }
    }
}
