using System;
using System.ComponentModel;

namespace DependencyInjection.Attributes
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class InjectAttribute : PropertyTabAttribute { }
}