using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class GameBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.antiAliasing = 2;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Time.fixedDeltaTime = 1f / 60f;

        if (!FindFirstObjectByType<CombatInput>())
            new GameObject("CombatInput").AddComponent<CombatInput>();
        if (!FindFirstObjectByType<GameAudio>())
            new GameObject("GameAudio").AddComponent<GameAudio>();
        if (!FindFirstObjectByType<EventSystem>())
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
        if (!FindFirstObjectByType<ArenaDirector>())
            new GameObject("ArenaDirector").AddComponent<ArenaDirector>();
        if (!FindFirstObjectByType<MobileHud>())
        {
            var hud = FindFirstObjectByType<HUD>();
            if (hud) hud.gameObject.AddComponent<MobileHud>();
            else new GameObject("MobileHud").AddComponent<MobileHud>();
        }
    }
}
