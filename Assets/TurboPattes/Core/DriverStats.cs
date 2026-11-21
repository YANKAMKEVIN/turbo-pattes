using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Caractère d'un pilote, de 1 à 5 sur quatre axes ; tous ont le même total pour rester équilibrés.
    /// Les écarts restent petits (quelques pour cent) : la conduite compte plus que le pilote choisi.
    /// </summary>
    public readonly struct DriverStats
    {
        public const int Total = 12;

        public readonly int Speed, Accel, Grip, Weight;

        public DriverStats(int speed, int accel, int grip, int weight)
        {
            Speed = speed;
            Accel = accel;
            Grip = grip;
            Weight = weight;
        }

        public int Sum => Speed + Accel + Grip + Weight;

        /// <summary>Vitesse de pointe : ±3 % entre les extrêmes.</summary>
        public float TopSpeedFactor => 1f + (Speed - 3) * 0.015f;
        /// <summary>Accélération : ±16 %.</summary>
        public float AccelFactor => 1f + (Accel - 3) * 0.08f;
        /// <summary>Braquage et tenue hors piste : ±12 %.</summary>
        public float GripFactor => 1f + (Grip - 3) * 0.06f;
        /// <summary>Masse relative dans les chocs entre karts.</summary>
        public float Mass => 0.7f + Weight * 0.1f;

        /// <summary>Part de la poussée encaissée par A dans un choc avec B (le plus lourd est moins bousculé).</summary>
        public static float PushShare(float massA, float massB) => massB / (massA + massB);
    }

    /// <summary>Caractère des huit pilotes, dans l'ordre de la grille de choix (identités dans <see cref="Roster"/>, au même index).</summary>
    public static class Drivers
    {
        public static readonly DriverStats[] Stats =
        {
            new(3, 3, 3, 3), // Alex Moreau : équilibré
            new(3, 2, 2, 5), // Viktor Brandt : lourd, inébranlable
            new(2, 4, 4, 2), // Lina Okafor : agile
            new(2, 5, 3, 2), // Sami Haddad : démarrages fulgurants
            new(4, 1, 2, 5), // Teo Ferraz : pointe et poids, lent à lancer
            new(3, 3, 4, 2), // Hana Mori : précise
            new(4, 3, 2, 3), // Nora Lindqvist : glisse vite
            new(3, 2, 3, 4), // Inès Duval : solide
        };

        public static DriverStats For(int index) => index >= 0 && index < Stats.Length ? Stats[index] : Stats[0];
    }
}
