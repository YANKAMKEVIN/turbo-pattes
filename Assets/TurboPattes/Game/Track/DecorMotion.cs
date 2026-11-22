using System.Collections.Generic;
using TurboPattes.Core;
using TurboPattes.Game.Visuals;
using UnityEngine;

namespace TurboPattes.Game.Track
{
    /// <summary>
    /// Décor animé d'un circuit, posé sur sa racine après la fusion du décor : la lave du volcan palpite
    /// (émission de son matériau, partagé par toutes les coulées fusionnées), la houle soulève le plan d'eau du lagon
    /// (sommets déplacés, normales analytiques) et les nuages de la Route des Nuages dérivent au vent.
    /// Les objets animés sont construits après la fusion (sinon figés dans un maillage commun). Détruit avec le circuit.
    /// Calculs de mouvement : <see cref="DecorWaves"/>.
    /// </summary>
    public sealed class DecorMotion : MonoBehaviour
    {
        /// <summary>Direction du vent qui pousse les nuages (plan horizontal).</summary>
        static readonly Vector3 Wind = new Vector3(1f, 0f, 0.35f).normalized;

        Material _lava;
        Color _lavaEmission;

        Mesh _water;
        Vector3[] _waterRest, _waterVerts, _waterNormals;
        float[] _waterEdge;

        readonly List<(Transform group, Vector3 rest, float reach, float phase)> _clouds = new();

        public void AnimateLava(Material lava, Color emission)
        {
            _lava = lava;
            _lavaEmission = emission;
        }

        /// <summary>
        /// Anime un plan d'eau maillé : <paramref name="rest"/> = sommets au repos (repère du maillage, y = 0),
        /// <paramref name="edge"/> = part de houle gardée par sommet (<see cref="DecorWaves.WaterEdge"/>).
        /// </summary>
        public void AnimateWater(Mesh water, Vector3[] rest, float[] edge)
        {
            _water = water;
            _waterRest = rest;
            _waterEdge = edge;
            _waterVerts = (Vector3[])rest.Clone();
            _waterNormals = new Vector3[rest.Length];
            UpdateWater(Time.time);
        }

        /// <summary>Fait dériver un groupe de nuages autour de sa place (<paramref name="reach"/> = <see cref="DecorWaves.CloudDriftReach"/>).</summary>
        public void AddCloud(Transform group, float reach, float phase) =>
            _clouds.Add((group, group.localPosition, reach, phase));

        void Update()
        {
            float t = Time.time;
            if (_lava != null) _lava.SetColor(Mats.EmissionColorProperty, _lavaEmission * DecorWaves.LavaGlow(t));
            // Houle lente : une mise à jour toutes les deux images suffit et divise le coût par deux.
            if (_water != null && (Time.frameCount & 1) == 0) UpdateWater(t);
            foreach (var (group, rest, reach, phase) in _clouds)
            {
                if (group == null) continue;
                group.localPosition = rest + Wind * (reach * DecorWaves.CloudDrift(t, phase)) + Vector3.up * DecorWaves.CloudBob(t, phase);
            }
        }

        void UpdateWater(float t)
        {
            for (int i = 0; i < _waterRest.Length; i++)
            {
                var p = _waterRest[i];
                float w = _waterEdge[i];
                if (w <= 0f)
                {
                    _waterVerts[i] = p;
                    _waterNormals[i] = Vector3.up;
                    continue;
                }
                var (dx, dz) = DecorWaves.WaterSlope(p.x, p.z, t);
                _waterVerts[i] = new Vector3(p.x, w * DecorWaves.WaterHeight(p.x, p.z, t), p.z);
                _waterNormals[i] = new Vector3(-dx * w, 1f, -dz * w).normalized;
            }
            _water.vertices = _waterVerts;
            _water.normals = _waterNormals;
        }
    }
}
