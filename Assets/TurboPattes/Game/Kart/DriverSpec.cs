using TurboPattes.Core;
using UnityEngine;

namespace TurboPattes.Game.Kart
{
    /// <summary>
    /// Un pilote humain et son kart, prêts pour Unity : identité et couleurs d'équipe lues dans <see cref="Core.Roster"/>
    /// (données testées hors moteur), converties en <see cref="Color"/>.
    /// </summary>
    public sealed class DriverSpec
    {
        public DriverProfile Profile;
        /// <summary>Prénom affiché dans les menus et le HUD.</summary>
        public string Name;
        public string Code;
        public int Number;
        /// <summary>Casque et carénage du kart.</summary>
        public Color Kart;
        /// <summary>Bande du casque et liserés du carénage.</summary>
        public Color KartTrim;
        public Color Suit;

        public static readonly DriverSpec[] Roster = Build();

        static DriverSpec[] Build()
        {
            var all = new DriverSpec[Core.Roster.All.Length];
            for (int i = 0; i < all.Length; i++)
            {
                var p = Core.Roster.All[i];
                all[i] = new DriverSpec
                {
                    Profile = p, Name = p.FirstName, Code = p.Code, Number = p.Number,
                    Kart = Of(p.Primary), KartTrim = Of(p.Secondary), Suit = Of(p.Suit),
                };
            }
            return all;
        }

        static Color Of(Rgb c) => new(c.R, c.G, c.B);
    }
}
