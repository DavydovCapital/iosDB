using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;
    public int maxHealth = 100;
    public int Health { get; private set; }

    void Awake() { Instance = this; Health = maxHealth; }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        HUD.Instance?.SetHealth(Health);
        HUD.Instance?.DamageFlash();
        GameAudio.Instance?.Damage();
        if (Health <= 0)
        {
            Health = 0;
            GameManager.Instance?.PlayerDied();
        }
    }

    public void ResetHealth()
    {
        Health = maxHealth;
        HUD.Instance?.SetHealth(Health);
    }
}
