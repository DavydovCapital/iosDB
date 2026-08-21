using UnityEngine;

public class ArenaDirector : MonoBehaviour
{
    public static ArenaDirector Instance { get; private set; }

    Transform root;
    Transform[] spawns = new Transform[8];
    Light sun;

    static readonly Color[] Fog = {
        new Color(0.42f, 0.48f, 0.52f),
        new Color(0.18f, 0.22f, 0.26f),
        new Color(0.38f, 0.28f, 0.18f),
        new Color(0.10f, 0.12f, 0.16f),
        new Color(0.12f, 0.05f, 0.07f),
    };
    static readonly Color[] SunCol = {
        new Color(1f, 0.86f, 0.62f),
        new Color(0.72f, 0.82f, 0.95f),
        new Color(1f, 0.55f, 0.28f),
        new Color(0.55f, 0.7f, 1f),
        new Color(1f, 0.35f, 0.28f),
    };

    void Awake()
    {
        Instance = this;
        if (!sun)
        {
            sun = FindFirstObjectByType<Light>();
            if (!sun)
            {
                var go = new GameObject("Sun");
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                go.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            }
        }
        BuildCity();
    }

    public void ApplyMission(int index)
    {
        index = Mathf.Clamp(index, 0, Fog.Length - 1);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = Fog[index];
        RenderSettings.fogDensity = 0.012f + index * 0.0025f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Fog[index] * 1.4f;
        RenderSettings.ambientEquatorColor = Fog[index] * 0.6f;
        RenderSettings.ambientGroundColor = Color.black;
        if (Camera.main) Camera.main.backgroundColor = Fog[index];
        if (sun)
        {
            sun.color = SunCol[index];
            sun.intensity = 1.15f + index * 0.08f;
            sun.transform.rotation = Quaternion.Euler(38f + index * 4f, -40f + index * 8f, 0f);
        }
    }

    public Transform[] SpawnPoints => spawns;

    void BuildCity()
    {
        if (root) Destroy(root.gameObject);
        root = new GameObject("ArenaRoot").transform;

        Ground();
        Road();
        Buildings();
        Cover();
        Props();
        Lamps();
        Spawns();
    }

    void Ground()
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
        g.name = "Asphalt";
        g.transform.SetParent(root);
        g.transform.localScale = new Vector3(28, 1, 28);
        SetMat(g, new Color(0.09f, 0.10f, 0.11f), 0.15f, 0.35f);
    }

    void Road()
    {
        var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.transform.SetParent(root);
        road.transform.position = new Vector3(0f, 0.02f, -10f);
        road.transform.localScale = new Vector3(12f, 0.05f, 90f);
        SetMat(road, new Color(0.07f, 0.07f, 0.08f), 0.05f, 0.2f);
        StripCol(road);
        for (int z = -40; z <= 30; z += 6)
        {
            var dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dash.transform.SetParent(root);
            dash.transform.position = new Vector3(0f, 0.05f, z);
            dash.transform.localScale = new Vector3(0.18f, 0.02f, 2.2f);
            SetUnlit(dash, new Color(0.92f, 0.86f, 0.35f));
            StripCol(dash);
        }
    }

    void Buildings()
    {
        Vector3[] spots =
        {
            new Vector3(-28, 0, -8), new Vector3(30, 0, -6), new Vector3(-32, 0, -28),
            new Vector3(34, 0, -32), new Vector3(-26, 0, 12), new Vector3(28, 0, 16),
            new Vector3(-38, 0, -50), new Vector3(40, 0, -48), new Vector3(-22, 0, -62),
            new Vector3(24, 0, -70), new Vector3(-40, 0, 4), new Vector3(42, 0, -12)
        };
        for (int i = 0; i < spots.Length; i++)
        {
            float h = 8f + (i * 5 % 16);
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.transform.SetParent(root);
            b.transform.position = spots[i] + Vector3.up * (h * 0.5f);
            b.transform.localScale = new Vector3(8f + i % 4, h, 8f + (i * 3) % 5);
            SetMat(b, i % 2 == 0 ? new Color(0.16f, 0.18f, 0.21f) : new Color(0.13f, 0.14f, 0.16f), 0.25f, 0.4f);
            for (int y = 2; y < h - 1; y += 2)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w.transform.SetParent(root);
                float side = spots[i].x < 0 ? 1f : -1f;
                w.transform.position = new Vector3(spots[i].x + side * 4.05f, y, spots[i].z);
                w.transform.localScale = new Vector3(0.12f, 0.55f, 5.5f);
                SetUnlit(w, (y + i) % 3 == 0 ? new Color(1f, 0.82f, 0.4f) : new Color(0.35f, 0.7f, 1f) * 0.6f);
                StripCol(w);
            }
        }
    }

    void Cover()
    {
        Vector3[] crates =
        {
            new Vector3(-6, 0.7f, 2), new Vector3(7, 0.7f, -4), new Vector3(-10, 0.55f, -12),
            new Vector3(9, 0.55f, -18), new Vector3(-4, 0.7f, -24), new Vector3(5, 0.9f, -30),
            new Vector3(-12, 0.6f, -36), new Vector3(11, 0.6f, -8), new Vector3(0, 0.5f, -42),
            new Vector3(-8, 0.8f, 8), new Vector3(8, 0.8f, 6), new Vector3(-14, 0.5f, -20)
        };
        foreach (var p in crates)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.SetParent(root);
            c.transform.position = p;
            c.transform.localScale = new Vector3(1.8f, p.y * 2f, 1.1f);
            c.transform.rotation = Quaternion.Euler(0f, p.x * 12f, 0f);
            SetMat(c, new Color(0.28f, 0.22f, 0.14f), 0.1f, 0.25f);
        }
        for (int i = 0; i < 6; i++)
        {
            var bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bag.transform.SetParent(root);
            bag.transform.position = new Vector3(-3f + i * 1.15f, 0.35f, -6f);
            bag.transform.localScale = new Vector3(1.2f, 0.7f, 0.7f);
            SetMat(bag, new Color(0.32f, 0.34f, 0.22f), 0.05f, 0.15f);
        }
    }

    void Props()
    {
        for (int i = 0; i < 8; i++)
        {
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.transform.SetParent(root);
            barrel.transform.position = new Vector3((i % 2 == 0 ? -1 : 1) * (14f + i), 0.6f, -10f - i * 5f);
            barrel.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
            SetMat(barrel, new Color(0.45f, 0.12f, 0.08f), 0.6f, 0.5f);
        }
        var truck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        truck.transform.SetParent(root);
        truck.transform.position = new Vector3(16f, 1.2f, -22f);
        truck.transform.localScale = new Vector3(3.2f, 2.4f, 8f);
        SetMat(truck, new Color(0.12f, 0.22f, 0.14f), 0.4f, 0.35f);
    }

    void Lamps()
    {
        for (int i = 0; i < 10; i++)
        {
            float z = 10f - i * 9f;
            float x = i % 2 == 0 ? -7.5f : 7.5f;
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(root);
            pole.transform.position = new Vector3(x, 2.4f, z);
            pole.transform.localScale = new Vector3(0.12f, 2.4f, 0.12f);
            SetMat(pole, new Color(0.08f, 0.08f, 0.09f), 0.7f, 0.6f);
            StripCol(pole);
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.transform.SetParent(root);
            lamp.transform.position = new Vector3(x, 4.7f, z);
            lamp.transform.localScale = Vector3.one * 0.35f;
            SetUnlit(lamp, new Color(1f, 0.9f, 0.65f));
            StripCol(lamp);
            var pl = lamp.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.range = 14f;
            pl.intensity = 1.6f;
            pl.color = new Color(1f, 0.88f, 0.6f);
        }
    }

    void Spawns()
    {
        Vector3[] pts =
        {
            new Vector3(-18, 0, -28), new Vector3(18, 0, -26), new Vector3(0, 0, -48),
            new Vector3(-22, 0, -40), new Vector3(22, 0, -44), new Vector3(-8, 0, -58),
            new Vector3(8, 0, -56), new Vector3(0, 0, -34)
        };
        var holder = new GameObject("SpawnPoints").transform;
        holder.SetParent(root);
        for (int i = 0; i < pts.Length; i++)
        {
            var t = new GameObject("SP" + i).transform;
            t.SetParent(holder);
            t.position = pts[i];
            spawns[i] = t;
        }
        if (GameManager.Instance) GameManager.Instance.spawnPoints = spawns;
    }

    public static void SetMat(GameObject go, Color c, float metallic, float gloss)
    {
        var r = go.GetComponent<Renderer>();
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Glossiness", gloss);
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        r.receiveShadows = true;
    }

    public static void SetUnlit(GameObject go, Color c)
    {
        var r = go.GetComponent<Renderer>();
        var sh = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        var m = new Material(sh);
        m.color = c;
        r.sharedMaterial = m;
    }

    public static void StripCol(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>())
        {
            if (Application.isPlaying) Object.Destroy(c);
            else Object.DestroyImmediate(c);
        }
    }
}
