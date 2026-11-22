using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TurboPattes.Core;

namespace TurboPattes.Tests
{
    /// <summary>Écran de lancement décidé le 21/11 : splash Unity gardé, puis écran de marque « Turbo Grid ».</summary>
    public class BrandSplashTests
    {
        [Test]
        public void Duree_CourteEtDecoupageCoherent()
        {
            Assert.That(BrandSplash.Duration, Is.EqualTo(BrandSplash.FadeIn + BrandSplash.Hold + BrandSplash.FadeOut).Within(1e-6f));
            Assert.That(BrandSplash.Duration, Is.InRange(1.2f, 2.5f), "écran de marque court");
            Assert.That(BrandSplash.MinShown, Is.LessThan(BrandSplash.FadeOutStart));
            Assert.That(BrandSplash.Done(BrandSplash.Duration - 0.01f), Is.False);
            Assert.That(BrandSplash.Done(BrandSplash.Duration), Is.True);
        }

        [Test]
        public void Logo_ApparaitTientPuisSEteint()
        {
            Assert.That(BrandSplash.LogoAlpha(0f), Is.EqualTo(0f));
            Assert.That(BrandSplash.LogoAlpha(BrandSplash.FadeIn * 0.5f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(BrandSplash.LogoAlpha(BrandSplash.FadeIn), Is.EqualTo(1f));
            Assert.That(BrandSplash.LogoAlpha(BrandSplash.FadeOutStart), Is.EqualTo(1f));
            Assert.That(BrandSplash.LogoAlpha(BrandSplash.FadeOutStart + BrandSplash.FadeOut * 0.5f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(BrandSplash.LogoAlpha(BrandSplash.Duration), Is.EqualTo(0f));
            Assert.That(BrandSplash.LogoAlpha(10f), Is.EqualTo(0f));
        }

        [Test]
        public void Fond_OpaqueJusquAuFonduFinal()
        {
            // Opaque dès la première image : le fond carbone prend la suite du splash Unity sans laisser voir le circuit.
            Assert.That(BrandSplash.BackdropAlpha(0f), Is.EqualTo(1f));
            Assert.That(BrandSplash.BackdropAlpha(BrandSplash.FadeOutStart), Is.EqualTo(1f));
            Assert.That(BrandSplash.BackdropAlpha(BrandSplash.Duration), Is.EqualTo(0f));
            float prev = 1f;
            for (int i = 0; i <= 40; i++)
            {
                float a = BrandSplash.BackdropAlpha(i * BrandSplash.Duration / 40f);
                Assert.That(a, Is.LessThanOrEqualTo(prev + 1e-6f), "le fond ne fait que s'éclaircir");
                prev = a;
            }
        }

        [Test]
        public void Echelle_SePoseSansDepasser()
        {
            Assert.That(BrandSplash.LogoScale(0f), Is.EqualTo(BrandSplash.StartScale).Within(1e-5f));
            Assert.That(BrandSplash.LogoScale(BrandSplash.FadeIn), Is.EqualTo(1f).Within(1e-6f));
            Assert.That(BrandSplash.LogoScale(5f), Is.EqualTo(1f).Within(1e-6f));
            for (int i = 0; i <= 30; i++)
                Assert.That(BrandSplash.LogoScale(i * BrandSplash.FadeIn / 30f), Is.InRange(1f, BrandSplash.StartScale));
            Assert.That(BrandSplash.Sweep(0f), Is.EqualTo(0f));
            Assert.That(BrandSplash.Sweep(BrandSplash.SweepDuration), Is.EqualTo(1f).Within(1e-6f));
            Assert.That(BrandSplash.Sweep(BrandSplash.SweepDuration * 0.5f), Is.GreaterThan(0.5f), "sortie douce : le trait part vite");
        }

        [Test]
        public void Passer_SauteAuFonduSansRevenirEnArriere()
        {
            Assert.That(BrandSplash.Skip(0.1f), Is.EqualTo(0.1f), "appui trop tôt ignoré");
            Assert.That(BrandSplash.Skip(BrandSplash.MinShown), Is.EqualTo(BrandSplash.FadeOutStart));
            Assert.That(BrandSplash.Skip(1f), Is.EqualTo(BrandSplash.FadeOutStart));
            float during = BrandSplash.FadeOutStart + 0.2f;
            Assert.That(BrandSplash.Skip(during), Is.EqualTo(during), "déjà en fondu : on ne recommence pas");
        }

        [Test]
        public void Montre_UneSeuleFoisEtJamaisEnTest()
        {
            Assert.That(BrandSplash.ShouldShow(alreadyShown: false, automated: false), Is.True);
            Assert.That(BrandSplash.ShouldShow(alreadyShown: true, automated: false), Is.False, "pas au retour au menu");
            Assert.That(BrandSplash.ShouldShow(alreadyShown: false, automated: true), Is.False, "pas pendant -tp-menutest");
        }

        [Test]
        public void SplashUnity_GardeSurFondCarbone()
        {
            string settings = File.ReadAllText(ProjectSettingsPath());
            Assert.That(settings, Does.Contain("\n  m_ShowUnitySplashScreen: 1\n"), "splash Unity gardé (décision du 21/11)");
            var m = Regex.Match(settings, @"m_SplashScreenBackgroundColor: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: 1\}");
            Assert.That(m.Success, Is.True);
            float C(int i) => float.Parse(m.Groups[i].Value, CultureInfo.InvariantCulture);
            // Même carbone que le fond de l'écran de marque : pas de saut de couleur entre les deux.
            Assert.That(C(1), Is.EqualTo(Palette.Carbon.R).Within(0.5f / 255f));
            Assert.That(C(2), Is.EqualTo(Palette.Carbon.G).Within(0.5f / 255f));
            Assert.That(C(3), Is.EqualTo(Palette.Carbon.B).Within(0.5f / 255f));
        }

        [Test]
        public void ConfigurerLeProjet_RemetLeMemeSplash()
        {
            // BuildScript.ConfigurePlayer réécrit les réglages du splash : il doit reprendre le carbone, pas l'ancien vert.
            string script = File.ReadAllText(RepoPath("Assets", "TurboPattes", "Editor", "BuildScript.cs"));
            Assert.That(script, Does.Contain("PlayerSettings.SplashScreen.show = true;"));
            Assert.That(script, Does.Contain("PlayerSettings.SplashScreen.showUnityLogo = true;"));
            Assert.That(script, Does.Contain("var carbon = TurboPattes.Core.Palette.Carbon;"));
            Assert.That(script, Does.Not.Contain("0x24 / 255f"), "ancien fond « Atelier plein gaz »");
        }

        static string ProjectSettingsPath() => RepoPath("ProjectSettings", "ProjectSettings.asset");

        /// <summary>Fichier du dépôt, trouvé en remontant depuis le dossier des tests.</summary>
        static string RepoPath(params string[] parts)
        {
            string relative = Path.Combine(parts);
            for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir != null; dir = dir.Parent)
            {
                string path = Path.Combine(dir.FullName, relative);
                if (File.Exists(path)) return path;
            }
            throw new FileNotFoundException($"{relative} introuvable au-dessus du dossier des tests.");
        }
    }
}
