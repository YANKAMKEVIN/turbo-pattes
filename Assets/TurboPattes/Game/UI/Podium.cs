using TurboPattes.Core;
using TurboPattes.Game.App;
using TurboPattes.Game.Audio;
using TurboPattes.Game.Kart;
using TurboPattes.Game.Track;
using TurboPattes.Game.Visuals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TurboPattes.Game.UI
{
    /// <summary>
    /// Fin de coupe : les trois premiers du classement cumulé sur un podium posé sur la piste, confettis,
    /// classement complet avec les points, puis retour au menu.
    /// </summary>
    public sealed class Podium : MonoBehaviour
    {
        static readonly float[] BlockHeights = { 1.6f, 1.1f, 0.75f };
        static readonly float[] BlockX = { 0f, -3.4f, 3.4f };
        static readonly Color[] BlockColors = { new(1f, 0.8f, 0.2f), new(0.8f, 0.85f, 0.92f), new(0.85f, 0.5f, 0.25f) };

        readonly UiKit _ui = new();
        CupRun _cup;
        int[] _ranking;
        Camera _cam;
        Transform _stage;
        readonly Transform[] _karts = new Transform[3];
        Transform[] _confetti;
        Vector3[] _confettiSpin;
        float _time;
        int _trophy;
        bool _newTrophy, _unlocked;
        static readonly string[] TrophyNames = { "", "Trophée de bronze", "Trophée d'argent", "Trophée d'or" };

        public void Setup(CupRun cup, TrackPath path, Camera cam)
        {
            _cup = cup;
            _ranking = cup.Standings.Ranking();
            _trophy = Cups.TrophyFor(System.Array.IndexOf(_ranking, cup.Player));
            int before = GameSession.CupTrophy(cup.CupIndex);
            _newTrophy = GameSession.AwardTrophy(cup.CupIndex, _trophy);
            // Premier podium dans cette coupe : la suivante vient de s'ouvrir.
            _unlocked = before == 0 && _trophy > 0 && cup.CupIndex + 1 < Cups.All.Length;
            _cam = cam;
            var chase = cam.GetComponent<Race.ChaseCamera>();
            if (chase != null) chase.enabled = false;

            // Podium posé sur la piste, avant la ligne d'arrivée, face à la caméra.
            path.Sample(path.Length - 30f, out var pos, out var tangent, out _);
            _stage = new GameObject("PodiumStage").transform;
            _stage.SetParent(transform, false);
            _stage.SetPositionAndRotation(pos, Quaternion.LookRotation(-tangent));
            for (int i = 0; i < 3; i++)
            {
                float h = BlockHeights[i];
                MeshKit.Prim(PrimitiveType.Cube, _stage, new Vector3(BlockX[i], h * 0.5f, 0f), new Vector3(3.2f, h, 3.2f), Mats.Lit(BlockColors[i], 0.5f, 0.3f));
                MeshKit.Prim(PrimitiveType.Cube, _stage, new Vector3(BlockX[i], h * 0.55f, 1.62f), new Vector3(0.9f, 0.6f, 0.05f), Mats.Lit(Color.white));
                var spec = AnimalSpec.Roster[_ranking[i]];
                var rig = KartFactory.Build(spec, _stage);
                rig.Shield.SetActive(false);
                rig.Root.localPosition = new Vector3(BlockX[i], h, 0f);
                rig.Root.localRotation = Quaternion.Euler(0f, 180f + (i == 0 ? 0f : i == 1 ? 18f : -18f), 0f);
                _karts[i] = rig.Root;
            }
            // Le reste du peloton au pied du podium.
            for (int r = 3; r < _ranking.Length; r++)
            {
                var rig = KartFactory.Build(AnimalSpec.Roster[_ranking[r]], _stage);
                rig.Shield.SetActive(false);
                float x = (r - 3 - (_ranking.Length - 4) * 0.5f) * 3.1f;
                rig.Root.localPosition = new Vector3(x, 0f, -5.5f);
                rig.Root.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            BuildConfetti();

            _cam.transform.position = _stage.TransformPoint(new Vector3(0f, 4.2f, 13.5f));
            _cam.transform.rotation = Quaternion.LookRotation(_stage.TransformPoint(new Vector3(0f, 1.6f, 0f)) - _cam.transform.position);
            _cam.fieldOfView = 45f;
            Jukebox.Instance.Play(Songs.Podium, 0.55f);
        }

        void BuildConfetti()
        {
            Color[] colors = { new(1f, 0.3f, 0.35f), new(1f, 0.85f, 0.2f), new(0.3f, 0.75f, 1f), new(0.45f, 0.95f, 0.4f), new(0.9f, 0.45f, 1f) };
            var rng = new System.Random(5);
            _confetti = new Transform[140];
            _confettiSpin = new Vector3[_confetti.Length];
            for (int i = 0; i < _confetti.Length; i++)
            {
                var go = MeshKit.Prim(PrimitiveType.Cube, _stage, Vector3.zero, new Vector3(0.22f, 0.02f, 0.14f), Mats.Lit(colors[i % colors.Length]));
                go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                _confetti[i] = go.transform;
                _confetti[i].localPosition = new Vector3((float)rng.NextDouble() * 14f - 7f, (float)rng.NextDouble() * 9f, (float)rng.NextDouble() * 6f - 3f);
                _confettiSpin[i] = new Vector3((float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 180f);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _time += dt;
            for (int i = 0; i < _confetti.Length; i++)
            {
                var c = _confetti[i];
                var p = c.localPosition;
                p.y -= dt * (1.2f + (i % 5) * 0.15f);
                p.x += Mathf.Sin(_time * 2f + i) * dt * 0.6f;
                if (p.y < 0f) p.y += 9f;
                c.localPosition = p;
                c.localRotation *= Quaternion.Euler(_confettiSpin[i] * dt);
            }
            // Le vainqueur sautille.
            float hop = Mathf.Abs(Mathf.Sin(_time * 3f)) * 0.35f;
            _karts[0].localPosition = new Vector3(BlockX[0], BlockHeights[0] + hop, 0f);

            var kb = Keyboard.current;
            var pad = Gamepad.current;
            bool confirm = kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
                           || pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
            var tap = UiKit.Tap();
            if (_time > 1f && (confirm || tap.HasValue && MenuButton().Contains(tap.Value))) BackToMenu();
        }

        static Rect MenuButton()
        {
            float s = Screen.height / 720f;
            return new Rect(Screen.width * 0.5f - 150 * s, Screen.height - Screen.safeArea.yMin - 100 * s, 300 * s, 70 * s);
        }

        void BackToMenu()
        {
            Jukebox.Instance.Confirm();
            GameSession.Cup = null;
            GameSession.ShowMenu = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnGUI()
        {
            if (_cup == null) return;
            _ui.EnsureStyles();
            float s = _ui.Scale, w = Screen.width;
            var safe = Screen.safeArea;
            float top = Screen.height - safe.yMax + 16 * s;
            // Titre et verdict sur une fiche crème ; noms au-dessus des marches sur des plaques.
            var head = new Rect(w * 0.5f - 330 * s, top - 6 * s, 660 * s, (_unlocked ? 172 : _trophy > 0 ? 142 : 112) * s);
            UiKit.Card(head, UiKit.Cream, 26 * s, 5 * s);
            _ui.Label(new Rect(0, top, w, 70 * s), _cup.Def.Name, _ui.Big, UiKit.Ink, TextAnchor.MiddleCenter);
            int playerPlace = System.Array.IndexOf(_ranking, _cup.Player);
            string verdict = playerPlace == 0 ? "Tu remportes la coupe !" : playerPlace < 3 ? $"Tu montes sur le podium ({Hud.Ordinal(playerPlace + 1)}) !" : $"Tu finis {Hud.Ordinal(playerPlace + 1)} de la coupe";
            _ui.Label(new Rect(0, top + 66 * s, w, 40 * s), verdict, _ui.Medium, UiKit.Vermilion, TextAnchor.MiddleCenter);
            if (_trophy > 0)
                _ui.Label(new Rect(0, top + 104 * s, w, 32 * s), TrophyNames[_trophy] + (_newTrophy ? " gagné !" : ""), _ui.Small, UiKit.Ink, TextAnchor.MiddleCenter);
            if (_unlocked)
                _ui.Label(new Rect(0, top + 134 * s, w, 32 * s), $"Nouvelle coupe ouverte : {Cups.All[_cup.CupIndex + 1].Name}", _ui.Small,
                    UiKit.Blue, TextAnchor.MiddleCenter);

            // Nom et points au-dessus de chaque marche.
            for (int i = 0; i < 3; i++)
            {
                var sp = _cam.WorldToScreenPoint(_karts[i].position + Vector3.up * 2.6f);
                if (sp.z <= 0f) continue;
                var spec = AnimalSpec.Roster[_ranking[i]];
                var r = new Rect(sp.x - 110 * s, Screen.height - sp.y - 54 * s, 220 * s, 60 * s);
                UiKit.Card(r, i == 0 ? UiKit.Vermilion : UiKit.Alpha(UiKit.Ink, 0.88f), 14 * s);
                _ui.Label(new Rect(r.x, r.y + 2 * s, r.width, 32 * s), $"{Hud.Ordinal(i + 1)}  {spec.Name}", _ui.Medium, UiKit.Cream, TextAnchor.MiddleCenter);
                _ui.Label(new Rect(r.x, r.y + 32 * s, r.width, 26 * s), $"{_cup.Standings.Total(_ranking[i])} pts", _ui.Small, UiKit.Ochre, TextAnchor.MiddleCenter);
            }

            // Classement complet, à droite.
            float row = 30 * s, pw = 250 * s, px = safe.xMax - 24 * s - pw, py = top + 180 * s;
            UiKit.Card(new Rect(px - 10 * s, py - 10 * s, pw + 20 * s, row * _ranking.Length + 20 * s), UiKit.Cream, 18 * s, 4 * s);
            for (int r = 0; r < _ranking.Length; r++)
            {
                var spec = AnimalSpec.Roster[_ranking[r]];
                float y = py + r * row;
                bool me = _ranking[r] == _cup.Player;
                var ink = me ? UiKit.Cream : UiKit.Ink;
                if (me) UiKit.Box(new Rect(px - 10 * s, y, pw + 20 * s, row), UiKit.Vermilion);
                _ui.Label(new Rect(px, y, 40 * s, row), (r + 1).ToString(), _ui.Small, ink, TextAnchor.MiddleLeft);
                UiKit.Portrait(new Vector2(px + 52 * s, y + row * 0.5f), 12 * s, spec);
                _ui.Label(new Rect(px + 72 * s, y, 110 * s, row), spec.Name, _ui.Small, ink, TextAnchor.MiddleLeft);
                _ui.Label(new Rect(px + pw - 80 * s, y, 80 * s, row), $"{_cup.Standings.Total(_ranking[r])} pts", _ui.Small, ink, TextAnchor.MiddleRight);
            }

            _ui.Button(MenuButton(), "Menu", true, true);
        }
    }
}
