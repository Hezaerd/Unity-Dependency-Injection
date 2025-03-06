using UnityEngine;
using DependencyInjection.Attributes;
using DependencyInjection.Interfaces;
using Demo.Services;

namespace Demo.Providers
{
  public class GameProvider : MonoBehaviour, IDependencyProvider
  {
    [Provide]
    public HealthService ProvideHealthService()
    {
        return new HealthService();
    }

    [Provide]
    public WeaponService ProvideWeaponService()
    {
        return new WeaponService();
    }
  }
}