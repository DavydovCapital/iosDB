using UnityEngine;

[System.Serializable]
public class MissionDef
{
    public string missionName;
    public string objective;
    public int enemyCount;
    public int heavyEvery;
    public float enemySpeed = 2.4f;
    public int enemyDamage = 12;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public MissionDef[] missions = {
        new MissionDef { missionName = "BLACKSITE DAWN", objective = "Breach the outer yard", enemyCount = 8, heavyEvery = 0, enemySpeed = 2.2f, enemyDamage = 10 },
        new MissionDef { missionName = "IRON HARBOR", objective = "Sweep the loading docks", enemyCount = 11, heavyEvery = 4, enemySpeed = 2.4f, enemyDamage = 12 },
        new MissionDef { missionName = "ASH RIDGE", objective = "Clear the fortified ridge", enemyCount = 14, heavyEvery = 3, enemySpeed = 2.6f, enemyDamage = 14 },
        new MissionDef { missionName = "DEAD GRID", objective = "Push through the city block", enemyCount = 17, heavyEvery = 3, enemySpeed = 2.8f, enemyDamage = 16 },
        new MissionDef { missionName = "COMMAND CORE", objective = "Destroy the final guard", enemyCount = 22, heavyEvery = 2, enemySpeed = 3.0f, enemyDamage = 18 },
    };

    public GameObject gruntPrefab;
    public GameObject heavyPrefab;
    public Transform[] spawnPoints;

    public int Mission { get; private set; } = 0;
    public int Kills { get; private set; }
    public int Score { get; private set; }

    private int spawned;
    private int alive;

    void Awake() { Instance = this; }

    void Start() { StartMission(0); }

    public void StartMission(int index)
    {
        Mission = index;
        Kills = 0;
        Score += index > 0 ? 1000 : 0;
        spawned = 0;
        alive = 0;
        PlayerHealth.Instance?.ResetHealth();
        HUD.Instance?.ShowBriefing(missions[index].missionName, missions[index].objective, missions[index].enemyCount);
    }

    public void BeginCombat()
    {
        HUD.Instance?.HideBriefing();
        for (int i = 0; i < 3; i++) SpawnEnemy();
    }

    void SpawnEnemy()
    {
        var m = missions[Mission];
        if (spawned >= m.enemyCount || spawnPoints.Length == 0) return;
        var sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        bool heavy = m.heavyEvery > 0 && spawned % m.heavyEvery == m.heavyEvery - 1;
        var prefab = heavy && heavyPrefab ? heavyPrefab : gruntPrefab;
        if (!prefab) return;
        var go = Instantiate(prefab, sp.position, sp.rotation);
        var e = go.GetComponent<Enemy>();
        if (e) { e.moveSpeed = m.enemySpeed * (heavy ? 0.7f : 1f); e.damage = m.enemyDamage; if (heavy) e.hp = 160f; e.scoreValue = heavy ? 300 : 100; }
        spawned++; alive++;
    }

    public void EnemyKilled(int scoreValue)
    {
        Kills++; Score += scoreValue; alive--;
        HUD.Instance?.SetKills(Kills, missions[Mission].enemyCount);
        HUD.Instance?.SetScore(Score);
        if (Kills >= missions[Mission].enemyCount)
        {
            if (Mission >= missions.Length - 1) HUD.Instance?.ShowWon(Score);
            else HUD.Instance?.ShowClear(missions[Mission].missionName, Score);
        }
        else SpawnEnemy();
    }

    public void NextMission() { StartMission(Mission + 1); }
    public void Restart() { Score = 0; StartMission(0); }
    public void PlayerDied() { HUD.Instance?.ShowLost(Score); }
}
