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
		#region Attribute Settings
		public const string attributeName_short = "GenerateDependencyProperty";
		public const string attributeName = attributeName_short + "Attribute";
		const string attributeNameSpace = "TR.SourceGenerator";
		const string attributeFullName = attributeNameSpace + "." + attributeName;
		const string AttributeFileName = attributeFullName;
		const string attributeArgName_PropName = "PropName";
		const string attributeArgName_PropType = "PropType";
		const string attributeArgName_metaD = "MetaDataVarName";
		const string attributeArgName_SetterAccessibility = "SetterAccessibility";
		const string attributeText = @$"using System;
namespace {attributeNameSpace}
{{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public sealed class {attributeName} : Attribute
	{{
		public {attributeName}(Type {attributeArgName_PropType}, string {attributeArgName_PropName}, string {attributeArgName_SetterAccessibility} = """", string {attributeArgName_metaD} = """")
		{{
			this.{attributeArgName_PropType} = {attributeArgName_PropType};
			this.{attributeArgName_PropName} = {attributeArgName_PropName};
			this.{attributeArgName_SetterAccessibility} = {attributeArgName_SetterAccessibility};
			this.{attributeArgName_metaD} = {attributeArgName_metaD};
		}}
		public Type {attributeArgName_PropType} {{ get; }}
		public string {attributeArgName_PropName} {{ get; }}
		public string {attributeArgName_SetterAccessibility} {{ get; }}
		public string {attributeArgName_metaD} {{ get; }}
	}}
}}
";
		#endregion

		public void Initialize(GeneratorInitializationContext context)
		{
			//初期化終了後にAttributeを実装したファイルを追加する処理を行う設定
			context.RegisterForPostInitialization((i) => i.AddSource(AttributeFileName, attributeText));

			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver() as ISyntaxReceiver);
		}

		public void Execute(GeneratorExecutionContext context)
		{
			SyntaxReceiver receiver;
			if (context.SyntaxContextReceiver is SyntaxReceiver _creceiver) receiver = _creceiver;
			else if (context.SyntaxReceiver is SyntaxReceiver _receiver) receiver = _receiver;
			else return;

			Compilation? compilation = context.Compilation;
			INamedTypeSymbol? attributeSymbol = compilation.GetTypeByMetadataName(attributeFullName);
			if (attributeSymbol is null)
				return;

			foreach (ClassDeclarationSyntax? candidateClass in receiver.CandidateClasses)
			{
				SemanticModel semModel = compilation.GetSemanticModel(candidateClass.SyntaxTree);
				ISymbol? typeSymbol = ModelExtensions.GetDeclaredSymbol(semModel, candidateClass);

				if (typeSymbol?.ContainingSymbol.Equals(typeSymbol.ContainingNamespace, SymbolEqualityComparer.Default) is null or false)
					continue;

				foreach (AttributeData attributeData in typeSymbol.GetAttributes())
				{
					if (attributeData.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) is null or false)
						continue;//属性がDependencyPropertyGenAttributeでないなら, SourceをGenerateしない
					if (attributeData.ConstructorArguments.Length < 2)
						continue;//ConstructorArgumentsは最低でも2つ必要

					string typeName = attributeData.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
					string propName = (attributeData.ConstructorArguments[1].Value as string) ?? string.Empty;

					string fname = $"{typeSymbol.ToDisplayString().Replace('.', '-')}-{propName}";//fname一致でエラーを送出させたい

					string createdSource = GenerateSource(typeSymbol, typeName, propName, attributeData);

					if (!string.IsNullOrWhiteSpace(createdSource))
						context.AddSource(fname, createdSource);//自動生成コードを追加
				}
			}
		}

		static string GenerateSource(ISymbol typeSymbol, in string typeName, in string propName, in AttributeData attributeData)
		{
			string metaD = attributeData.NamedArguments.SingleOrDefault(kv_pair => (kv_pair.Key == attributeArgName_metaD)).Value.Value as string ?? string.Empty;
			if (string.IsNullOrWhiteSpace(metaD) && attributeData.ConstructorArguments.Length >= 4)
				if (attributeData.ConstructorArguments[3].Value is string s)//下手に上と一緒にすると, sのスコープが広くなるため
					metaD = s;
			metaD = string.IsNullOrWhiteSpace(metaD) ? string.Empty : (", " + metaD);

			string namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
			string className = typeSymbol.ToDisplayString().Split('.').Last();

			string setterAccessor = (attributeData.NamedArguments.SingleOrDefault(kv_pair => kv_pair.Key == attributeArgName_SetterAccessibility).Value.Value as string) ?? string.Empty;
			if (string.IsNullOrWhiteSpace(setterAccessor) && attributeData.ConstructorArguments.Length >= 3)
				if (attributeData.ConstructorArguments[2].Value is string s)//下手に上と一緒にすると, sのスコープが広くなるため
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
			if (!string.IsNullOrWhiteSpace(setterAccessor))//setterが存在し, かつsetterにアクセス制御子指定があるなら, setterはDependencyPropertyKey経由でsetする
			{
				if (string.IsNullOrWhiteSpace(metaD))
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

			_ = ReturnStr.Append(//プロパティ実装を行う
$@"

		public {typeName} {propName}
		{{
			get => ({typeName})GetValue({propName}Property);
			{setterAccessor} set => SetValue({DependencyProperty_SetValue_dp}, value);
		}}
");
			ReturnStr.Append("\t}\n}");

			return ReturnStr.ToString();
		}
	}


	internal class SyntaxReceiver : ISyntaxContextReceiver, ISyntaxReceiver
	{
		public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

		public void OnVisitSyntaxNode(GeneratorSyntaxContext context) => OnVisitSyntaxNode(context.Node);

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is ClassDeclarationSyntax PDS)
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
