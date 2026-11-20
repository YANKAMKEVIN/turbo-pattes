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
}
