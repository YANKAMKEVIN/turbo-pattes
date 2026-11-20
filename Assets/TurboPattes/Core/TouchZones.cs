namespace TurboPattes.Core
{
    /// <summary>Bouton tactile rond : centre et rayon en pixels écran (origine en bas à gauche).</summary>
    public readonly struct TouchSpot
    {
        public readonly float X, Y, R;

        public TouchSpot(float x, float y, float r)
        {
            X = x;
            Y = y;
            R = r;
        }

        public bool Contains(float x, float y, float slack = 1f) => (x - X) * (x - X) + (y - Y) * (y - Y) <= R * slack * R * slack;
    }

    /// <summary>
    /// Disposition des commandes tactiles : zone de glissement pour tourner d'un côté, boutons DÉRAPAGE, OBJET et
    /// FREIN de l'autre. Droitier : direction à gauche, boutons à droite ; gaucher : les deux moitiés échangées,
    /// en miroir dans la zone sûre de l'écran.
    /// </summary>
    public sealed class TouchZones
    {
        public float SteerMinX, SteerMaxX, SteerRange;
        /// <summary>Où dessiner le repère « glisser » quand personne ne touche la zone.</summary>
        public float SteerHintX;
        public TouchSpot Drift, Item, Brake;
        public bool LeftHanded;

        public bool InSteerZone(float x) => x >= SteerMinX && x <= SteerMaxX;

        public static TouchZones Compute(float width, float height, float safeXMin, float safeXMax, float safeYMin, float sensitivity, bool leftHanded)
        {
            float margin = height * 0.05f;
            float driftR = height * 0.13f;
            float dx = safeXMax - margin - driftR, dy = safeYMin + margin + driftR;
            float itemX = dx - driftR * 0.25f, itemY = dy + driftR + height * 0.14f;
            float brakeX = dx - driftR - height * 0.16f, brakeY = dy - driftR * 0.35f;
            // Plus sensible = moins de glisser pour braquer à fond (réglages).
            float range = height * 0.09f / sensitivity;
            var z = new TouchZones { SteerRange = range, LeftHanded = leftHanded };
            float Mirror(float x) => leftHanded ? safeXMin + safeXMax - x : x;
            z.Drift = new TouchSpot(Mirror(dx), dy, driftR);
            z.Item = new TouchSpot(Mirror(itemX), itemY, height * 0.095f);
            z.Brake = new TouchSpot(Mirror(brakeX), brakeY, height * 0.075f);
            if (leftHanded)
            {
                z.SteerMinX = width * 0.55f;
                z.SteerMaxX = width;
                z.SteerHintX = safeXMax - range * 2f;
            }
            else
            {
                z.SteerMinX = 0f;
                z.SteerMaxX = width * 0.45f;
                z.SteerHintX = safeXMin + range * 2f;
            }
            return z;
        }
    }
}
