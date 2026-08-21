using UnityEngine;

public static class Effects
{
    static Material tracerMat;
    static Material sparkMat;

    static Material Mat(Color c)
    {
        if (!sparkMat) sparkMat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
        var m = new Material(sparkMat) { color = c };
        return m;
    }

    public static void Tracer(Vector3 from, Vector3 to, Color color)
    {
        var go = new GameObject("Tracer");
        var lr = go.AddComponent<LineRenderer>();
        if (!tracerMat) tracerMat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
        lr.material = tracerMat;
        lr.startColor = color;
        lr.endColor = new Color(color.r, color.g, color.b, 0.1f);
        lr.startWidth = 0.045f;
        lr.endWidth = 0.006f;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<AutoDestroy>().lifetime = 0.05f;
    }

    public static void Impact(Vector3 pos, Color color, int count = 8, float speed = 5f)
    {
        count = Mathf.Clamp(count, 1, 10);
        for (int i = 0; i < count; i++)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.transform.position = pos;
            p.transform.localScale = Vector3.one * Random.Range(0.03f, 0.08f);
            Object.Destroy(p.GetComponent<Collider>());
            var rb = p.AddComponent<Rigidbody>();
            rb.linearVelocity = Random.insideUnitSphere * speed + Vector3.up * 2f;
            p.GetComponent<Renderer>().sharedMaterial = Mat(color);
            p.AddComponent<AutoDestroy>().lifetime = 0.28f + Random.value * 0.2f;
        }
    }

    public static void Smoke(Vector3 pos, int count = 5)
    {
        count = Mathf.Clamp(count, 1, 4);
        for (int i = 0; i < count; i++)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.transform.position = pos + Random.insideUnitSphere * 0.25f;
            p.transform.localScale = Vector3.one * Random.Range(0.25f, 0.55f);
            Object.Destroy(p.GetComponent<Collider>());
            p.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.25f, 0.25f, 0.25f, 0.35f));
            var rb = p.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearVelocity = Vector3.up * Random.Range(0.4f, 1.2f);
            p.AddComponent<AutoDestroy>().lifetime = 0.7f;
        }
    }
}

public class AutoDestroy : MonoBehaviour
{
    public float lifetime = 1f;
    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0) Destroy(gameObject);
    }
}
