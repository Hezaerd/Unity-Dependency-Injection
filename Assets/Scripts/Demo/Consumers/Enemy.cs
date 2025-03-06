using UnityEngine;
using Demo.Services;
using DependencyInjection.Attributes;

namespace Demo.Consumers
{
  public class Enemy : MonoBehaviour
  {
    [Inject] private readonly HealthService _healthService;

    public void Start()
    {
      _healthService?.SetHealth(10);
    }
  }
}