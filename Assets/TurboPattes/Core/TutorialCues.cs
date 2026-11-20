using System;

namespace TurboPattes.Core
{
    /// <summary>Commande tactile visée par un conseil du tutoriel.</summary>
    public enum TutorialTarget { None, Steer, Drift, Item }

    /// <summary>
    /// Conseils du tutoriel (0 super départ, 1 direction, 2 dérapage, 3 objet, 4 figure) : quelle commande pointer en
    /// tactile, et quand le geste est réussi (le conseil s'efface alors, avec un son de validation).
    /// </summary>
    public static class TutorialCues
    {
        public static TutorialTarget Target(int tip) => tip switch
        {
            1 => TutorialTarget.Steer,
            2 => TutorialTarget.Drift,
            3 => TutorialTarget.Item,
            4 => TutorialTarget.Drift,
            _ => TutorialTarget.None,
        };

        /// <param name="steer">Direction du joueur (-1 à 1).</param>
        /// <param name="driftLevel">Niveau de charge du dérapage en cours (0 hors dérapage).</param>
        /// <param name="hasItem">Le joueur a encore un objet en main.</param>
        /// <param name="newTricks">Figures réussies depuis l'apparition du conseil.</param>
        public static bool Achieved(int tip, float steer, int driftLevel, bool hasItem, int newTricks) => tip switch
        {
            1 => Math.Abs(steer) > 0.5f,
            2 => driftLevel >= 1,
            3 => !hasItem,
            4 => newTricks > 0,
            _ => false,
        };

        /// <summary>Rayon relatif de l'anneau qui pulse autour de la commande (1,1 à 1,3, deux battements par seconde).</summary>
        public static float Pulse(float time) => 1.2f + 0.1f * (float)Math.Sin(time * Math.PI * 4.0);
    }
}
