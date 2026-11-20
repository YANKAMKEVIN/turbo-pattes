using System;
using NUnit.Framework;
using TurboPattes.Core;

namespace TurboPattes.Tests
{
    public class AtelierTests
    {
        [Test]
        public void Palette_ValeursDeLaDirectionAtelier()
        {
            AssertRgb(Atelier.Charcoal, 0x24, 0x30, 0x2D);
            AssertRgb(Atelier.Vermilion, 0xD9, 0x4D, 0x3F);
            AssertRgb(Atelier.Ochre, 0xE9, 0xB9, 0x49);
            AssertRgb(Atelier.Workshop, 0x3C, 0x8F, 0x91);
            AssertRgb(Atelier.Cream, 0xF2, 0xE7, 0xCE);
        }

        [Test]
        public void Hex_AccepteSansDieseEtRefuseLeReste()
        {
            AssertRgb(Rgb.Hex("ff8000"), 255, 128, 0);
            Assert.Throws<FormatException>(() => Rgb.Hex("#FFF"));
        }

        static void AssertRgb(Rgb c, int r, int g, int b)
        {
            Assert.That(c.R * 255f, Is.EqualTo(r).Within(0.01f));
            Assert.That(c.G * 255f, Is.EqualTo(g).Within(0.01f));
            Assert.That(c.B * 255f, Is.EqualTo(b).Within(0.01f));
        }

        [Test]
        public void ChiffresFixes_MemeLargeurQuelsQueSoientLesChiffres()
        {
            Assert.That(FixedDigits.Width("1:11,11"), Is.EqualTo(FixedDigits.Width("8:08,08")));
            Assert.That(FixedDigits.Width("111"), Is.EqualTo(FixedDigits.Width("999")));
            Assert.That(FixedDigits.Cell(':'), Is.LessThan(FixedDigits.Cell('0')));
            Assert.That(FixedDigits.Width(null), Is.EqualTo(0f));
        }

        [Test]
        public void Temps_MinutesSecondesCentiemesAvecVirgule()
        {
            Assert.That(RaceText.Time(0f), Is.EqualTo("0:00,00"));
            Assert.That(RaceText.Time(38.52f), Is.EqualTo("0:38,52"));
            Assert.That(RaceText.Time(65.3f), Is.EqualTo("1:05,30"));
            // Arrondi au centième sans afficher « 0:60,00 ».
            Assert.That(RaceText.Time(59.996f), Is.EqualTo("1:00,00"));
            Assert.That(RaceText.Time(-1f), Is.EqualTo("0:00,00"));
        }

        [Test]
        public void PlacesEtEcarts()
        {
            Assert.That(RaceText.Ordinal(1), Is.EqualTo("1er"));
            Assert.That(RaceText.Ordinal(4), Is.EqualTo("4e"));
            Assert.That(RaceText.Gap(-0.42f), Is.EqualTo("-0,42"));
            Assert.That(RaceText.Gap(1.8f), Is.EqualTo("+1,80"));
            Assert.That(RaceText.Gap(-0.001f), Is.EqualTo("+0,00"));
        }
    }

    public class GapTests
    {
        [Test]
        public void Horloge_PremierPassageEtInterpolation()
        {
            var c = new ProgressClock(step: 10f, origin: 0f);
            c.Record(0f, 1f);
            c.Record(10f, 2f);
            c.Record(5f, 9f); // recul : n'efface rien
            c.Record(20f, 4f);
            Assert.That(c.TimeAt(15f, out float t), Is.True);
            Assert.That(t, Is.EqualTo(3f).Within(1e-4f));
            Assert.That(c.TimeAt(20f, out t), Is.True);
            Assert.That(t, Is.EqualTo(4f));
            Assert.That(c.TimeAt(25f, out _), Is.False, "pas encore atteint");
            Assert.That(c.TimeAt(-1f, out _), Is.False);
        }

        [Test]
        public void Ecarts_DevantEtDerriere()
        {
            // Celui de devant passe chaque repère de 10 m 1,5 s plus tôt que le joueur.
            var ahead = new ProgressClock(10f, 0f);
            var me = new ProgressClock(10f, 0f);
            for (int i = 0; i <= 10; i++)
            {
                ahead.Record(i * 10f, i * 1f);
                me.Record(i * 10f, i * 1f + 1.5f);
            }
            Assert.That(RaceGaps.Ahead(ahead, 50f, 6.5f, out float gap), Is.True);
            Assert.That(gap, Is.EqualTo(1.5f).Within(1e-4f));
            // Le joueur est passé à 40 m à 5,5 s : celui qui y est maintenant (8 s) a 2,5 s de retard.
            Assert.That(RaceGaps.Behind(me, 40f, 8f, out gap), Is.True);
            Assert.That(gap, Is.EqualTo(2.5f).Within(1e-4f));
        }

        [Test]
        public void Tendance_EtSecondes()
        {
            Assert.That(RaceGaps.Trend(2f, 1.8f), Is.EqualTo(-1));
            Assert.That(RaceGaps.Trend(2f, 2.02f), Is.EqualTo(0));
            Assert.That(RaceGaps.Trend(2f, 2.3f), Is.EqualTo(1));
            Assert.That(RaceText.Seconds(1.244f), Is.EqualTo("1,24"));
            Assert.That(RaceText.Seconds(-3f), Is.EqualTo("0,00"));
        }
    }

    public class MiniMapTests
    {
        [Test]
        public void Cadrage_MemeEchelleCentreNordEnHaut()
        {
            // Tracé de 200 m (x) sur 100 m (z) dans un carré de 100 px sans marge : échelle 0,5, centré verticalement.
            var xs = new[] { -100f, 100f, 100f, -100f };
            var zs = new[] { -50f, -50f, 50f, 50f };
            var fit = MiniMap.Fit(xs, zs, 10f, 20f, 100f, 100f);
            Assert.That(fit.Scale, Is.EqualTo(0.5f).Within(1e-5f));
            fit.Project(-100f, 50f, out float x, out float y);
            Assert.That(x, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(y, Is.EqualTo(45f).Within(1e-4f), "coin nord-ouest en haut à gauche, tracé centré");
            fit.Project(100f, -50f, out x, out y);
            Assert.That(x, Is.EqualTo(110f).Within(1e-4f));
            Assert.That(y, Is.EqualTo(95f).Within(1e-4f));
        }

        [Test]
        public void Pointilles_RegulierementEspaces()
        {
            Assert.That(MiniMap.Spread(10, 5), Is.EqualTo(new[] { 0, 2, 4, 6, 8 }));
            Assert.That(MiniMap.Spread(3, 10), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(MiniMap.Spread(0, 4), Is.Empty);
        }
    }

    public class ItemButtonTests
    {
        [Test]
        public void Bouton_VideRouletteOuObjet()
        {
            Assert.That(ItemRoll.Shown(ItemKind.None, 0f, 3f), Is.EqualTo(ItemKind.None));
            Assert.That(ItemRoll.Shown(ItemKind.Honey, 0f, 3f), Is.EqualTo(ItemKind.Honey));
            Assert.That(ItemRoll.Shown(ItemKind.Honey, 0.5f, 0f), Is.EqualTo(ItemKind.Carrot), "roulette au début");
        }

        [Test]
        public void Roulette_PasseParTousLesObjets()
        {
            var seen = new System.Collections.Generic.HashSet<ItemKind>();
            for (int i = 0; i < ItemRoll.KindCount; i++) seen.Add(ItemRoll.Reel((i + 0.5f) / 14f));
            Assert.That(seen.Count, Is.EqualTo(ItemRoll.KindCount));
            Assert.That(seen, Does.Not.Contain(ItemKind.None));
        }
    }

    public class ThreatTests
    {
        [Test]
        public void Urgence_GrossitEtAccelereEnApprochant()
        {
            Assert.That(ThreatAlert.Urgency(ThreatAlert.Range), Is.EqualTo(0f));
            Assert.That(ThreatAlert.Urgency(0f), Is.EqualTo(1f));
            Assert.That(ThreatAlert.Urgency(100f), Is.EqualTo(0f));
            Assert.That(ThreatAlert.Interval(1f), Is.LessThan(ThreatAlert.Interval(0.2f)));
            Assert.That(ThreatAlert.Scale(1f), Is.GreaterThan(ThreatAlert.Scale(0f)));
        }

        [Test]
        public void Bips_UnToutDeSuitePuisAuRythme()
        {
            var b = new ThreatBeeper();
            Assert.That(b.Tick(0.01f, true, 0f), Is.True, "premier bip immédiat");
            int beeps = 0;
            for (int i = 0; i < 110; i++) if (b.Tick(0.01f, true, 0f)) beeps++;
            Assert.That(beeps, Is.EqualTo(2), "un bip toutes les 0,5 s au loin (1,1 s)");
            b.Tick(0.01f, false, 0f);
            beeps = 0;
            for (int i = 0; i < 100; i++) if (b.Tick(0.01f, true, 1f)) beeps++;
            Assert.That(beeps, Is.InRange(8, 11), "un bip toutes les 0,1 s au contact");
            Assert.That(b.Tick(0.01f, false, 1f), Is.False);
            Assert.That(b.Tick(0.01f, true, 1f), Is.True, "nouvelle menace : bip immédiat");
        }
    }

    public class LeaveWarningTests
    {
        [Test]
        public void Coupe_NommeLaCoupeEtLesCoursesPerdues()
        {
            Assert.That(LeaveWarning.Question(true, "Coupe Braise"), Is.EqualTo("Quitter la Coupe Braise ?"));
            Assert.That(LeaveWarning.Loss(true, "Coupe Braise", 2, false), Is.EqualTo("Les 2 courses jouées seront perdues."));
            Assert.That(LeaveWarning.Loss(true, "Coupe Braise", 1, false), Is.EqualTo("La course jouée sera perdue."));
            Assert.That(LeaveWarning.Loss(true, "Coupe Braise", 0, false), Is.EqualTo("La coupe en cours sera perdue."));
        }

        [Test]
        public void HorsCoupe_EtRecommencer()
        {
            Assert.That(LeaveWarning.Question(true, null), Is.EqualTo("Revenir au menu ?"));
            Assert.That(LeaveWarning.Question(false, "Coupe Braise"), Is.EqualTo("Recommencer la course ?"));
            Assert.That(LeaveWarning.Loss(false, null, 0, true), Is.EqualTo("Le chrono repart de zéro."));
            Assert.That(LeaveWarning.Confirm(true), Is.EqualTo("Quitter"));
            Assert.That(LeaveWarning.Confirm(false), Is.EqualTo("Recommencer"));
        }
    }

    public class DriftGaugeTests
    {
        [Test]
        public void Segments_SeRemplissentEntreLesPaliers()
        {
            var t = DriftCharge.Thresholds;
            Assert.That(DriftCharge.SegmentFill(0f, 0), Is.EqualTo(0f));
            Assert.That(DriftCharge.SegmentFill(t[0] * 0.5f, 0), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(DriftCharge.SegmentFill(t[0] * 0.5f, 1), Is.EqualTo(0f));
            Assert.That(DriftCharge.SegmentFill((t[0] + t[1]) * 0.5f, 0), Is.EqualTo(1f));
            Assert.That(DriftCharge.SegmentFill((t[0] + t[1]) * 0.5f, 1), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(DriftCharge.SegmentFill(99f, 2), Is.EqualTo(1f));
            Assert.That(DriftCharge.SegmentFill(99f, 3), Is.EqualTo(0f));
        }
    }

    public class CircuitGridTests
    {
        [Test]
        public void Grille_DeplacementSansSortir()
        {
            Assert.That(CircuitGrid.Rows(10), Is.EqualTo(2));
            Assert.That(CircuitGrid.Move(0, -1, 0, 10), Is.EqualTo(0));
            Assert.That(CircuitGrid.Move(0, 1, 0, 10), Is.EqualTo(1));
            Assert.That(CircuitGrid.Move(2, 0, 1, 10), Is.EqualTo(7));
            Assert.That(CircuitGrid.Move(7, 0, 1, 10), Is.EqualTo(7));
            Assert.That(CircuitGrid.Move(4, 1, 0, 10), Is.EqualTo(4), "pas de retour à la ligne");
            // Grille incomplète : on reste sur la dernière vignette existante.
            Assert.That(CircuitGrid.Move(3, 0, 1, 7), Is.EqualTo(6));
        }
    }

    public class ResultRevealTests
    {
        [Test]
        public void Lignes_DuDernierAuPremier()
        {
            Assert.That(ResultReveal.RowStart(7, 8), Is.LessThan(ResultReveal.RowStart(0, 8)), "le dernier entre d'abord");
            Assert.That(ResultReveal.RowProgress(0, 8, 0f), Is.EqualTo(0f));
            Assert.That(ResultReveal.RowProgress(7, 8, ResultReveal.RowStart(7, 8) + ResultReveal.Slide), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(ResultReveal.Done(8), Is.EqualTo(ResultReveal.RowStart(0, 8) + ResultReveal.Slide));
            float mid = ResultReveal.RowProgress(3, 8, ResultReveal.RowStart(3, 8) + ResultReveal.Slide * 0.5f);
            Assert.That(mid, Is.GreaterThan(0.5f), "entrée freinée en fin de course");
        }

        [Test]
        public void Confettis_SeulementSurLePodium()
        {
            Assert.That(ResultReveal.Celebrate(0), Is.True);
            Assert.That(ResultReveal.Celebrate(2), Is.True);
            Assert.That(ResultReveal.Celebrate(3), Is.False);
        }
    }

    public class TutorialCueTests
    {
        [Test]
        public void Cibles_DesConseils()
        {
            Assert.That(TutorialCues.Target(0), Is.EqualTo(TutorialTarget.None));
            Assert.That(TutorialCues.Target(1), Is.EqualTo(TutorialTarget.Steer));
            Assert.That(TutorialCues.Target(2), Is.EqualTo(TutorialTarget.Drift));
            Assert.That(TutorialCues.Target(3), Is.EqualTo(TutorialTarget.Item));
            Assert.That(TutorialCues.Target(4), Is.EqualTo(TutorialTarget.Drift));
        }

        [Test]
        public void Gestes_Reussis()
        {
            Assert.That(TutorialCues.Achieved(1, 0.2f, 0, false, 0), Is.False);
            Assert.That(TutorialCues.Achieved(1, -0.8f, 0, false, 0), Is.True);
            Assert.That(TutorialCues.Achieved(2, 0f, 1, false, 0), Is.True);
            Assert.That(TutorialCues.Achieved(3, 0f, 0, true, 0), Is.False);
            Assert.That(TutorialCues.Achieved(3, 0f, 0, false, 0), Is.True);
            Assert.That(TutorialCues.Achieved(4, 0f, 0, false, 1), Is.True);
            Assert.That(TutorialCues.Achieved(0, 1f, 3, false, 2), Is.False, "le super départ n'a pas de geste à valider");
            Assert.That(TutorialCues.Pulse(0.3f), Is.InRange(1.1f, 1.3f));
        }
    }

    public class TouchZonesTests
    {
        [Test]
        public void Droitier_DirectionAGaucheBoutonsADroite()
        {
            var z = TouchZones.Compute(1600f, 720f, 0f, 1600f, 0f, 1f, false);
            Assert.That(z.InSteerZone(100f), Is.True);
            Assert.That(z.InSteerZone(1500f), Is.False);
            Assert.That(z.Drift.X, Is.GreaterThan(800f));
            Assert.That(z.Item.X, Is.GreaterThan(800f));
            Assert.That(z.Brake.X, Is.GreaterThan(800f));
            Assert.That(z.SteerHintX, Is.LessThan(800f));
        }

        [Test]
        public void Gaucher_MiroirDansLaZoneSure()
        {
            // Encoche à gauche : zone sûre de 80 à 1600 px.
            var r = TouchZones.Compute(1600f, 720f, 80f, 1600f, 0f, 1f, false);
            var l = TouchZones.Compute(1600f, 720f, 80f, 1600f, 0f, 1f, true);
            Assert.That(l.Drift.X, Is.EqualTo(80f + 1600f - r.Drift.X).Within(1e-3f));
            Assert.That(l.Brake.X, Is.EqualTo(80f + 1600f - r.Brake.X).Within(1e-3f));
            Assert.That(l.Item.Y, Is.EqualTo(r.Item.Y));
            Assert.That(l.Drift.R, Is.EqualTo(r.Drift.R));
            Assert.That(l.InSteerZone(1500f), Is.True);
            Assert.That(l.InSteerZone(100f), Is.False);
            Assert.That(l.SteerHintX, Is.GreaterThan(800f));
        }

        [Test]
        public void Sensibilite_RaccourcitLeGlisser()
        {
            var normal = TouchZones.Compute(1600f, 720f, 0f, 1600f, 0f, 1f, false);
            var vive = TouchZones.Compute(1600f, 720f, 0f, 1600f, 0f, 1.5f, false);
            Assert.That(vive.SteerRange, Is.LessThan(normal.SteerRange));
            Assert.That(normal.Drift.Contains(normal.Drift.X + normal.Drift.R * 1.1f, normal.Drift.Y, 1.15f), Is.True);
        }
    }

    public class PlaceTrackerTests
    {
        [Test]
        public void Depassement_AnnonceEtRebond()
        {
            var t = new PlaceTracker();
            Assert.That(t.Update(5, 0.1f), Is.EqualTo(0), "première mesure : rien à annoncer");
            Assert.That(t.Showing, Is.False);
            Assert.That(t.Update(4, 0.5f), Is.EqualTo(1), "place gagnée");
            Assert.That(t.Direction, Is.EqualTo(1));
            Assert.That(t.Showing, Is.True);
            t.Update(4, PlaceTracker.Show * 0.5f);
            Assert.That(t.Bounce, Is.GreaterThan(1.2f));
            t.Update(4, PlaceTracker.Show);
            Assert.That(t.Showing, Is.False);
            Assert.That(t.Bounce, Is.EqualTo(1f));
            Assert.That(t.Update(5, 0.1f), Is.EqualTo(-1), "place perdue");
        }

        [Test]
        public void Rafale_UneSeuleAnnonce()
        {
            var t = new PlaceTracker();
            t.Update(3, 0f);
            Assert.That(t.Update(2, 1f), Is.EqualTo(1));
            Assert.That(t.Update(3, 0.05f), Is.EqualTo(0), "redoublé aussitôt : pas de second son");
            Assert.That(t.Direction, Is.EqualTo(-1), "mais la flèche suit");
            Assert.That(t.Update(2, PlaceTracker.MinGap), Is.EqualTo(1));
        }
    }

    public class SpeedLinesTests
    {
        [Test]
        public void Nombre_SuitLaQualite()
        {
            Assert.That(SpeedLines.Count(0), Is.GreaterThan(SpeedLines.Count(4)));
            Assert.That(SpeedLines.Count(4), Is.GreaterThan(0));
            Assert.That(SpeedLines.Count(99), Is.EqualTo(SpeedLines.Count(4)));
        }

        [Test]
        public void Intensite_TurboEtSauts()
        {
            Assert.That(SpeedLines.Target(true, false, 0.2f), Is.EqualTo(1f));
            Assert.That(SpeedLines.Target(false, true, 1f), Is.EqualTo(0.6f).Within(1e-5f));
            Assert.That(SpeedLines.Target(false, false, 1f), Is.EqualTo(0f));
        }

        [Test]
        public void Traits_SurLesBords()
        {
            for (int i = 0; i < 20; i++)
            {
                SpeedLines.Line(i, 20, i * 0.37f, out _, out float inner, out float length, out float alpha);
                Assert.That(inner, Is.InRange(0.6f, 1.01f), "loin du centre : la piste reste dégagée");
                Assert.That(length, Is.InRange(0.1f, 0.3f));
                Assert.That(alpha, Is.InRange(0f, 1f));
            }
        }
    }

    public class SprintMusicTests
    {
        [Test]
        public void DernierTour_MemePartitionPlusRapide()
        {
            var song = Songs.Canyon;
            var fast = Songs.Sprint(song);
            Assert.That(fast, Is.SameAs(Songs.Sprint(song)), "même instance à chaque appel");
            Assert.That(fast.Bpm, Is.EqualTo(song.Bpm * 1.12f).Within(1e-3f));
            Assert.That(fast.Melody, Is.EqualTo(song.Melody));
            Assert.That(fast.Chords, Is.EqualTo(song.Chords));
            Assert.That(fast.Style, Is.EqualTo(song.Style));
            Assert.That(fast.Name, Is.Not.EqualTo(song.Name), "nom distinct : le lecteur enchaîne sur le nouveau morceau");
            Assert.That(fast.StepSeconds, Is.LessThan(song.StepSeconds));
            Assert.That(Songs.Sprint(null), Is.Null);
        }
    }
}
