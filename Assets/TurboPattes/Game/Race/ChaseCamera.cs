using TurboPattes.Game.Kart;
using UnityEngine;

namespace TurboPattes.Game.Race
{
    /// <summary>
    /// Caméra de poursuite. Pendant le compte à rebours elle passe de face au kart à derrière lui ;
    /// après l'arrivée elle tourne lentement autour.
    /// </summary>
    public sealed class ChaseCamera : MonoBehaviour
    {
        public KartMotor Target;
        /// <summary>0 = vue de face (intro), 1 = poursuite.</summary>
        public float IntroBlend = 1f;
        public bool Orbit;

        Camera _cam;
        float _yaw;
        float _orbitAngle;
        Vector3 _smoothPos;
        bool _init;
        float _shake;

        /// <summary>Secousse (réception d'un saut, choc) : amplitude en mètres, s'amortit en ~0,4 s.</summary>
        public void Shake(float amount) => _shake = Mathf.Max(_shake, amount);

        void Awake() => _cam = GetComponent<Camera>();

        void LateUpdate()
        {
            if (Target == null) return;
            float dt = Time.deltaTime;
            var k = Target;
            float kartYaw = k.Yaw * Mathf.Rad2Deg;
            if (!_init) { _yaw = kartYaw; _init = true; }
            _yaw = Mathf.LerpAngle(_yaw, kartYaw, 1f - Mathf.Exp(-(k.Drift.Active ? 3.5f : 5.5f) * dt));

            Vector3 pivot = k.Position + Vector3.up * 1.1f;
            Vector3 desired;
            Vector3 look;
            if (Orbit)
            {
                _orbitAngle += dt * 20f;
                var rot = Quaternion.Euler(18f, kartYaw + 150f + _orbitAngle, 0f);
                desired = pivot + rot * new Vector3(0f, 0f, -8f);
                look = pivot;
            }
            else
            {
                var behind = Quaternion.Euler(0f, _yaw, 0f);
                Vector3 chase = pivot + behind * new Vector3(0f, 2.1f, -6.6f);
                Vector3 front = pivot + Quaternion.Euler(0f, kartYaw, 0f) * new Vector3(2.2f, 0.6f, 5.5f);
                float t = Mathf.SmoothStep(0f, 1f, IntroBlend);
                desired = Vector3.Lerp(front, chase, t);
                look = Vector3.Lerp(pivot, pivot + behind * new Vector3(0f, 0.2f, 4f), t);
            }

            if (!_init || IntroBlend < 1f || Orbit) _smoothPos = desired;
            else _smoothPos = Vector3.Lerp(_smoothPos, desired, 1f - Mathf.Exp(-14f * dt));
            transform.position = _smoothPos;
            transform.rotation = Quaternion.LookRotation(look - _smoothPos, Vector3.up);
            if (_shake > 0.001f)
            {
                float t = Time.time * 47f;
                transform.position += transform.rotation * new Vector3(Mathf.Sin(t) * _shake, Mathf.Sin(t * 1.37f + 1f) * _shake * 0.7f, 0f);
                _shake = Mathf.MoveTowards(_shake, 0f, dt * 1.2f);
            }

            float speed01 = Mathf.Clamp01(k.Speed / KartMotor.MaxSpeed);
            float fov = 62f + speed01 * 8f + (k.BoostTime > 0f ? 9f : 0f);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, fov, 1f - Mathf.Exp(-4f * dt));
        }
    }
}
