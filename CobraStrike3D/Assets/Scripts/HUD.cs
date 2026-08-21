using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public static HUD Instance;
    public static bool Paused = true;

    public Text missionText;
    public Text killsText;
    public Text healthText;
    public Text ammoText;
    public Text scoreText;
    public GameObject briefingPanel;
    public Text briefingTitle;
    public Text briefingObjective;
    public GameObject resultPanel;
    public Text resultTitle;
    public Text resultScore;
    public Button deployButton;
    public Button nextButton;
    public Button restartButton;
    public Graphic hitMarker;

    private float hitTimer;

    void Awake() { Instance = this; }

    void Start()
    {
        deployButton?.onClick.AddListener(() => GameManager.Instance?.BeginCombat());
        nextButton?.onClick.AddListener(() => { resultPanel.SetActive(false); GameManager.Instance?.NextMission(); });
        restartButton?.onClick.AddListener(() => { resultPanel.SetActive(false); GameManager.Instance?.Restart(); });
        if (hitMarker) hitMarker.enabled = false;
    }

    void Update()
    {
        if (hitMarker && hitMarker.enabled)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0) hitMarker.enabled = false;
        }
    }

    public void SetKills(int k, int total) { if (killsText) killsText.text = $"KILLS {k}/{total}"; }
    public void SetHealth(int h) { if (healthText) healthText.text = $"HP {h}"; }
    public void SetAmmo(int a, bool reloading) { if (ammoText) ammoText.text = reloading ? "RELOADING" : $"AMMO {a}/30"; }
    public void SetScore(int s) { if (scoreText) scoreText.text = s.ToString("D6"); }
    public void ShowHitMarker() { if (hitMarker) { hitMarker.enabled = true; hitTimer = 0.08f; } }

    private float dmgTimer;
    public Image damageVignette;
    public void DamageFlash() { dmgTimer = 0.25f; if (damageVignette) damageVignette.enabled = true; }

    void LateUpdate()
    {
        if (dmgTimer > 0)
        {
            dmgTimer -= Time.deltaTime;
            if (dmgTimer <= 0 && damageVignette) damageVignette.enabled = false;
        }
    }

    public void ShowBriefing(string name, string objective, int count)
    {
        if (missionText) missionText.text = name;
        if (briefingTitle) briefingTitle.text = name;
        if (briefingObjective) briefingObjective.text = $"{objective} — eliminate {count} hostiles";
        briefingPanel?.SetActive(true);
        resultPanel?.SetActive(false);
    }

    public void HideBriefing() { briefingPanel?.SetActive(false); }

    public void ShowClear(string name, int score)
    {
        resultPanel?.SetActive(true);
        if (resultTitle) resultTitle.text = "MISSION CLEAR";
        if (resultScore) resultScore.text = score.ToString("D6");
        nextButton?.gameObject.SetActive(true);
    }

    public void ShowWon(int score)
    {
        resultPanel?.SetActive(true);
        if (resultTitle) resultTitle.text = "CAMPAIGN COMPLETE";
        if (resultScore) resultScore.text = score.ToString("D6");
        nextButton?.gameObject.SetActive(false);
    }

    public void ShowLost(int score)
    {
        resultPanel?.SetActive(true);
        if (resultTitle) resultTitle.text = "OPERATIVE DOWN";
        if (resultScore) resultScore.text = score.ToString("D6");
        nextButton?.gameObject.SetActive(false);
    }
}
