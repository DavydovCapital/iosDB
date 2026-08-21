using UnityEngine;

[System.Serializable]
public class MissionDef
{
    public string missionName;
    public string objective;
    public int enemyCount;
    public int heavyEvery;
    public float enemySpeed = 4.4f;
    public int enemyDamage = 10;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public MissionDef[] missions = {
        new MissionDef { missionName = "BLACKSITE DAWN", objective = "Breach the outer yard", enemyCount = 10, heavyEvery = 0, enemySpeed = 4.2f, enemyDamage = 8 },
        new MissionDef { missionName = "IRON HARBOR", objective = "Sweep the loading docks", enemyCount = 13, heavyEvery = 4, enemySpeed = 4.5f, enemyDamage = 10 },
        new MissionDef { missionName = "ASH RIDGE", objective = "Clear the fortified ridge", enemyCount = 16, heavyEvery = 3, enemySpeed = 4.8f, enemyDamage = 11 },
        new MissionDef { missionName = "DEAD GRID", objective = "Push through the city block", enemyCount = 19, heavyEvery = 3, enemySpeed = 5.1f, enemyDamage = 12 },
        new MissionDef { missionName = "COMMAND CORE", objective = "Destroy the final guard", enemyCount = 24, heavyEvery = 2, enemySpeed = 5.4f, enemyDamage = 13 },
    };

    public GameObject gruntPrefab;
    public GameObject heavyPrefab;
    public Transform[] spawnPoints;

    public int Mission { get; private set; }
    public int Kills { get; private set; }
    public int Score { get; private set; }
    public bool Combat { get; private set; }

    int spawned;
    int alive;

    void Awake() { Instance = this; }

    void Start()
    {
        if (ArenaDirector.Instance && (spawnPoints == null || spawnPoints.Length == 0))
            spawnPoints = ArenaDirector.Instance.SpawnPoints;
        StartMission(0);
    }

    public void StartMission(int index)
    {
        Mission = index;
        Kills = 0;
        spawned = 0;
        alive = 0;
        Combat = false;
        ClearEnemies();
        PlayerHealth.Instance?.ResetHealth();
        FindFirstObjectByType<Weapon>()?.ResetAmmo();
        ArenaDirector.Instance?.ApplyMission(index);
        HUD.Paused = true;
        HUD.Instance?.ShowBriefing(missions[index].missionName, missions[index].objective, missions[index].enemyCount);
        GameAudio.Instance?.StopMusic();
    }

    public void BeginCombat()
    {
        HUD.Paused = false;
        Combat = true;
        HUD.Instance?.HideBriefing();
        HUD.Instance?.SetKills(0, missions[Mission].enemyCount);
        GameAudio.Instance?.StartMusic();
        int wave = Mathf.Min(4, missions[Mission].enemyCount);
        for (int i = 0; i < wave; i++) SpawnEnemy();
    }

    void SpawnEnemy()
    {
        var m = missions[Mission];
        if (spawned >= m.enemyCount) return;
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            if (ArenaDirector.Instance) spawnPoints = ArenaDirector.Instance.SpawnPoints;
            if (spawnPoints == null || spawnPoints.Length == 0) return;
        }
        var sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        bool heavy = m.heavyEvery > 0 && spawned % m.heavyEvery == m.heavyEvery - 1;
        GameObject prefab = heavy && heavyPrefab ? heavyPrefab : gruntPrefab;
        if (!prefab)
        {
            prefab = EnemyFactory.Create(heavy);
        }
        var go = Instantiate(prefab, sp.position, sp.rotation);
        var e = go.GetComponent<Enemy>();
        if (e)
        {
            e.heavy = heavy;
            e.moveSpeed = m.enemySpeed * (heavy ? 0.72f : 1f);
            e.damage = m.enemyDamage + (heavy ? 4 : 0);
            e.hp = heavy ? 170f : 85f;
            e.scoreValue = heavy ? 300 : 100;
            e.fireInterval = heavy ? 0.75f : 1.05f;
        }
        spawned++;
        alive++;
    }

    public void EnemyKilled(int scoreValue)
    {
        Kills++; Score += scoreValue; alive = Mathf.Max(0, alive - 1);
        HUD.Instance?.SetKills(Kills, missions[Mission].enemyCount);
        HUD.Instance?.SetScore(Score);
        if (Kills >= missions[Mission].enemyCount)
        {
            Combat = false;
            HUD.Paused = true;
            GameAudio.Instance?.StopMusic();
            if (Mission >= missions.Length - 1) HUD.Instance?.ShowWon(Score);
            else HUD.Instance?.ShowClear(missions[Mission].missionName, Score);
        }
        else SpawnEnemy();
    }

    public void NextMission()
    {
        Score += 1000;
        StartMission(Mission + 1);
    }

    public void Restart()
    {
        Score = 0;
        StartMission(0);
    }

    public void PlayerDied()
    {
        Combat = false;
        HUD.Paused = true;
        GameAudio.Instance?.StopMusic();
        HUD.Instance?.ShowLost(Score);
    }

    void ClearEnemies()
    {
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            Destroy(e.gameObject);
    }
}

public static class EnemyFactory
{
    public static GameObject Create(bool heavy)
    {
        var root = new GameObject(heavy ? "Heavy" : "Grunt");
        Color body = heavy ? new Color(0.42f, 0.12f, 0.1f) : new Color(0.2f, 0.32f, 0.16f);
        var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cap.transform.SetParent(root.transform);
        cap.transform.localPosition = new Vector3(0f, 1f, 0f);
        ArenaDirector.SetMat(cap, body, 0.35f, 0.4f);
        var vest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vest.transform.SetParent(root.transform);
        vest.transform.localPosition = new Vector3(0f, 1.15f, 0.05f);
        vest.transform.localScale = new Vector3(0.85f, 0.55f, 0.4f);
        ArenaDirector.SetMat(vest, new Color(0.08f, 0.09f, 0.1f), 0.5f, 0.25f);
        ArenaDirector.StripCol(vest);
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0f, 1.85f, 0f);
        head.transform.localScale = Vector3.one * 0.48f;
        ArenaDirector.SetMat(head, new Color(0.18f, 0.18f, 0.2f), 0.6f, 0.55f);
        ArenaDirector.StripCol(head);
        var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visor.transform.SetParent(root.transform);
        visor.transform.localPosition = new Vector3(0f, 1.88f, 0.18f);
        visor.transform.localScale = new Vector3(0.38f, 0.1f, 0.08f);
        ArenaDirector.SetUnlit(visor, heavy ? new Color(1f, 0.4f, 0.1f) : new Color(0.2f, 1f, 0.55f));
        ArenaDirector.StripCol(visor);
        var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gun.transform.SetParent(root.transform);
        gun.transform.localPosition = new Vector3(0.38f, 1.2f, 0.45f);
        gun.transform.localScale = new Vector3(0.08f, 0.08f, 0.9f);
        ArenaDirector.SetMat(gun, new Color(0.05f, 0.05f, 0.06f), 0.8f, 0.6f);
        ArenaDirector.StripCol(gun);
        var e = root.AddComponent<Enemy>();
        e.heavy = heavy;
        return root;
    }
}
