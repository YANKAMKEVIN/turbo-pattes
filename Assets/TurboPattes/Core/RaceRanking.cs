using System.Collections.Generic;

namespace TurboPattes.Core
{
    public readonly struct RacerStanding
    {
        public readonly bool Finished;
        public readonly float FinishTime;
        public readonly float Progress;

        public RacerStanding(bool finished, float finishTime, float progress)
        {
            Finished = finished;
            FinishTime = finishTime;
            Progress = progress;
        }
    }

    public static class RaceRanking
    {
        /// <summary>
        /// Ordre de classement (indices des pilotes) : arrivés d'abord par temps, puis les autres
        /// par distance parcourue. À égalité, l'ordre d'origine est conservé.
        /// </summary>
        public static int[] Order(IReadOnlyList<RacerStanding> racers)
        {
            var order = new int[racers.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            // Tri par insertion : stable, et 8 pilotes au plus.
            for (int i = 1; i < order.Length; i++)
            {
                int cur = order[i];
                int j = i - 1;
                while (j >= 0 && Before(racers[cur], racers[order[j]]))
                {
                    order[j + 1] = order[j];
                    j--;
                }
                order[j + 1] = cur;
            }
            return order;
        }

        static bool Before(RacerStanding a, RacerStanding b)
        {
            if (a.Finished != b.Finished) return a.Finished;
            if (a.Finished) return a.FinishTime < b.FinishTime;
            return a.Progress > b.Progress;
        }
    }
}
