using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public static class SceneBuilder
{
    [MenuItem("Cobra/Build Game Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Lighting
        var sunGO = new GameObject("Sun");
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.3f;
        sun.shadows = LightShadows.Soft;
        sunGO.transform.rotation = Quaternion.Euler(50, -30, 0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.35f, 0.42f, 0.5f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.12f, 0.14f, 0.18f);
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.015f;

        // Ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(20, 1, 20);
        ground.GetComponent<Renderer>().sharedMaterial = MakeMat(new Color(0.16f, 0.17f, 0.19f));

        // Arena cover
        var coverMat = MakeMat(new Color(0.28f, 0.3f, 0.34f));
        for (int i = 0; i < 24; i++)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = "Cover" + i;
            c.transform.position = new Vector3(Random.Range(-35f, 35f), 0.6f, Random.Range(-45f, 15f));
            c.transform.localScale = new Vector3(Random.Range(1.5f, 4f), Random.Range(1f, 2.2f), Random.Range(0.6f, 1.2f));
            c.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            c.GetComponent<Renderer>().sharedMaterial = coverMat;
        }

        // Buildings
        var buildingMat = MakeMat(new Color(0.2f, 0.23f, 0.28f));
        for (int i = 0; i < 10; i++)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = "Building" + i;
            float h = Random.Range(6f, 18f);
            b.transform.position = new Vector3((i % 2 == 0 ? 1 : -1) * Random.Range(30f, 60f), h / 2, Random.Range(-80f, 10f));
            b.transform.localScale = new Vector3(Random.Range(6f, 12f), h, Random.Range(6f, 12f));
            b.GetComponent<Renderer>().sharedMaterial = buildingMat;
        }

        // Player
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0, 1.1f, 12);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.center = new Vector3(0, 0.9f, 0);
        player.AddComponent<PlayerHealth>();
        var fps = player.AddComponent<FPSController>();

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 68f;
        camGO.AddComponent<AudioListener>();
        camGO.transform.SetParent(player.transform);
        camGO.transform.localPosition = new Vector3(0, 1.62f, 0);
        fps.cameraRoot = camGO.transform;

        // Gun model
        var gun = new GameObject("Gun");
        gun.transform.SetParent(camGO.transform);
        gun.transform.localPosition = new Vector3(0.35f, -0.28f, 0.55f);
        var receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
        receiver.transform.SetParent(gun.transform);
        receiver.transform.localPosition = Vector3.zero;
        receiver.transform.localScale = new Vector3(0.16f, 0.12f, 0.5f);
        receiver.GetComponent<Renderer>().sharedMaterial = MakeMat(new Color(0.08f, 0.09f, 0.11f));
        var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.transform.SetParent(gun.transform);
        barrel.transform.localPosition = new Vector3(0, 0.02f, -0.35f);
        barrel.transform.localRotation = Quaternion.Euler(90, 0, 0);
        barrel.transform.localScale = new Vector3(0.035f, 0.25f, 0.035f);
        barrel.GetComponent<Renderer>().sharedMaterial = MakeMat(new Color(0.05f, 0.05f, 0.06f));
        var weapon = player.AddComponent<Weapon>();
        weapon.cam = cam;
        weapon.gunModel = gun.transform;

        // Enemy prefabs (created as prefab assets)
        var gruntPrefab = MakeEnemyPrefab("Grunt", new Color(0.25f, 0.35f, 0.2f), 2.2f);
        var heavyPrefab = MakeEnemyPrefab("Heavy", new Color(0.4f, 0.15f, 0.15f), 1.6f, true);

        // Spawn points
        var spRoot = new GameObject("SpawnPoints");
        var sps = new Transform[6];
        for (int i = 0; i < 6; i++)
        {
            var sp = new GameObject("SP" + i);
            sp.transform.SetParent(spRoot.transform);
            sp.transform.position = new Vector3(-30 + i * 12, 0, -40 - (i % 3) * 15);
            sps[i] = sp.transform;
        }

        // GameManager
        var gmGO = new GameObject("GameManager");
        var gm = gmGO.AddComponent<GameManager>();
        gm.gruntPrefab = gruntPrefab;
        gm.heavyPrefab = heavyPrefab;
        gm.spawnPoints = sps;

        // HUD canvas
        var canvasGO = new GameObject("HUD");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        var hud = canvasGO.AddComponent<HUD>();

        hud.missionText = MakeText(canvasGO.transform, "MissionText", "BLACKSITE DAWN", 16, TextAnchor.UpperLeft, new Vector2(20, -20), new Vector2(0, 1), new Vector2(0, 1));
        hud.killsText = MakeText(canvasGO.transform, "KillsText", "KILLS 0/8", 14, TextAnchor.UpperCenter, new Vector2(0, -20), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        hud.healthText = MakeText(canvasGO.transform, "HealthText", "HP 100", 14, TextAnchor.UpperRight, new Vector2(-20, -20), new Vector2(1, 1), new Vector2(1, 1));
        hud.ammoText = MakeText(canvasGO.transform, "AmmoText", "AMMO 30/30", 14, TextAnchor.LowerRight, new Vector2(-20, 20), new Vector2(1, 0), new Vector2(1, 0));
        hud.scoreText = MakeText(canvasGO.transform, "ScoreText", "000000", 14, TextAnchor.LowerLeft, new Vector2(20, 20), new Vector2(0, 0), new Vector2(0, 0));

        MakeText(canvasGO.transform, "Crosshair", "+", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var hitText = MakeText(canvasGO.transform, "HitMarker", "✕", 26, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        hud.hitMarker = hitText;
        hitText.color = new Color(1f, 0.9f, 0.4f, 1f);
        hitText.enabled = false;

        // Briefing panel
        var brief = MakePanel(canvasGO.transform, "BriefingPanel");
        hud.briefingPanel = brief;
        hud.briefingTitle = MakeText(brief.transform, "Title", "BLACKSITE DAWN", 26, TextAnchor.MiddleCenter, new Vector2(0, 60), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        hud.briefingObjective = MakeText(brief.transform, "Objective", "Breach the outer yard", 14, TextAnchor.MiddleCenter, new Vector2(0, 10), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        hud.deployButton = MakeButton(brief.transform, "DeployButton", "DEPLOY", new Vector2(0, -60));

        // Result panel
        var result = MakePanel(canvasGO.transform, "ResultPanel");
        hud.resultPanel = result;
        hud.resultTitle = MakeText(result.transform, "Title", "MISSION CLEAR", 26, TextAnchor.MiddleCenter, new Vector2(0, 60), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        hud.resultScore = MakeText(result.transform, "Score", "000000", 18, TextAnchor.MiddleCenter, new Vector2(0, 10), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        hud.nextButton = MakeButton(result.transform, "NextButton", "NEXT MISSION", new Vector2(0, -60));
        hud.restartButton = MakeButton(result.transform, "RestartButton", "RESTART", new Vector2(0, -120));
        result.SetActive(false);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
        Debug.Log("Cobra Strike 3D scene built successfully.");
    }

    static Material MakeMat(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        m.color = c;
        return m;
    }

    static GameObject MakeEnemyPrefab(string name, Color color, float speed, bool heavy = false)
    {
        var root = new GameObject(name);
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 1f, 0);
        body.GetComponent<Renderer>().sharedMaterial = MakeMat(color);
        var e = root.AddComponent<Enemy>();
        e.moveSpeed = speed;
        e.hp = heavy ? 160f : 100f;
        e.damage = heavy ? 16 : 12;
        string path = $"Assets/{name}.prefab";
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
