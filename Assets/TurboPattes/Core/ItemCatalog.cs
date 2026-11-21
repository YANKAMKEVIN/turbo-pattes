namespace TurboPattes.Core
{
    /// <summary>Ce que fait un objet en jeu (indépendant de son habillage).</summary>
    public enum ItemMechanic
    {
        None,
        /// <summary>Accélération immédiate.</summary>
        InstantBoost,
        /// <summary>Projectile lancé devant, roule tout droit, rebondit sur les murets, fait tourner le kart touché.</summary>
        StraightBouncer,
        /// <summary>Piège posé derrière soi, fait tourner le kart qui roule dessus.</summary>
        DroppedTrap,
        /// <summary>Absorbe un coup pendant quelques secondes.</summary>
        TimedShield,
        /// <summary>Grand bond plané avec élan, par-dessus les objets au sol.</summary>
        GlideLeap,
        /// <summary>Projectile qui suit la piste jusqu'au kart juste devant.</summary>
        TrackHoming,
        /// <summary>Objet rare des derniers : turbo en pilote automatique, intouchable, renverse les karts percutés.</summary>
        RareAutopilot,
    }

    /// <summary>
    /// Habillage « sport mécanique » des objets (docs/design/da-arcade-sportif.md, § 7) : nom affiché et description.
    /// Les identifiants <see cref="ItemKind"/> sont gardés (ils nomment une mécanique, utilisée par le tirage, l'IA et
    /// les effets) : seul ce qui est montré au joueur change.
    /// </summary>
    public static class ItemCatalog
    {
        /// <summary>Nom affiché dans le HUD.</summary>
        public static string Name(ItemKind kind) => kind switch
        {
            ItemKind.Carrot => "Nitro",
            ItemKind.Coconut => "Pneu fou",
            ItemKind.Honey => "Flaque d'huile",
            ItemKind.Shield => "Bouclier carbone",
            ItemKind.Feather => "Aileron",
            ItemKind.Yarn => "Drone traqueur",
            ItemKind.Comet => "Aspiration",
            _ => "",
        };

        /// <summary>Mécanique de l'objet : la même qu'avant la refonte, pour chaque identifiant.</summary>
        public static ItemMechanic Mechanic(ItemKind kind) => kind switch
        {
            ItemKind.Carrot => ItemMechanic.InstantBoost,
            ItemKind.Coconut => ItemMechanic.StraightBouncer,
            ItemKind.Honey => ItemMechanic.DroppedTrap,
            ItemKind.Shield => ItemMechanic.TimedShield,
            ItemKind.Feather => ItemMechanic.GlideLeap,
            ItemKind.Yarn => ItemMechanic.TrackHoming,
            ItemKind.Comet => ItemMechanic.RareAutopilot,
            _ => ItemMechanic.None,
        };

        /// <summary>Description courte (aide, planche d'icônes).</summary>
        public static string Description(ItemKind kind) => kind switch
        {
            ItemKind.Carrot => "Bouteille de nitro : accélération immédiate.",
            ItemKind.Coconut => "Pneu lancé devant : roule tout droit et rebondit sur les murets.",
            ItemKind.Honey => "Bidon renversé derrière soi : le kart qui passe dessus part en tête-à-queue.",
            ItemKind.Shield => "Enveloppe carbone : encaisse un coup pendant quelques secondes.",
            ItemKind.Feather => "Grand bond plané, par-dessus les pièges au sol.",
            ItemKind.Yarn => "Suit la piste jusqu'au kart juste devant.",
            ItemKind.Comet => "Rare, pour les derniers : turbo en pilote automatique, intouchable.",
            _ => "",
        };
    }
}
