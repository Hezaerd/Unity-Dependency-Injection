using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using DependencyInjection.Attributes;
using DependencyInjection.Interfaces;

namespace DependencyInjection
{
	[DefaultExecutionOrder(-1000)]
	public class Injector : MonoBehaviour
	{
		private const BindingFlags KBindingFlags =
			BindingFlags.Instance |
			BindingFlags.NonPublic |
			BindingFlags.Public;

		private readonly Dictionary<Type, object> _registry = new();


		private void Awake()
		{
			var monoBehaviours = FindMonoBehaviours();

			// Find all modules implementing IDependencyProvider and register them
			var providers = monoBehaviours.OfType<IDependencyProvider>();
			foreach (IDependencyProvider provider in providers)
			{
				Register(provider);
			}

			// Find all injectable objects and inject their dependencies
			var injectables = monoBehaviours.Where(IsInjectable);
			foreach (MonoBehaviour injectable in injectables)
			{
				Inject(injectable);
			}
		}


		/// <summary>
		/// Injects dependencies into an object.
		/// </summary>
		/// <param name="instance"> The object to inject dependencies into. </param>
		private void Inject(object instance)
		{
			Type type = instance.GetType();

			// Inject into fields
			var injectableFields = type.GetFields(KBindingFlags)
				.Where(member => Attribute.IsDefined(member, typeof(InjectAttribute)));

			foreach (FieldInfo injectableField in injectableFields)
			{
				// If the field is already set, skip it
				if (injectableField.GetValue(instance) != null)
				{
					Debug.LogWarning($"[HezInjector] Field `{injectableField.Name}` of class `{type.Name}` is already set.");
					continue;
				}

				var fieldType = injectableField.FieldType;
				var resolvedInstance = Resolve(fieldType);

				if (resolvedInstance == null)
					throw new Exception($"Failed to inject dependency into field `{injectableField.Name}` of class `{type.Name}`.");

				injectableField.SetValue(instance, resolvedInstance);
			}

			// Inject into properties
			var injectableProperties = type.GetProperties(KBindingFlags)
				.Where(property => Attribute.IsDefined(property, typeof(InjectAttribute)));

			foreach (var injectableProperty in injectableProperties)
			{
				var propertyType = injectableProperty.PropertyType;
				var resolvedInstance = Resolve(propertyType);

				if (resolvedInstance == null)
					throw new Exception($"Failed to inject dependency into property `{injectableProperty.Name}` of class `{type.Name}`.");

				injectableProperty.SetValue(instance, resolvedInstance);
			}

			// Inject into methods
			var injectableMethods = type.GetMethods(KBindingFlags)
				.Where(method => Attribute.IsDefined(method, typeof(InjectAttribute)));

			foreach (var injectableMethod in injectableMethods)
			{
				var requiredParameters = injectableMethod.GetParameters()
					.Select(parameter => parameter.ParameterType)
					.ToArray();
				var resolvedParameters = requiredParameters.Select(Resolve).ToArray();

				if (resolvedParameters.Any(parameter => parameter == null))
					throw new Exception($"Failed to inject dependencies into method `{injectableMethod.Name}` of class `{type.Name}`.");

				injectableMethod.Invoke(instance, resolvedParameters);
			}
		}

		/// <summary>
		/// Registers a dependency provider in the registry.
		/// </summary>
		/// <param name="provider"> The provider to register. </param>
		/// <exception cref="Exception"> Thrown when a method marked with the ProvideAttribute returns null. </exception>
		private void Register(IDependencyProvider provider)
		{
			var methods = provider.GetType().GetMethods(KBindingFlags);

			foreach (MethodInfo method in methods)
			{
				// Skip if the method is not marked with the ProvideAttribute
				if (!Attribute.IsDefined(method, typeof(ProvideAttribute)))
					continue;

				Type returnType = method.ReturnType;
				var providedInstance = method.Invoke(provider, null);

				if (providedInstance != null)
					_registry.Add(returnType, providedInstance);
				else
					throw new Exception($"[HezInjector] Method `{method.Name}` of class `{provider.GetType().Name}` returned null when providing type `{returnType.Name}`.");
			}
		}

		private void ValidateDependencies()
		{
			var monoBehaviours = FindMonoBehaviours();
			var providers = monoBehaviours.OfType<IDependencyProvider>();
			var providedDependencies = GetProvidedDependencies(providers);

			var invalidDependencies = monoBehaviours
				.SelectMany(mb => mb.GetType().GetFields(KBindingFlags), (mb, field) => new { MonoBehaviour = mb, Field = field })
				.Where(t => Attribute.IsDefined(t.Field, typeof(InjectAttribute)))
				.Where(t => !providedDependencies.Contains(t.Field.FieldType) && t.Field.GetValue(t.MonoBehaviour) == null)
				.Select(t => $"[Validation] {t.MonoBehaviour.GetType().Name} is missing dependency {t.Field.FieldType.Name} on GameObject {t.MonoBehaviour.gameObject.name}");

			var invalidDependencyList = invalidDependencies.ToList();

			if (!invalidDependencyList.Any())
				Debug.Log($"[DependencyInjection (Validation)] All dependencies are valid.");
			else
			{
				Debug.LogError($"[DependencyInjection (Validation)] {invalidDependencyList.Count} dependencies are invalid:");
				foreach (var invalidDependency in invalidDependencyList)
					Debug.LogError(invalidDependency);
			}
		}

		private HashSet<Type> GetProvidedDependencies(IEnumerable<IDependencyProvider> providers)
		{
			var providedDependencies = new HashSet<Type>();
			foreach (var provider in providers)
			{
				var methods = provider.GetType().GetMethods(KBindingFlags);

				foreach (var method in methods)
				{
						if (!Attribute.IsDefined(method, typeof(ProvideAttribute))) continue;

						var returnType = method.ReturnType;
						providedDependencies.Add(returnType);
				}
			}

			return providedDependencies;
		}

		/// <summary>
		/// Resolves a dependency.
		/// </summary>
		/// <param name="type"> The type to resolve. </param>
		/// <returns> The resolved instance. </returns>
		/// <exception cref="Exception"> Thrown when the type is not registered. </exception>
		private object Resolve(Type type)
		{
			if (_registry.TryGetValue(type, out var instance))
				return instance;

			throw new Exception($"[HezInjector] Type `{type.Name}` is not registered.");
		}

		/// <summary>
		/// Find all MonoBehaviours in the scene.
		/// </summary>
		/// <returns></returns>
		private static MonoBehaviour[] FindMonoBehaviours()
		{
			return FindObjectsOfType<MonoBehaviour>();
		}

		/// <summary>
		/// Check if a MonoBehaviour is injectable.
		/// </summary>
		/// <param name="monoBehaviour"> The MonoBehaviour to check. </param>
		/// <returns> True if the MonoBehaviour is injectable, false otherwise. </returns>
		private static bool IsInjectable(MonoBehaviour monoBehaviour)
		{
			var members = monoBehaviour.GetType().GetMembers(KBindingFlags);
			return members.Any(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
		}
	}
}