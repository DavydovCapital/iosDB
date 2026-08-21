using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float hp = 100f;
    public float moveSpeed = 2.4f;
    public float fireRange = 26f;
    public float fireInterval = 1.4f;
    public int damage = 12;
    public int scoreValue = 100;

    private Transform player;
    private float nextShot;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (!player) return;
        Vector3 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;

        Vector3 look = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(look);

        if (dist > fireRange * 0.7f)
            transform.position += toPlayer.normalized * moveSpeed * Time.deltaTime;

        if (dist < fireRange && Time.time >= nextShot)
        {
            nextShot = Time.time + fireInterval + Random.value * 0.5f;
            Vector3 muzzle = transform.position + Vector3.up * 1.4f + transform.forward * 0.5f;
            Vector3 target = player.position + Vector3.up * 1.4f + Random.insideUnitSphere * 0.4f;
            Effects.Tracer(muzzle, target, new Color(1f, 0.3f, 0.3f));
            Effects.Impact(muzzle, new Color(1f, 0.7f, 0.3f), 3, 1f);
            GameAudio.Instance?.EnemyShot();
            PlayerHealth.Instance?.TakeDamage(damage);
        }
    }

    public void TakeDamage(float amount)
    {
        hp -= amount;
        // hit flash
        foreach (var r in GetComponentsInChildren<Renderer>())
            StartCoroutine(Flash(r));
        if (hp <= 0)
        {
            GameAudio.Instance?.Kill();
            Effects.Impact(transform.position + Vector3.up * 1f, new Color(0.8f, 0.1f, 0.1f), 14, 6f);
            Effects.Smoke(transform.position + Vector3.up * 1f, 4);
            GameManager.Instance?.EnemyKilled(scoreValue);
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator Flash(Renderer r)
    {
        var mat = r.material;
        var orig = mat.color;
        mat.color = Color.white;
        yield return new WaitForSeconds(0.06f);
        if (mat) mat.color = orig;
    }
}
