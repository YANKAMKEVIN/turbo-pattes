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
    }
}
