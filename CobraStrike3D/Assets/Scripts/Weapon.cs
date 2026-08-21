using UnityEngine;

public class Weapon : MonoBehaviour
{
    public Camera cam;
    public int magazineSize = 30;
    public float fireRate = 11f;
    public float reloadTime = 0.85f;
    public float damage = 38f;
    public float range = 140f;
    public Transform gunModel;
    public Light muzzleLight;

    public int Ammo { get; private set; }
    float nextShot;
    float reloadEnd;
    Vector3 recoil;
    readonly Vector3 hipPos = new Vector3(0.32f, -0.26f, 0.52f);
    readonly Vector3 adsPos = new Vector3(0.0f, -0.18f, 0.38f);

    void Start()
    {
        Ammo = magazineSize;
        if (gunModel)
        {
            foreach (var col in gunModel.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }
        if (!muzzleLight && gunModel)
        {
            var go = new GameObject("MuzzleLight");
            go.transform.SetParent(gunModel);
            go.transform.localPosition = new Vector3(0f, 0.02f, -0.55f);
            muzzleLight = go.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.range = 8f;
            muzzleLight.color = new Color(1f, 0.75f, 0.3f);
            muzzleLight.intensity = 0f;
        }
    }

    public void ResetAmmo()
    {
        Ammo = magazineSize;
        reloadEnd = 0f;
        HUD.Instance?.SetAmmo(Ammo, false);
    }

    void Update()
    {
        var input = CombatInput.Instance;
        if (!input || HUD.Paused) return;

        bool aiming = input.Ads;
        recoil = Vector3.Lerp(recoil, Vector3.zero, Time.deltaTime * 16f);
        if (gunModel)
            gunModel.localPosition = Vector3.Lerp(gunModel.localPosition, (aiming ? adsPos : hipPos) + recoil, Time.deltaTime * 18f);
        if (cam)
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, aiming ? 52f : 70f, Time.deltaTime * 14f);
        if (muzzleLight)
            muzzleLight.intensity = Mathf.MoveTowards(muzzleLight.intensity, 0f, Time.deltaTime * 40f);

        if (Time.time < reloadEnd) return;

        if (input.Reload || (Ammo <= 0 && input.Fire))
        {
            reloadEnd = Time.time + reloadTime;
            Ammo = magazineSize;
            HUD.Instance?.SetAmmo(Ammo, true);
            GameAudio.Instance?.Reload();
            return;
        }

        float rate = aiming ? fireRate * 0.85f : fireRate;
        if (input.Fire && Time.time >= nextShot && Ammo > 0)
        {
            nextShot = Time.time + 1f / rate;
            Shoot(aiming);
        }
    }

    void Shoot(bool aiming)
    {
        Ammo--;
        HUD.Instance?.SetAmmo(Ammo, false);
        recoil += new Vector3(Random.Range(-0.012f, 0.012f), Random.Range(0.012f, 0.028f), -0.05f);
        if (muzzleLight) muzzleLight.intensity = 5.5f;
        GameAudio.Instance?.Gunshot();

        float spread = aiming ? 0.004f : 0.012f;
        Vector3 dir = cam.transform.forward + cam.transform.right * Random.Range(-spread, spread) + cam.transform.up * Random.Range(-spread, spread * 0.4f);
        Ray ray = new Ray(cam.transform.position, dir.normalized);
        Vector3 muzzlePos = gunModel ? gunModel.TransformPoint(new Vector3(0f, 0.02f, -0.58f)) : ray.origin;
        Vector3 end = ray.origin + ray.direction * range;
        int mask = ~LayerMask.GetMask("UI", "Ignore Raycast");
        if (Physics.Raycast(ray, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
        {
            end = hit.point;
            var enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy)
            {
                enemy.TakeDamage(damage);
                HUD.Instance?.ShowHitMarker();
                GameAudio.Instance?.Hit();
                Effects.Impact(hit.point, new Color(1f, 0.45f, 0.15f), 8, 7f);
            }
            else
            {
                Effects.Impact(hit.point, new Color(0.95f, 0.9f, 0.7f), 4, 3f);
            }
        }
        Effects.Tracer(muzzlePos, end, new Color(1f, 0.85f, 0.25f));
    }
}
