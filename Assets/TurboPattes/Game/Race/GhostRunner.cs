using TurboPattes.Core;
using TurboPattes.Game.App;
using TurboPattes.Game.Kart;
using TurboPattes.Game.Visuals;
using UnityEngine;
using UnityEngine.Rendering;

namespace TurboPattes.Game.Race
{
    /// <summary>
    /// Contre-la-montre : enregistre chaque tour du joueur, garde le meilleur (fantôme sauvegardé avec le record)
    /// et le rejoue en kart translucide en même temps que le tour en cours. Tient aussi les records de temps total.
    /// </summary>
    public sealed class GhostRunner : MonoBehaviour
    {
        RaceManager _race;
        int _circuit;
        GhostLap _best, _recording;
        readonly LapSplits _splits = new();
        Transform _ghost;
        bool _finishHandled;

        /// <summary>Records avant la course (0 = aucun), pour l'affichage.</summary>
        public float BestTimeBefore { get; private set; }
        public float BestLapBefore { get; private set; }
        public float BestLap { get; private set; }
        public bool NewTimeRecord { get; private set; }
        public bool NewLapRecord { get; private set; }
        public bool GhostVisible => _ghost != null && _ghost.gameObject.activeSelf;
        public Vector3 GhostPosition => _ghost != null ? _ghost.position : Vector3.zero;
        public int GhostSamples => _best?.Count ?? 0;
        /// <summary>Dernier écart avec le meilleur tour (négatif = en avance) et temps depuis lequel il est affiché.</summary>
        public float Delta { get; private set; }
        public float DeltaAge { get; private set; } = 99f;
        /// <summary>Écarts affichés pendant la course (journal de l'autotest).</summary>
        public int DeltasShown { get; private set; }
        /// <summary>Médaille du meilleur tour avant la course, et maintenant.</summary>
        public Medal MedalBefore { get; private set; }
        public Medal Medal => Medals.For(_circuit, BestLap);
        public int Circuit => _circuit;

        public void Setup(RaceManager race, int circuit)
        {
            _race = race;
            _circuit = circuit;
            BestTimeBefore = GameSession.BestTime(circuit, race.Laps);
            BestLap = BestLapBefore = GameSession.BestLap(circuit);
            MedalBefore = Medals.For(circuit, BestLapBefore);
            _best = BestLap > 0f ? GameSession.LoadGhost(circuit) : null;
            _recording = new GhostLap();
            BuildGhost(race.Player.Spec);
            race.LapCompleted += OnLap;
        }

        void BuildGhost(AnimalSpec spec)
        {
            var rig = KartFactory.Build(spec, transform);
            _ghost = rig.Root;
            rig.Shield.SetActive(false);
            var mat = Mats.Transparent(new Color(0.75f, 0.9f, 1f, 0.38f));
            foreach (var r in _ghost.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            _ghost.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (_race == null || _race.Paused) return;
            var p = _race.Player;
            bool racing = _race.State == RaceManager.Phase.Racing && !p.Finished;
            float t = _race.RaceTime - _race.LapStartTime;
            DeltaAge += Time.deltaTime;
            if (racing)
            {
                // Temps intermédiaires : écart avec ceux du meilleur tour.
                int split = _splits.Update(p.Laps.S / _race.Path.Length, t);
                if (split >= 0 && _best != null && split < _best.Splits.Length) ShowDelta(t - _best.Splits[split]);
                var root = p.Rig.Root;
                var e = root.eulerAngles;
                _recording.Record(t, new GhostPose(root.position.x, root.position.y, root.position.z, e.y, Mathf.DeltaAngle(0f, e.x)));
            }

            GhostPose pose = default;
            bool show = racing && _best != null && _best.Sample(t, out pose);
            if (show) _ghost.SetPositionAndRotation(new Vector3(pose.X, pose.Y, pose.Z), Quaternion.Euler(pose.Pitch, pose.Yaw, 0f));
            if (_ghost.gameObject.activeSelf != show) _ghost.gameObject.SetActive(show);

            if (p.Finished && !_finishHandled)
            {
                _finishHandled = true;
                if (Records.IsBetter(p.FinishTime, BestTimeBefore))
                {
                    NewTimeRecord = true;
                    GameSession.SetBestTime(_circuit, _race.Laps, p.FinishTime);
                }
            }
        }

        void ShowDelta(float delta)
        {
            Delta = delta;
            DeltaAge = 0f;
            DeltasShown++;
        }

        void OnLap(int lap, float time)
        {
            var done = _recording;
            _recording = new GhostLap();
            done.LapTime = time;
            done.Splits = _splits.Count == LapSplits.Points.Length ? (float[])_splits.Times.Clone() : System.Array.Empty<float>();
            _splits.Reset();
            if (BestLap > 0f) ShowDelta(time - BestLap);
            if (!Records.IsBetter(time, BestLap) || done.Count < 10) return;
            BestLap = time;
            _best = done;
            GameSession.SetBestLap(_circuit, time);
            GameSession.SaveGhost(_circuit, done);
            if (BestLapBefore > 0f || lap > 0)
            {
                NewLapRecord = true;
                _race.ShowBanner("Record du tour !", 1.8f);
            }
        }
    }
}
