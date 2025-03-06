using System;
using System.ComponentModel;

namespace DependencyInjection.Attributes
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
	public sealed class InjectAttribute : PropertyTabAttribute { }
}