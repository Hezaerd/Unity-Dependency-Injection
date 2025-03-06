using System;
using System.ComponentModel;

namespace DependencyInjection.Attributes
{
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class ProvideAttribute : PropertyTabAttribute { }
}