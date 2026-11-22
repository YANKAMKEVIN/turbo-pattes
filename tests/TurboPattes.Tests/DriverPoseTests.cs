using System;
using NUnit.Framework;
using TurboPattes.Core;

namespace TurboPattes.Tests
{
    public class DriverPoseTests
    {
        static void AssertRest(DriverPose p)
        {
            Assert.That(p.ArmLift, Is.EqualTo(0f).Within(1e-4));
            Assert.That(p.ArmSpread, Is.EqualTo(0f).Within(1e-4));
            Assert.That(p.HeadPitch, Is.EqualTo(0f).Within(1e-4));
            Assert.That(p.HeadRoll, Is.EqualTo(0f).Within(1e-4));
            Assert.That(p.Bounce, Is.EqualTo(0f).Within(1e-4));
        }

        [Test]
        public void PartentDeLaPostureDeConduite()
        {
            AssertRest(DriverPoses.Victory(0f));
            AssertRest(DriverPoses.Defeat(0f));
            AssertRest(DriverPoses.Pose(Celebration.None, 3f));
        }

        [Test]
        public void Victoire_BrasLevesEtSauts()
        {
            var p = DriverPoses.Victory(0.6f);
            Assert.That(p.ArmLift, Is.GreaterThan(100f));
            Assert.That(p.HeadPitch, Is.LessThan(0f), "regard vers le ciel");
            float maxBounce = 0f;
            for (float t = 0.4f; t < 3f; t += 0.01f) maxBounce = Math.Max(maxBounce, DriverPoses.Victory(t).Bounce);
            Assert.That(maxBounce, Is.InRange(0.08f, 0.13f));
        }

        [Test]
        public void Defaite_TeteBasseBrasTombants()
        {
            var p = DriverPoses.Defeat(1.5f);
            Assert.That(p.HeadPitch, Is.GreaterThan(25f));
            Assert.That(p.ArmLift, Is.LessThan(0f));
            Assert.That(p.Bounce, Is.LessThan(0f), "buste tassé");
        }

        [Test]
        public void ContinuesEtBornees()
        {
            foreach (var c in new[] { Celebration.Victory, Celebration.Defeat })
            {
                var prev = DriverPoses.Pose(c, 0f);
                for (float t = 0.01f; t < 10f; t += 0.01f)
                {
                    var p = DriverPoses.Pose(c, t);
                    Assert.That(Math.Abs(p.ArmLift - prev.ArmLift), Is.LessThan(6f), $"{c} t={t}");
                    Assert.That(Math.Abs(p.HeadRoll - prev.HeadRoll), Is.LessThan(2f), $"{c} t={t}");
                    Assert.That(Math.Abs(p.ArmLift), Is.LessThanOrEqualTo(115f));
                    Assert.That(Math.Abs(p.HeadPitch), Is.LessThanOrEqualTo(30f));
                    prev = p;
                }
            }
        }

        [TestCase(0, 8, Celebration.Victory)]
        [TestCase(2, 8, Celebration.Victory)]
        [TestCase(3, 8, Celebration.None)]
        [TestCase(4, 8, Celebration.Defeat)]
        [TestCase(7, 8, Celebration.Defeat)]
        [TestCase(0, 1, Celebration.Victory)]
        public void AnimationSelonLaPlace(int rank, int racers, Celebration expected) =>
            Assert.That(DriverPoses.For(rank, racers), Is.EqualTo(expected));
    
    /// <summary>Posture de conduite : penché dans les virages, coup d'œil en arrière après un dépassement.</summary>
    public class DrivingPoseTests
    {
        const float NoGlance = DriverPoses.GlanceCooldown;

        [Test]
        public void LigneDroite_PiloteDroit()
        {
            var p = DriverPoses.Driving(0f, 1f, NoGlance, 1);
            Assert.That(p.Lean, Is.EqualTo(0f));
            Assert.That(p.HeadYaw, Is.EqualTo(0f));
            Assert.That(p.HeadRoll, Is.EqualTo(0f));
            Assert.That(p.ArmLift, Is.EqualTo(0f), "mains au volant");
            Assert.That(p.Bounce, Is.EqualTo(0f));
        }

        [Test]
        public void VirageADroite_PencheEtRegardeADroite_SymetriqueAGauche()
        {
            var r = DriverPoses.Driving(1f, 1f, NoGlance, 1);
            var l = DriverPoses.Driving(-1f, 1f, NoGlance, 1);
            Assert.That(r.Lean, Is.EqualTo(-DriverPoses.DrivingLean), "même sens que l'inclinaison du kart (négatif = droite)");
            Assert.That(r.HeadYaw, Is.EqualTo(DriverPoses.DrivingLook));
            Assert.That(l.Lean, Is.EqualTo(-r.Lean));
            Assert.That(l.HeadYaw, Is.EqualTo(-r.HeadYaw));
            Assert.That(l.HeadRoll, Is.EqualTo(-r.HeadRoll));
        }

        [Test]
        public void PencheSelonLaVitesse_EtBorne()
        {
            Assert.That(DriverPoses.Driving(1f, 0f, NoGlance, 1).Lean, Is.EqualTo(0f), "à l'arrêt, on ne se penche pas");
            Assert.That(Math.Abs(DriverPoses.Driving(1f, 0.5f, NoGlance, 1).Lean), Is.EqualTo(DriverPoses.DrivingLean * 0.5f).Within(1e-5));
            var over = DriverPoses.Driving(3f, 2f, NoGlance, 1);
            Assert.That(Math.Abs(over.Lean), Is.EqualTo(DriverPoses.DrivingLean).Within(1e-5), "braquage et vitesse bornés");
            Assert.That(Math.Abs(over.HeadYaw), Is.EqualTo(DriverPoses.DrivingLook).Within(1e-5));
        }

        [Test]
        public void CoupDOeil_TourneLaTeteEtRevient()
        {
            Assert.That(DriverPoses.Driving(0f, 1f, 0f, 1).HeadYaw, Is.EqualTo(0f).Within(1e-4));
            Assert.That(DriverPoses.Driving(0f, 1f, DriverPoses.GlanceDuration / 2f, 1).HeadYaw, Is.EqualTo(DriverPoses.GlanceYaw).Within(1e-3));
            Assert.That(DriverPoses.Driving(0f, 1f, DriverPoses.GlanceDuration / 2f, -1).HeadYaw, Is.EqualTo(-DriverPoses.GlanceYaw).Within(1e-3));
            Assert.That(DriverPoses.Driving(0f, 1f, DriverPoses.GlanceDuration, 1).HeadYaw, Is.EqualTo(0f).Within(1e-4));
            // Continu : pas de saut de la tête d'une image à l'autre (60 i/s).
            float prev = DriverPoses.Driving(0.6f, 1f, 0f, 1).HeadYaw;
            for (float t = 0f; t <= DriverPoses.GlanceDuration + 0.2f; t += 1f / 60f)
            {
                float y = DriverPoses.Driving(0.6f, 1f, t, 1).HeadYaw;
                Assert.That(Math.Abs(y - prev), Is.LessThan(6f), $"t={t}");
                prev = y;
            }
        }

        [Test]
        public void CoupDOeil_SeulementApresUnDepassementEnRoulant_EtPasTropSouvent()
        {
            Assert.That(DriverPoses.StartsGlance(4, 3, NoGlance, 0.8f), Is.True);
            Assert.That(DriverPoses.StartsGlance(3, 3, NoGlance, 0.8f), Is.False, "même place");
            Assert.That(DriverPoses.StartsGlance(3, 4, NoGlance, 0.8f), Is.False, "dépassé, pas dépassant");
            Assert.That(DriverPoses.StartsGlance(-1, 3, NoGlance, 0.8f), Is.False, "place précédente inconnue (première image)");
            Assert.That(DriverPoses.StartsGlance(4, 3, 1f, 0.8f), Is.False, "trop tôt après le précédent");
            Assert.That(DriverPoses.StartsGlance(4, 3, NoGlance, 0.1f), Is.False, "presque à l'arrêt (départ, tête-à-queue)");
        }

        [Test]
        public void PostureDeRepos_RemetLePiloteDroit()
        {
            Assert.That(DriverPose.Rest.HeadYaw, Is.EqualTo(0f));
            Assert.That(DriverPose.Rest.Lean, Is.EqualTo(0f));
            Assert.That(DriverPoses.Victory(1f).Lean, Is.EqualTo(0f), "les animations de fin partent d'un pilote droit");
        }
    }
}
}
