using System;
namespace TR.SourceGenerator
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public sealed class DependencyPropertyGenAttribute : Attribute
	{
		public DependencyPropertyGenAttribute(Type _type, string _name) => SetProps(_type, _name, string.Empty, true, string.Empty);
		public DependencyPropertyGenAttribute(Type _type, string _name, bool hasSetter) => SetProps(_type, _name, string.Empty, hasSetter, string.Empty);
		public DependencyPropertyGenAttribute(Type _type, string _name, string metaDVarName, bool hasSetter = true) => SetProps(_type, _name, metaDVarName, hasSetter, string.Empty);
		public DependencyPropertyGenAttribute(Type _type, string _name, string metaDVarName, bool hasSetter, string setterAccessibility) => SetProps(_type, _name, metaDVarName, hasSetter, setterAccessibility);

		private void SetProps(in Type _type, in string _name, in string metaDVarName, in bool hasSetter, in string setterAccessibility)
		{ this.PropType = _type; this.PropName = _name; this.MetaDataVarName = metaDVarName; this.HasSetter = hasSetter; this.SetterAccessibility = setterAccessibility; }

		public Type PropType { get; }
		public string PropName { get; }
		public bool HasSetter { get; set; } = true;
		public string SetterAccessibility { get; set; } = string.Empty;
		public string MetaDataVarName { get; set; } = string.Empty;
	}
}