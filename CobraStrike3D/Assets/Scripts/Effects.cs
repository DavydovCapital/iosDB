using UnityEngine;

public static class Effects
{
    public static void Tracer(Vector3 from, Vector3 to, Color color)
    {
        var go = new GameObject("Tracer");
        var lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = color;
        lr.startWidth = 0.03f; lr.endWidth = 0.005f;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        go.AddComponent<AutoDestroy>().lifetime = 0.06f;
    }

    public static void Impact(Vector3 pos, Color color, int count = 8, float speed = 5f)
    {
        for (int i = 0; i < count; i++)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.transform.position = pos;
            p.transform.localScale = Vector3.one * Random.Range(0.04f, 0.1f);
            Object.Destroy(p.GetComponent<Collider>());
            var rb = p.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.velocity = Random.insideUnitSphere * speed + Vector3.up * 2f;
            p.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default")) { color = color };
            p.AddComponent<AutoDestroy>().lifetime = 0.5f + Random.value * 0.4f;
        }
    }

    public static void Smoke(Vector3 pos, int count = 5)
    {
        for (int i = 0; i < count; i++)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.transform.position = pos + Random.insideUnitSphere * 0.3f;
            p.transform.localScale = Vector3.one * Random.Range(0.3f, 0.7f);
            Object.Destroy(p.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.3f, 0.3f, 0.3f, 0.4f) };
            p.GetComponent<Renderer>().material = mat;
            var rb = p.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.velocity = Vector3.up * Random.Range(0.5f, 1.5f) + Random.insideUnitSphere * 0.5f;
            p.AddComponent<AutoDestroy>().lifetime = 1.2f + Random.value;
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
