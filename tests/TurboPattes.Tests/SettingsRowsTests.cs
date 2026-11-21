using NUnit.Framework;
using TurboPattes.Core;

namespace TurboPattes.Tests
{
    public class SettingsRowsTests
    {
        [Test]
        public void TableauxCoherents()
        {
            Assert.That(SettingsRows.Kinds.Length, Is.EqualTo(SettingsRows.Count));
            Assert.That(SettingsRows.ToggleNames.Length, Is.EqualTo(SettingsRows.Count));
            for (int i = 0; i < SettingsRows.Count; i++)
                Assert.That(SettingsRows.ToggleNames[i] != null, Is.EqualTo(SettingsRows.Kinds[i] == SettingKind.Toggle), SettingsRows.Labels[i]);
            Assert.That(SettingsRows.Kinds[SettingsRows.Count - 1], Is.EqualTo(SettingKind.Action), "Retour en dernier");
        }

        [Test]
        public void SectionsOuvertesUneSeuleFois()
        {
            Assert.That(SettingsRows.SectionStart(0), Is.EqualTo("Son"));
            Assert.That(SettingsRows.SectionStart(1), Is.Null);
            Assert.That(SettingsRows.SectionStart(2), Is.EqualTo("Commandes"));
            Assert.That(SettingsRows.SectionStart(4), Is.Null);
            Assert.That(SettingsRows.SectionStart(5), Is.EqualTo("Affichage"));
            Assert.That(SettingsRows.SectionStart(6), Is.Null, "Retour hors section");
            Assert.That(SettingsRows.SectionStart(99), Is.Null);
        }

        [Test]
        public void PasSansDerive_EtBornes()
        {
            float v = 0f;
            for (int i = 0; i < 7; i++) v = SettingsRows.Step(v, 0.1f, 0f, 1f);
            Assert.That(v, Is.EqualTo(0.7f));
            Assert.That(SettingsRows.Step(1f, 0.1f, 0f, 1f), Is.EqualTo(1f));
            Assert.That(SettingsRows.Step(0.5f, -0.1f, 0.5f, 1.5f), Is.EqualTo(0.5f));
        }

        [TestCase(0f, 0f, 1f, 0)]
        [TestCase(0.8f, 0f, 1f, 8)]
        [TestCase(1f, 0f, 1f, 10)]
        [TestCase(1f, 0.5f, 1.5f, 5)]
        [TestCase(1.5f, 0.5f, 1.5f, 10)]
        [TestCase(2f, 0f, 1f, 10)]
        [TestCase(1f, 1f, 1f, 0)]
        public void CransAllumes(float value, float min, float max, int lit) => Assert.That(SettingsRows.Lit(value, min, max), Is.EqualTo(lit));
    }
}
