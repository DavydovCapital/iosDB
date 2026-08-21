using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class SceneBuilder
{
    [MenuItem("Cobra/Build Game Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");

        PlayerSettings.companyName = "DavydovCapital";
        PlayerSettings.productName = "Cobra Strike";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.davydovcapital.cobrastrike3d");

        var sunGO = new GameObject("Sun");
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.35f;
        sun.shadows = LightShadows.Soft;
        sun.color = new Color(1f, 0.86f, 0.62f);
        sunGO.transform.rotation = Quaternion.Euler(42, -35, 0);

        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0, 0.1f, 10);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0, 0.9f, 0);
        player.AddComponent<PlayerHealth>();
        var fps = player.AddComponent<FPSController>();

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 220f;
        cam.allowHDR = true;
        camGO.AddComponent<AudioListener>();
        camGO.transform.SetParent(player.transform);
        camGO.transform.localPosition = new Vector3(0, 1.62f, 0);
        fps.cameraRoot = camGO.transform;

        var gun = BuildGun(camGO.transform);
        var weapon = player.AddComponent<Weapon>();
        weapon.cam = cam;
        weapon.gunModel = gun;

        var gruntPrefab = MakeEnemyPrefab("Grunt", false);
        var heavyPrefab = MakeEnemyPrefab("Heavy", true);

        var gmGO = new GameObject("GameManager");
        var gm = gmGO.AddComponent<GameManager>();
        gm.gruntPrefab = gruntPrefab;
        gm.heavyPrefab = heavyPrefab;

        new GameObject("ArenaDirector").AddComponent<ArenaDirector>();
        new GameObject("CombatInput").AddComponent<CombatInput>();
        new GameObject("GameAudio").AddComponent<GameAudio>();

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif

        var canvasGO = new GameObject("HUD");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        var hud = canvasGO.AddComponent<HUD>();
        canvasGO.AddComponent<MobileHud>();

        hud.missionText = MakeText(canvasGO.transform, "MissionText", "BLACKSITE DAWN", 18, TextAnchor.UpperLeft, new Vector2(24, -18), new Vector2(0, 1), new Vector2(0, 1));
        hud.killsText = MakeText(canvasGO.transform, "KillsText", "KILLS 0/10", 16, TextAnchor.UpperCenter, new Vector2(0, -18), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        hud.healthText = MakeText(canvasGO.transform, "HealthText", "HP 100", 16, TextAnchor.UpperRight, new Vector2(-24, -18), new Vector2(1, 1), new Vector2(1, 1));
        hud.ammoText = MakeText(canvasGO.transform, "AmmoText", "AMMO 30/30", 16, TextAnchor.LowerCenter, new Vector2(0, 24), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        hud.scoreText = MakeText(canvasGO.transform, "ScoreText", "000000", 16, TextAnchor.LowerLeft, new Vector2(24, 24), new Vector2(0, 0), new Vector2(0, 0));
        MakeText(canvasGO.transform, "Crosshair", "+", 28, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var hitText = MakeText(canvasGO.transform, "HitMarker", "✕", 30, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        hud.hitMarker = hitText;
        hitText.color = new Color(1f, 0.9f, 0.35f, 1f);
        hitText.enabled = false;

        var brief = MakePanel(canvasGO.transform, "BriefingPanel");
        hud.briefingPanel = brief;
        hud.briefingTitle = MakeText(brief.transform, "Title", "BLACKSITE DAWN", 28, TextAnchor.MiddleCenter, new Vector2(0, 70), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        hud.briefingObjective = MakeText(brief.transform, "Objective", "Breach the outer yard", 16, TextAnchor.MiddleCenter, new Vector2(0, 16), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        hud.deployButton = MakeButton(brief.transform, "DeployButton", "DEPLOY", new Vector2(0, -70));

        var result = MakePanel(canvasGO.transform, "ResultPanel");
        hud.resultPanel = result;
        hud.resultTitle = MakeText(result.transform, "Title", "MISSION CLEAR", 28, TextAnchor.MiddleCenter, new Vector2(0, 70), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        hud.resultScore = MakeText(result.transform, "Score", "000000", 20, TextAnchor.MiddleCenter, new Vector2(0, 16), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        hud.nextButton = MakeButton(result.transform, "NextButton", "NEXT MISSION", new Vector2(0, -60));
        hud.restartButton = MakeButton(result.transform, "RestartButton", "RESTART", new Vector2(0, -120));
        result.SetActive(false);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Cobra Strike 3D scene built.");
    }

    [MenuItem("Cobra/Build Scene And iOS")]
    public static void BuildSceneAndiOS()
    {
        Build();
        GameBuilder.BuildiOS();
    }

    static Transform BuildGun(Transform cam)
    {
        var gun = new GameObject("Gun").transform;
        gun.SetParent(cam);
        gun.localPosition = new Vector3(0.32f, -0.26f, 0.52f);
        gun.localRotation = Quaternion.identity;
        void Part(PrimitiveType type, Vector3 pos, Vector3 scale, Quaternion rot, Color col, float metal, float gloss)
        {
            var p = GameObject.CreatePrimitive(type);
            p.transform.SetParent(gun);
            p.transform.localPosition = pos;
            p.transform.localRotation = rot;
            p.transform.localScale = scale;
            var m = new Material(Shader.Find("Standard"));
            m.color = col;
            m.SetFloat("_Metallic", metal);
            m.SetFloat("_Glossiness", gloss);
            p.GetComponent<Renderer>().sharedMaterial = m;
            Object.DestroyImmediate(p.GetComponent<Collider>());
        }
        Part(PrimitiveType.Cube, Vector3.zero, new Vector3(0.18f, 0.14f, 0.55f), Quaternion.identity, new Color(0.07f, 0.08f, 0.09f), 0.85f, 0.55f);
        Part(PrimitiveType.Cylinder, new Vector3(0f, 0.02f, -0.42f), new Vector3(0.04f, 0.28f, 0.04f), Quaternion.Euler(90, 0, 0), new Color(0.04f, 0.04f, 0.05f), 0.9f, 0.7f);
        Part(PrimitiveType.Cube, new Vector3(0f, -0.16f, 0.08f), new Vector3(0.08f, 0.22f, 0.14f), Quaternion.identity, new Color(0.08f, 0.08f, 0.08f), 0.4f, 0.25f);
        Part(PrimitiveType.Cube, new Vector3(0f, 0.1f, 0.05f), new Vector3(0.04f, 0.08f, 0.16f), Quaternion.identity, new Color(0.15f, 0.8f, 1f), 0.2f, 0.8f);
        return gun;
    }

    static Material MakeMat(Color c)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        m.SetFloat("_Metallic", 0.3f);
        m.SetFloat("_Glossiness", 0.4f);
        return m;
    }

    static GameObject MakeEnemyPrefab(string name, bool heavy)
    {
        var root = EnemyFactory.Create(heavy);
        root.name = name;
        string path = $"Assets/Prefabs/{name}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static Text MakeText(Transform parent, string name, string content, int size, TextAnchor anchor, Vector2 pos, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.raycastTarget = false;
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 60);
        return t;
    }

    static GameObject MakePanel(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.02f, 0.03f, 0.06f, 0.9f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520, 320);
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    static Button MakeButton(Transform parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200, 44);
        rt.anchoredPosition = pos;
        var txt = MakeText(go.transform, "Label", label, 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        txt.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 44);
        return btn;
    }
}
