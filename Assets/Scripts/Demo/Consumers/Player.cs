using UnityEngine;
using Demo.Services;
using DependencyInjection.Attributes;

namespace Demo.Consumers
{
  public class Player : MonoBehaviour
  {
    [Inject] private readonly HealthService _healthService;
    [Inject] private readonly WeaponService _weaponService;

    public void Start()
    {
      _healthService?.SetHealth(100);
      _weaponService?.Fire();
    }
  }
}