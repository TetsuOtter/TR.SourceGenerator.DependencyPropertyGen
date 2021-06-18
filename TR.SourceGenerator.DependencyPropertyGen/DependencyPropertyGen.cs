using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TR.SourceGenerator
{
	//Source Generator ref : https://github.com/RyotaMurohoshi/ValueObjectGenerator
	//Source Generator ref : https://github.com/dotnet/roslyn-sdk/blob/main/samples/CSharp/SourceGenerators
	//CodeAnalysis ref : https://aonasuzutsuki.hatenablog.jp/entry/2019/05/07/104305

	[Generator]
	public class DependencyPropertyGenerator : ISourceGenerator
	{
		static readonly Encoding myEncording = Encoding.Unicode;

		public const string attributeName = "DependencyPropertyGenAttribute";
		public const string attributeName_short = "DependencyPropertyGen";
		const string attributeNameSpace = "TR.SourceGenerator";
		const string attributeFullName = attributeNameSpace + "." + attributeName;
		const string AttributeFileName = attributeFullName;
		const string attributeArgName_PropName = "PropName";
		const string attributeArgName_PropType = "PropType";
		const string attributeArgName_metaD = "MetaDataVarName";
		const string attributeArgName_hasSetter = "HasSetter";
		const string attributeArgName_SetterAccessibility = "SetterAccessibility";
		static readonly string attributeText = @$"using System;
namespace {attributeNameSpace}
{{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public sealed class {attributeName} : Attribute
	{{
		public {attributeName}(Type _type, string _name) => SetProps(_type, _name, string.Empty, true, string.Empty);
		public {attributeName}(Type _type, string _name, bool hasSetter) => SetProps(_type, _name, string.Empty, hasSetter, string.Empty);
		public {attributeName}(Type _type, string _name, string metaDVarName, bool hasSetter = true) => SetProps(_type, _name, metaDVarName, hasSetter, string.Empty);
		public {attributeName}(Type _type, string _name, string metaDVarName, bool hasSetter, string setterAccessibility) => SetProps(_type, _name, metaDVarName, hasSetter, setterAccessibility);

		private void SetProps(in Type _type, in string _name, in string metaDVarName, in bool hasSetter, in string setterAccessibility)
		{{ this.{attributeArgName_PropType} = _type; this.{attributeArgName_PropName} = _name; this.{attributeArgName_metaD} = metaDVarName; this.{attributeArgName_hasSetter} = hasSetter; this.{attributeArgName_SetterAccessibility} = setterAccessibility; }}

		public Type {attributeArgName_PropType} {{ get; set; }}
		public string {attributeArgName_PropName} {{ get; set; }}
		public bool {attributeArgName_hasSetter} {{ get; set; }}
		public string {attributeArgName_SetterAccessibility} {{ get; set; }}
		public string {attributeArgName_metaD} {{ get; set; }}
	}}
}}
";
		/// <summary>DependencyPropertyGenAttribute SourceText</summary>
		static SourceText DPGAttributeSourceText { get => SourceText.From(attributeText, myEncording); }//インスタンスの使いまわしは無理っぽい?  パフォーマンスをそこまで気にする必要はないだろうし, 都度生成でいく.
		public void Initialize(GeneratorInitializationContext context)
		{
			//初期化終了後にAttributeを実装したファイルを追加する処理を行う設定
			context.RegisterForPostInitialization((i) => i.AddSource(AttributeFileName, DPGAttributeSourceText));

			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (context.SyntaxContextReceiver is not SyntaxReceiver receiver) return;

			Compilation? compilation = context.Compilation;
			INamedTypeSymbol? attributeSymbol = compilation.GetTypeByMetadataName(attributeFullName);
			if (attributeSymbol is null)
				return;

			foreach (ClassDeclarationSyntax? candiate in receiver.CandidateClasses)
			{
				SemanticModel semModel = compilation.GetSemanticModel(candiate.SyntaxTree);
				ISymbol? typeSymbol = ModelExtensions.GetDeclaredSymbol(semModel, candiate);

				if (typeSymbol?.ContainingSymbol.Equals(typeSymbol.ContainingNamespace, SymbolEqualityComparer.Default) is null or false)
					continue;

				foreach (AttributeData attributeData in typeSymbol.GetAttributes())
				{
					if (attributeData.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) is null or false)
						continue;//属性がDependencyPropertyGenAttributeでないなら, SourceをGenerateしない

					string typeName = attributeData.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
					string propName = (attributeData.ConstructorArguments[1].Value as string) ?? string.Empty;

					string fname = $"{typeSymbol.ToDisplayString().Replace('.', '-')}-{propName}";//fname一致でエラーを送出させたい

					string createdSource = GenerateSource(typeSymbol, typeName, propName, attributeData);

					if (!string.IsNullOrWhiteSpace(createdSource))
						context.AddSource(fname, SourceText.From(createdSource, myEncording));//自動生成コードを追加
				}
			}
		}

		static string GenerateSource(ISymbol typeSymbol, in string typeName, in string propName, in AttributeData attributeData)
		{
			string metaD = attributeData.NamedArguments.SingleOrDefault(kv_pair => (kv_pair.Key == attributeArgName_metaD)).Value.Value as string ?? string.Empty;
			if (string.IsNullOrWhiteSpace(metaD) && attributeData.ConstructorArguments.Length >= 3)
				if (attributeData.ConstructorArguments[2].Value is string s)//下手に上と一緒にすると, sのスコープが広くなるため
					metaD = s;
			metaD = string.IsNullOrWhiteSpace(metaD) ? string.Empty : (", " + metaD);

			string namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
			string className = typeSymbol.ToDisplayString().Split('.').Last();

			bool? IsSetterAvailable_tmp = (bool?)attributeData.NamedArguments.SingleOrDefault(kv_pair => kv_pair.Key == attributeArgName_hasSetter).Value.Value;
			if (IsSetterAvailable_tmp is null)
				if (attributeData.ConstructorArguments.Length == 3 && attributeData.ConstructorArguments[2].Value is bool b)
					IsSetterAvailable_tmp = b;
				else if (attributeData.ConstructorArguments.Length >= 4 && attributeData.ConstructorArguments[3].Value is bool b2)
					IsSetterAvailable_tmp = b2;
			bool IsSetterAvailable = IsSetterAvailable_tmp ?? true;
			
			string setterAccessor = (attributeData.NamedArguments.SingleOrDefault(kv_pair => kv_pair.Key == attributeArgName_SetterAccessibility).Value.Value as string) ?? string.Empty;
			if (string.IsNullOrWhiteSpace(setterAccessor) && attributeData.ConstructorArguments.Length == 5)
				if (attributeData.ConstructorArguments[4].Value is string s)//下手に上と一緒にすると, sのスコープが広くなるため
					setterAccessor = s;

			StringBuilder ReturnStr = new();

			ReturnStr.Append(
$@"using System.Windows;
namespace {namespaceName}
{{
	public partial class {className}
	{{"+"\n"
);

			string DependencyProperty_SetValue_dp = string.Empty;
			string DependencyPropertyFieldName = $"{propName}Property";
			if (IsSetterAvailable && setterAccessor != string.Empty)//setterが存在し, かつsetterにアクセス制御子指定があるなら, setterはDependencyPropertyKey経由でsetする
			{
				if (metaD == string.Empty)
					metaD = ", new()";//DependencyProperty.RegisterReadOnlyではmetaDataが必須になるため

				DependencyProperty_SetValue_dp = DependencyPropertyFieldName + "Key";
				ReturnStr.Append($"\t\tprivate static readonly DependencyPropertyKey {DependencyProperty_SetValue_dp} = DependencyProperty.RegisterReadOnly(nameof({propName}), typeof({typeName}), typeof({className}) {metaD});\n");
				ReturnStr.Append($"\t\tpublic static readonly DependencyProperty {DependencyPropertyFieldName} = {DependencyProperty_SetValue_dp}.DependencyProperty;\n");
			}
			else
			{
				DependencyProperty_SetValue_dp = DependencyPropertyFieldName;
				ReturnStr.Append($"\t\tpublic static readonly DependencyProperty {DependencyPropertyFieldName} = DependencyProperty.Register(nameof({propName}), typeof({typeName}), typeof({className}) {metaD});\n");
			}

			string setter = string.Empty;
			if (IsSetterAvailable)
				setter = $"{setterAccessor} set => SetValue({DependencyProperty_SetValue_dp}, value);";//setterに実装する内容

			ReturnStr.Append(//プロパティ実装を行う
$@"

		public {typeName} {propName}
		{{
			get => ({typeName})GetValue({propName}Property);
			{setter}
		}}
");
			ReturnStr.Append("\t}\n}");

			return ReturnStr.ToString();
		}
	}


	internal class SyntaxReceiver : ISyntaxContextReceiver
	{
		public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

		public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
		{
			if (context.Node is ClassDeclarationSyntax PDS)
				foreach (var tmp0 in PDS.AttributeLists) 
					foreach (var tmp1 in tmp0.Attributes)
						if (Equals(tmp1.Name.ToString(), DependencyPropertyGenerator.attributeName_short) || Equals(tmp1.Name.ToString(), DependencyPropertyGenerator.attributeName))
						{
							CandidateClasses.Add(PDS);
							return;
						}
		}
	}
}
