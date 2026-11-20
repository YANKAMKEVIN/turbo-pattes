using TurboPattes.Core;
using TurboPattes.Game.App;
using TurboPattes.Game.Audio;
using TurboPattes.Game.Race;
using UnityEngine;

namespace TurboPattes.Game.UI
{
    /// <summary>
    /// Conseils pendant la toute première course : super départ, direction, drift, objets, figures. Chacun s'affiche
    /// une fois, au moment où il sert, puis le tutoriel est marqué comme vu à l'arrivée (<see cref="GameSession.TutorialDone"/>).
    /// </summary>
    public sealed class Tutorial : MonoBehaviour
    {
        const float ShowTime = 3.8f;
        const float Gap = 1.5f;

        public RaceManager Race;
        /// <summary>Conseils affichés (journal de l'autotest).</summary>
        public int Shown { get; private set; }

        readonly UiKit _ui = new();
        readonly bool[] _done = new bool[5];
        string _text;
        float _left, _cooldown;
        int _tip = -1, _tricksAtShow;
        bool _achieved;

        void Update()
        {
            if (Race == null || Race.Player == null || Race.Paused) return;
            float dt = Time.deltaTime;
            if (_left > 0f)
            {
                // Geste réussi : le conseil s'efface vite, avec un son de validation.
                var pl = Race.Player;
                if (!_achieved && TutorialCues.Achieved(_tip, Race.LastPlayerInput.Steer, pl.Drift.Active ? pl.Drift.Level : 0,
                        pl.Item != ItemKind.None, pl.Tricks - _tricksAtShow))
                {
                    _achieved = true;
                    _left = Mathf.Min(_left, 0.35f);
                    Jukebox.Instance.Confirm();
                }
                _left -= dt;
                return;
            }
            if (_cooldown > 0f) { _cooldown -= dt; return; }
            var p = Race.Player;
            bool touch = Race.Input.TouchMode;

            if (Race.State == RaceManager.Phase.Results)
            {
                GameSession.TutorialDone = true;
                enabled = false;
                return;
            }
            if (Race.State == RaceManager.Phase.Countdown)
            {
                if (Race.CountdownLeft < 3.2f)
                    Show(0, touch ? "L'accélération est automatique : prêt pour le départ !" : "Accélère dans la dernière seconde : super départ !");
                return;
            }
            if (Race.RaceTime > 1.5f)
                Show(1, touch ? "Glisse le doigt à gauche de l'écran pour tourner" : "Flèches ou stick gauche pour tourner");
            if (Race.RaceTime > 6f && Race.Path.TurnBetween(p.Laps.S, p.Laps.S + 45f) > 45f && p.Speed > 12f)
                Show(2, $"Maintiens {(touch ? "DÉRAPAGE" : "Espace")} en tournant : les étincelles chargent un turbo");
            if (p.Item != ItemKind.None && p.ItemRoll <= 0f)
                Show(3, $"{(touch ? "OBJET" : "E")} pour utiliser ton objet");
            if (p.Airborne && p.Vertical.AirTime > 0.1f)
                Show(4, $"{(touch ? "DÉRAPAGE" : "Espace")} en l'air : figure et turbo à l'atterrissage");
        }

        void Show(int index, string text)
        {
            if (_done[index] || _left > 0f) return;
            _done[index] = true;
            _tip = index;
            _achieved = false;
            _tricksAtShow = Race.Player.Tricks;
            _text = text;
            _left = ShowTime;
            _cooldown = Gap;
            Shown++;
        }

        void OnGUI()
        {
            if (_left <= 0f || string.IsNullOrEmpty(_text) || Race == null || Race.Paused || Race.State == RaceManager.Phase.Results) return;
            _ui.EnsureStyles();
            GUI.depth = -1; // par-dessus le HUD
            float s = _ui.Scale, w = Screen.width, h = Screen.height;
            float a = Mathf.Clamp01(_left * 3f) * Mathf.Clamp01((ShowTime - _left) * 4f);
            if (Race.Input.TouchMode && !_achieved) DrawPointer(h, s, a);
            var r = new Rect(w * 0.5f - 420 * s, h * 0.7f, 840 * s, 58 * s);
            _ui.Plate(r, a);
            UiKit.Slash(new Rect(r.x + 14 * s, r.y + r.height * 0.2f, 8 * s, r.height * 0.6f), UiKit.Alpha(UiKit.Ochre, a));
            _ui.Label(r, _text, _ui.Medium, UiKit.Alpha(UiKit.Cream, a), TextAnchor.MiddleCenter);
        }

        /// <summary>Anneau ocre qui pulse autour de la commande tactile visée par le conseil.</summary>
        void DrawPointer(float h, float s, float a)
        {
            var l = Race.Input.Layout;
            Vector2 c;
            float r;
            switch (TutorialCues.Target(_tip))
            {
                case TutorialTarget.Steer: c = new Vector2(l.SteerHintX, h * 0.78f); r = l.SteerRange * 1.2f; break;
                case TutorialTarget.Drift: c = new Vector2(l.Drift.x, h - l.Drift.y); r = l.DriftR; break;
                case TutorialTarget.Item: c = new Vector2(l.Item.x, h - l.Item.y); r = l.ItemR; break;
                default: return;
            }
            float pulse = TutorialCues.Pulse(Time.time);
            UiKit.Ring(c, r * pulse + 6 * s, 10 * s, UiKit.Alpha(UiKit.Ink, 0.6f * a), 56);
            UiKit.Ring(c, r * pulse + 6 * s, 7 * s, UiKit.Alpha(UiKit.Ochre, a), 56);
        }
    }
}
