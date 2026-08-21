using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float hp = 90f;
    public float moveSpeed = 4.4f;
    public float fireRange = 28f;
    public float fireInterval = 1.05f;
    public int damage = 10;
    public int scoreValue = 100;
    public bool heavy;

    Transform player;
    CharacterController cc;
    float nextShot;
    float strafe = 1f;
    float flash;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        cc = GetComponent<CharacterController>();
        if (!cc)
        {
            cc = gameObject.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.38f;
            cc.center = new Vector3(0f, 0.9f, 0f);
        }
        strafe = Random.value > 0.5f ? 1f : -1f;
    }

    void Update()
    {
        if (!player || HUD.Paused) return;
        Vector3 to = player.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist > 0.01f)
        {
            Quaternion face = Quaternion.LookRotation(to);
            transform.rotation = Quaternion.Slerp(transform.rotation, face, 12f * Time.deltaTime);
        }

        Vector3 wish = Vector3.zero;
        if (dist > 9f) wish += to.normalized * moveSpeed;
        else if (dist < 6f) wish -= to.normalized * moveSpeed * 0.4f;
        wish += transform.right * strafe * moveSpeed * 0.45f;
        if (Random.value < 0.012f) strafe = -strafe;
        cc.Move((wish + Vector3.down * 12f) * Time.deltaTime);

        if (dist < fireRange && Time.time >= nextShot)
        {
            nextShot = Time.time + fireInterval + Random.Range(0f, 0.25f);
            Vector3 muzzle = transform.position + Vector3.up * 1.35f + transform.forward * 0.6f;
            Vector3 target = player.position + Vector3.up * 1.4f + Random.insideUnitSphere * 0.55f;
            Effects.Tracer(muzzle, target, new Color(1f, 0.25f, 0.2f));
            GameAudio.Instance?.EnemyShot();
            if (Random.value < 0.72f)
                PlayerHealth.Instance?.TakeDamage(damage);
        }

        if (flash > 0f)
        {
            flash -= Time.deltaTime;
            if (flash <= 0f) SetTint(heavy ? new Color(0.45f, 0.12f, 0.1f) : new Color(0.22f, 0.32f, 0.16f));
        }
    }

    public void TakeDamage(float amount)
    {
        hp -= amount;
        flash = 0.06f;
        SetTint(Color.white);
        if (hp <= 0f)
        {
            GameAudio.Instance?.Kill();
            Effects.Impact(transform.position + Vector3.up, new Color(0.85f, 0.12f, 0.08f), 12, 7f);
            Effects.Smoke(transform.position + Vector3.up, 3);
            GameManager.Instance?.EnemyKilled(scoreValue);
            Destroy(gameObject);
        }
    }

    void SetTint(Color c)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r.material.HasProperty("_Color")) r.material.color = c;
        }
    }
}
