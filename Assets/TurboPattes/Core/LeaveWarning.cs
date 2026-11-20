namespace TurboPattes.Core
{
    /// <summary>
    /// Confirmation avant « Recommencer » ou « Menu » dans la pause : une question et la perte qu'elle entraîne,
    /// nommée (« Quitter la Coupe Braise ? Les 2 courses jouées seront perdues. »).
    /// </summary>
    public static class LeaveWarning
    {
        /// <param name="toMenu">Retour au menu (sinon : recommencer la course).</param>
        /// <param name="cupName">Coupe en cours (null hors coupe).</param>
        public static string Question(bool toMenu, string cupName) =>
            !toMenu ? "Recommencer la course ?" : cupName != null ? $"Quitter la {cupName} ?" : "Revenir au menu ?";

        /// <param name="racesDone">Courses de la coupe déjà terminées.</param>
        public static string Loss(bool toMenu, string cupName, int racesDone, bool timeTrial)
        {
            if (!toMenu)
                return cupName != null ? "La course repart de zéro, sans compter ses points."
                    : timeTrial ? "Le chrono repart de zéro." : "La course repart de zéro.";
            if (cupName == null) return timeTrial ? "Ce chrono ne sera pas enregistré." : "La course en cours sera abandonnée.";
            if (racesDone <= 0) return "La coupe en cours sera perdue.";
            return racesDone == 1 ? "La course jouée sera perdue." : $"Les {racesDone} courses jouées seront perdues.";
        }

        /// <summary>Libellé du bouton qui confirme.</summary>
        public static string Confirm(bool toMenu) => toMenu ? "Quitter" : "Recommencer";
    }
}
