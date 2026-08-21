using UnityEngine;

public class Weapon : MonoBehaviour
{
    public Camera cam;
    public int magazineSize = 30;
    public float fireRate = 9f;
    public float reloadTime = 1.2f;
    public float damage = 34f;
    public float range = 120f;
    public ParticleSystem muzzleFlash;
    public Transform gunModel;

    public int Ammo { get; private set; }
    private float nextShot;
    private float reloadEnd;
    private Vector3 hipPos = new Vector3(0.35f, -0.28f, 0.55f);
    private Vector3 adsPos = new Vector3(0f, -0.2f, 0.42f);

    void Start() { Ammo = magazineSize; }

    private Vector3 recoil;

    void Update()
    {
        bool aiming = Input.GetMouseButton(1);
        recoil = Vector3.Lerp(recoil, Vector3.zero, Time.deltaTime * 8f);
        if (gunModel) gunModel.localPosition = Vector3.Lerp(gunModel.localPosition, (aiming ? adsPos : hipPos) + recoil, Time.deltaTime * 14f);
        if (cam) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, aiming ? 50f : 68f, Time.deltaTime * 10f);

        if (Time.time < reloadEnd) return;

        if (Input.GetKeyDown(KeyCode.R) || (Ammo <= 0 && Input.GetMouseButtonDown(0)))
        {
            reloadEnd = Time.time + reloadTime;
            Ammo = magazineSize;
            HUD.Instance?.SetAmmo(Ammo, true);
            GameAudio.Instance?.Reload();
            return;
        }

        if (Input.GetMouseButton(0) && Time.time >= nextShot && Ammo > 0)
        {
            nextShot = Time.time + 1f / fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        Ammo--;
        HUD.Instance?.SetAmmo(Ammo, false);
        muzzleFlash?.Play();
        recoil += new Vector3(Random.Range(-0.01f, 0.01f), Random.Range(0.01f, 0.03f), -0.06f);
        GameAudio.Instance?.Gunshot();

        Ray ray = new Ray(cam.transform.position, cam.transform.forward + cam.transform.right * Random.Range(-0.005f, 0.005f));
        Vector3 muzzlePos = gunModel ? gunModel.TransformPoint(new Vector3(0, 0.02f, -0.6f)) : ray.origin;
        Vector3 end = ray.origin + ray.direction * range;
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            end = hit.point;
            var enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy)
            {
                enemy.TakeDamage(damage);
                HUD.Instance?.ShowHitMarker();
                GameAudio.Instance?.Hit();
                Effects.Impact(hit.point, new Color(1f, 0.6f, 0.2f), 10, 6f);
            }
            else
            {
                Effects.Impact(hit.point, new Color(0.9f, 0.85f, 0.7f), 5, 3f);
            }
        }
        Effects.Tracer(muzzlePos, end, new Color(1f, 0.9f, 0.4f));
    }
}
