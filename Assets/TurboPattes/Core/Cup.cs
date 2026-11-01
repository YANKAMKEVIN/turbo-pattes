using System;
using System.Collections.Generic;

namespace TurboPattes.Core
{
    /// <summary>Une coupe : quatre circuits enchaînés (indices dans <c>Circuit.All()</c>).</summary>
    public sealed class CupDef
    {
        public string Name;
        public int[] Circuits;
    }

    public static class Cups
    {
        public const int RacesPerCup = 4;

        /// <summary>Points selon la place (1er = 10 … 8e = 1).</summary>
        public static readonly int[] Points = { 10, 8, 6, 5, 4, 3, 2, 1 };

        public static readonly CupDef[] All =
        {
            new() { Name = "Coupe Pâquerette", Circuits = new[] { 0, 1, 2, 3 } }, // Prairie, Canyon, Jungle, Lagon
            new() { Name = "Coupe Coquillage", Circuits = new[] { 4, 7, 5, 6 } }, // Récif, Banquise, Nuages, Espace
            new() { Name = "Coupe Braise", Circuits = new[] { 8, 9, 1, 6 } },     // Volcan, Ville Néon, Canyon, Espace
        };

        public static int PointsFor(int place) => place >= 0 && place < Points.Length ? Points[place] : 0;

        /// <summary>Trophée selon la place finale : 3 = or (1er), 2 = argent, 1 = bronze, 0 = rien.</summary>
        public static int TrophyFor(int finalPlace) => finalPlace >= 0 && finalPlace < 3 ? 3 - finalPlace : 0;

        /// <summary>Une coupe s'ouvre quand on est monté sur le podium de la précédente ; la première est toujours ouverte.</summary>
        public static bool IsUnlocked(int cup, Func<int, int> trophyOf) => cup <= 0 || trophyOf(cup - 1) > 0;

        /// <summary>
        /// Grille de départ d'une course de coupe : ordre inverse du classement cumulé (le dernier part en tête,
        /// le leader en fond de grille). <paramref name="ranking"/> : pilotes du premier au dernier.
        /// </summary>
        public static int[] Grid(int[] ranking)
        {
            var grid = new int[ranking.Length];
            for (int i = 0; i < ranking.Length; i++) grid[i] = ranking[ranking.Length - 1 - i];
            return grid;
        }
    }

    /// <summary>
    /// Classement cumulé d'une coupe. Les pilotes sont désignés par un numéro fixe (0 à n-1) d'une course à l'autre.
    /// À égalité de points : le plus de victoires, puis de deuxièmes places, etc. ; puis la meilleure place à la dernière course.
    /// </summary>
    public sealed class CupStandings
    {
        readonly int[] _totals;
        readonly int[][] _placeCounts;
        readonly int[] _lastPlace;
        readonly int[] _lastPoints;

        public int Racers { get; }
        public int Races { get; private set; }

        public CupStandings(int racers)
        {
            if (racers <= 0) throw new ArgumentOutOfRangeException(nameof(racers));
            Racers = racers;
            _totals = new int[racers];
            _lastPlace = new int[racers];
            _lastPoints = new int[racers];
            _placeCounts = new int[racers][];
            for (int i = 0; i < racers; i++) _placeCounts[i] = new int[racers];
        }

        public int Total(int racer) => _totals[racer];
        /// <summary>Points gagnés à la dernière course.</summary>
        public int LastPoints(int racer) => _lastPoints[racer];
        /// <summary>Place (0 = premier) à la dernière course.</summary>
        public int LastPlace(int racer) => _lastPlace[racer];

        /// <summary>Ajoute une course : <paramref name="finishOrder"/>[place] = numéro du pilote.</summary>
        public void AddRace(IReadOnlyList<int> finishOrder)
        {
            if (finishOrder.Count != Racers) throw new ArgumentException($"{finishOrder.Count} pilotes au lieu de {Racers}");
            var seen = new bool[Racers];
            foreach (int r in finishOrder)
            {
                if (r < 0 || r >= Racers || seen[r]) throw new ArgumentException("Ordre d'arrivée invalide");
                seen[r] = true;
            }
            for (int place = 0; place < Racers; place++)
            {
                int r = finishOrder[place];
                int pts = Cups.PointsFor(place);
                _totals[r] += pts;
                _lastPoints[r] = pts;
                _lastPlace[r] = place;
                _placeCounts[r][place]++;
            }
            Races++;
        }

        /// <summary>Numéros des pilotes, du premier au dernier du classement cumulé.</summary>
        public int[] Ranking()
        {
            var order = new int[Racers];
            for (int i = 0; i < Racers; i++) order[i] = i;
            Array.Sort(order, Compare);
            return order;
        }

        int Compare(int a, int b)
        {
            if (_totals[a] != _totals[b]) return _totals[b].CompareTo(_totals[a]);
            for (int place = 0; place < Racers; place++)
                if (_placeCounts[a][place] != _placeCounts[b][place]) return _placeCounts[b][place].CompareTo(_placeCounts[a][place]);
            if (_lastPlace[a] != _lastPlace[b]) return _lastPlace[a].CompareTo(_lastPlace[b]);
            return a.CompareTo(b);
        }
    }
}
