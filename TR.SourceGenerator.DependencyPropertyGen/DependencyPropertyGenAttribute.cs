using System;
namespace TR.SourceGenerator
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public sealed class GenerateDependencyPropertyAttribute : Attribute
	{
		public DependencyPropertyGenAttribute(Type PropType, string PropName, string SetterAccessibility = "", string MetaDataVarName = "")
		{
			this.PropType = PropType;
			this.PropName = PropName;
			this.SetterAccessibility = SetterAccessibility;
			this.MetaDataVarName = MetaDataVarName;
		}
		public Type PropType { get; }
		public string PropName { get; }
		public string SetterAccessibility { get; }
		public string MetaDataVarName { get; }
	}
}