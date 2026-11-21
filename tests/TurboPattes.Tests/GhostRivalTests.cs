using NUnit.Framework;
using TurboPattes.Core;

namespace TurboPattes.Tests
{
    public class GhostRivalTests
    {
        [Test]
        public void LePiloteEstSauvegardeAvecLeFantome()
        {
            var g = new GhostLap { LapTime = 40f, Driver = 4, Splits = new[] { 10f, 20f, 30f } };
            g.Record(0f, new GhostPose(1, 2, 3, 4, 5));
            var back = GhostLap.Parse(g.Serialize());
            Assert.That(back, Is.Not.Null);
            Assert.That(back.Driver, Is.EqualTo(4));
            Assert.That(back.Splits, Is.EqualTo(new[] { 10f, 20f, 30f }));
        }

        [Test]
        public void AnciensFormats_PiloteInconnu()
        {
            Assert.That(GhostLap.Parse("fantome1;41.5;1;1,2,3,4,5").Driver, Is.EqualTo(-1));
            var v2 = GhostLap.Parse("fantome2;41.5;10,20,30;1;1,2,3,4,5");
            Assert.That(v2, Is.Not.Null);
            Assert.That(v2.Driver, Is.EqualTo(-1));
            Assert.That(v2.Splits.Length, Is.EqualTo(3));
        }

        [TestCase("fantome3;x;41.5;;1;1,2,3,4,5")]
        [TestCase("fantome3;-2;41.5;;1;1,2,3,4,5")]
        [TestCase("fantome3;2;41.5;;1;1,2,3")]
        public void Parse_RefusePiloteOuDonneesAbimes(string text) => Assert.That(GhostLap.Parse(text), Is.Null);

        [Test]
        public void Rival_LePlusRapideDesAutresPilotes()
        {
            var laps = new[] { 40f, 0f, 41f, 0f, 39.5f, 0f, 0f, 0f };
            Assert.That(GhostPick.Rival(0, laps), Is.EqualTo(4));
            Assert.That(GhostPick.Rival(4, laps), Is.EqualTo(0), "jamais son propre pilote");
        }

        [Test]
        public void Rival_AucunTantQuOnNaRouleQuAvecSonPilote()
        {
            var laps = new[] { 40f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
            Assert.That(GhostPick.Rival(0, laps), Is.EqualTo(-1));
            Assert.That(GhostPick.Rival(1, laps), Is.EqualTo(0));
            Assert.That(GhostPick.Names.Length, Is.EqualTo(3));
        }
    }
}
