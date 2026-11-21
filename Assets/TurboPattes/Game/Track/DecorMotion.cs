using TurboPattes.Core;
using TurboPattes.Game.Visuals;
using UnityEngine;

namespace TurboPattes.Game.Track
{
    /// <summary>
    /// Décor animé d'un circuit, posé sur sa racine après la fusion du décor : la lave du volcan palpite
    /// (émission de son matériau, partagé par toutes les coulées fusionnées). Détruit avec le circuit.
    /// </summary>
    public sealed class DecorMotion : MonoBehaviour
    {
        Material _lava;
        Color _lavaEmission;

        public void AnimateLava(Material lava, Color emission)
        {
            _lava = lava;
            _lavaEmission = emission;
        }

        void Update()
        {
            if (_lava != null) _lava.SetColor(Mats.EmissionColorProperty, _lavaEmission * DecorWaves.LavaGlow(Time.time));
        }
    }
}
