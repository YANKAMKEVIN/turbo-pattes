using UnityEngine;

namespace TurboPattes.Game.App
{
    /// <summary>
    /// Vibrations courtes sur téléphone (chocs, réception, arrivée), coupées dans les réglages. Sur Android on passe par
    /// le Vibrator du système pour des durées de quelques dizaines de millisecondes (<see cref="Handheld.Vibrate"/> dure
    /// trop longtemps, mais sa présence dans le code ajoute la permission VIBRATE au manifeste).
    /// </summary>
    public static class Haptics
    {
        const float MinGap = 0.08f;
        static float _last = -1f;
#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _vibrator;
        static bool _failed;
#endif

        public static void Pulse(int milliseconds)
        {
            if (!GameSession.Vibration || !Application.isMobilePlatform) return;
            if (Time.unscaledTime - _last < MinGap) return;
            _last = Time.unscaledTime;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_failed) return;
            try
            {
                if (_vibrator == null)
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                if (milliseconds >= 400) Handheld.Vibrate();
                else _vibrator.Call("vibrate", (long)milliseconds);
            }
            catch (System.Exception e)
            {
                _failed = true;
                Debug.LogWarning($"[TurboPattes] Vibreur indisponible : {e.Message}");
            }
#elif UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
