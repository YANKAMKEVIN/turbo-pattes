using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Suit la place du joueur pour faire réagir le HUD à chaque dépassement : rebond du chiffre et flèche pendant
    /// une demi-seconde, son et vibration annoncés au plus toutes les <see cref="MinGap"/> secondes (pas de rafale
    /// quand deux karts se doublent et se redoublent).
    /// </summary>
    public sealed class PlaceTracker
    {
        public const float Show = 0.5f;
        public const float MinGap = 0.3f;

        /// <summary>Place actuelle (0 = premier), -1 avant la première mesure.</summary>
        public int Rank { get; private set; } = -1;
        /// <summary>Dernier changement : +1 place gagnée, -1 place perdue, 0 aucun.</summary>
        public int Direction { get; private set; }
        /// <summary>Temps depuis le dernier changement.</summary>
        public float Since { get; private set; } = 99f;

        float _sinceAnnounce = 99f;

        /// <summary>Met à jour la place ; renvoie +1 / -1 s'il faut annoncer un gain / une perte (son, vibration), 0 sinon.</summary>
        public int Update(int rank, float dt)
        {
            Since += dt;
            _sinceAnnounce += dt;
            if (Rank < 0) { Rank = rank; return 0; }
            if (rank == Rank) return 0;
            Direction = rank < Rank ? 1 : -1;
            Rank = rank;
            Since = 0f;
            if (_sinceAnnounce < MinGap) return 0;
            _sinceAnnounce = 0f;
            return Direction;
        }

        /// <summary>La flèche et le rebond sont visibles.</summary>
        public bool Showing => Since < Show;

        /// <summary>Échelle du chiffre : rebond jusqu'à 1,3 puis retour à 1 en une demi-seconde.</summary>
        public float Bounce => Showing ? 1f + 0.3f * (float)Math.Sin(Math.PI * Since / Show) : 1f;
    }
}
