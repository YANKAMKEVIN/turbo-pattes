namespace TurboPattes.Game.Kart
{
    /// <summary>Commandes d'un kart pour une image, quelle que soit leur source (joueur ou IA).</summary>
    public struct KartInput
    {
        /// <summary>-1 (gauche) à +1 (droite).</summary>
        public float Steer;
        public float Throttle;
        public float Brake;
        /// <summary>Bouton de drift maintenu.</summary>
        public bool Drift;
        /// <summary>Utiliser l'objet (vrai sur une seule image).</summary>
        public bool UseItem;
    }
}
