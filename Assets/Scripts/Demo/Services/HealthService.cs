using UnityEngine;

namespace Demo.Services
{
  public class HealthService
  {
    private int _health = 100;
    private int _maxHealth = 100;

    public void TakeDamage(int amount)
    {
      _health -= amount;
      Debug.Log($"[HealthService] Took {amount} damage. Current health: {_health}");
    }

    public void Heal(int amount)
    {
      _health += amount;
      _health = Mathf.Min(_health, _maxHealth);
      Debug.Log($"[HealthService] Healed {amount} health. Current health: {_health}");
    }

    public void SetHealth(int health)
    {
      _health = Mathf.Clamp(health, 0, _maxHealth);
      Debug.Log($"[HealthService] Set health to {health}. Current health: {_health}");
    }
  }
}