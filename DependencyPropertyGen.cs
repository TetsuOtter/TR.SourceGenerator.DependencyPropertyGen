using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TR.SourceGenerator
{
	//Source Generator ref : https://github.com/RyotaMurohoshi/ValueObjectGenerator
	//CodeAnalysis ref : https://aonasuzutsuki.hatenablog.jp/entry/2019/05/07/104305

	[Generator]
	public class DependencyPropertyGen : ISourceGenerator
	{
		static Encoding myEncording = Encoding.UTF8;

		const string attributeName = "DependencyPropertyGenAttribute";
		const string attributeFullName = "TR.SourceGenerator.DependencyPropertyGenAttribute";
		const string attributeArgName_type = "type";
		const string attributeArgName_name = "name";
		const string attributeArgName_metaD = "metaData";
		const string attributeArgName_hasSetter = "HasSetter";
		const string attributeArgName_SetterAccessibility = "SetterAccessibility";
		const string attributeText = @"
using System;
namespace TR.SourceGenerator
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	sealed class DependencyPropertyGenAttribute : Attribute
	{
		public DependencyPropertyGenAttribute(Type _type, string _name) { this.type = _type; this.name = _name; }
		public Type type { get; set; }
		public string name { get; set; }
		public bool HasSetter { get; set; } = true;
		public string SetterAccessibility { get; set; } = string.Empty;
		public string metaData { get; set; } = string.Empty;
	}
}
";

		static DependencyPropertyGen()
		{
			try
			{
				File.Delete("D:\\DepPropAutoGen\\log.log");
			}
			catch (Exception) { }
		}

		static List<string> WrittenCheckList = new List<string>();

		public void Execute(GeneratorExecutionContext context)
		{
			File.AppendAllText("D:\\DepPropAutoGen\\log.log", "Execute\n");
			var sourceText = SourceText.From(attributeText, myEncording);
			context.AddSource(attributeName, sourceText);

			var receiver = context.SyntaxReceiver as SyntaxReceiver;

			if (receiver is null) return;
			File.AppendAllText("D:\\DepPropAutoGen\\log.log", "Execute Pass1\n");

			var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceText,
				(context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions));

			var attributeSymbol = compilation.GetTypeByMetadataName(attributeFullName);

			File.AppendAllText("D:\\DepPropAutoGen\\log.log", "Execute Pass2\n");
			foreach (var candiate in receiver.CandidateClasses)
			{
				File.AppendAllText("D:\\DepPropAutoGen\\log.log", "Execute Pass2-1\n");
				var model = compilation.GetSemanticModel(candiate.SyntaxTree);
				File.AppendAllText("D:\\DepPropAutoGen\\log.log", "Execute Pass2-2\n");
				var typeSymbol = ModelExtensions.GetDeclaredSymbol(model, candiate);
				File.AppendAllText("D:\\DepPropAutoGen\\log.log", "Execute Pass3  name:" + typeSymbol.Name + "\n");

				if (typeSymbol.GetAttributes().Any(attrData => attrData.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
				{
					File.AppendAllText("D:\\DepPropAutoGen\\log.log", "Execute Pass3-if  name:" + typeSymbol.Name + "\n");

					var attributeData = typeSymbol.GetAttributes().Single(attrD => attrD.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));

					string typeName = attributeData.ConstructorArguments[0].Value.ToString();
					string propName = ((attributeData.ConstructorArguments[1].Value as string) ?? "AnonymousProp");

					if (!typeSymbol.ContainingSymbol.Equals(typeSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
						continue;

					File.AppendAllText("D:\\DepPropAutoGen\\log.log", "Execute Pass3-1\n");
					string fname = $"{typeSymbol.ToDisplayString()}_{propName}_dependency_prop";

					File.AppendAllText("D:\\DepPropAutoGen\\log.log", $"Execute fname:{fname}\n");
					if (WrittenCheckList.Find(v => v.Equals(fname)) is not null)//既に出力済み
						continue;

					string createdSource = GenerateSource(typeSymbol, attributeSymbol, typeName, propName, attributeData);
					File.AppendAllText("D:\\DepPropAutoGen\\" + fname + ".tmp.cs", createdSource);
					File.AppendAllText("D:\\DepPropAutoGen\\log.log", "Execute Pass3-1.1\n");
					try
					{
						context.AddSource(fname, SourceText.From(createdSource, myEncording));
						WrittenCheckList.Add(fname);
					}
					catch (Exception ex)
					{
						File.AppendAllText("D:\\DepPropAutoGen\\log.log", $"{ex}\n");
					}
					File.AppendAllText("D:\\DepPropAutoGen\\log.log", "Execute Pass3-2\n");
				}
				File.AppendAllText("D:\\DepPropAutoGen\\log.log", "Execute Pass4  name:" + typeSymbol.Name + "\n");
			}
			File.AppendAllText("D:\\DepPropAutoGen\\log.log", "Execute Done\n");
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			File.AppendAllText("D:\\DepPropAutoGen\\log.log", "Initialize\n");
			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
			File.AppendAllText("D:\\DepPropAutoGen\\log.log", "Initialize Done\n");
		}


		static string GenerateSource(ISymbol typeSymbol, ISymbol attributeSymbol, in string typeName, in string propName, in AttributeData attributeData)
		{
			//propName = string.Empty;
			File.AppendAllText("D:\\DepPropAutoGen\\log.log", "GenerateSource Pass1\n");

			//var attributeData = typeSymbol.GetAttributes().Single(attrD => attrD.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
			var metaDataSetting = attributeData.NamedArguments.SingleOrDefault(kv_pair => (kv_pair.Key == attributeArgName_metaD));
			string metaD = metaDataSetting.Value.Value as string;
			metaD = string.IsNullOrWhiteSpace(metaD) ? string.Empty : (", " + metaD);

			string namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
			string className = typeSymbol.ToDisplayString().Split('.').Last();


			File.AppendAllText("D:\\DepPropAutoGen\\log.log", $"{attributeData.ConstructorArguments[0].Value}\t{attributeData.ConstructorArguments[1].Value}");
			File.AppendAllText("D:\\DepPropAutoGen\\log.log", "\tGenerateSource Pass_Check\n");

			string setter = string.Empty;
			bool IsSetterAvailable = false;
			string setterAccessor = string.Empty;

			IsSetterAvailable = ((bool?)attributeData.NamedArguments.SingleOrDefault(kv_pair => (kv_pair.Key == attributeArgName_hasSetter)).Value.Value ?? true);
			setterAccessor = ((attributeData.NamedArguments.SingleOrDefault(kv_pair => (kv_pair.Key == attributeArgName_SetterAccessibility)).Value.Value as string) ?? string.Empty);
			File.AppendAllText("D:\\DepPropAutoGen\\log.log", "GenerateSource Pass3\n");

			if (IsSetterAvailable)
				setter = $"{setterAccessor} set => SetValue({propName}Property, value);";

			var source = new StringBuilder($@"using System.Windows;
namespace {namespaceName}
{{
	public partial class {className}
	{{
		public static readonly DependencyProperty {propName}Property = DependencyProperty.Register(nameof({propName}), typeof({typeName}), typeof({className}) {metaD});

		public {typeName} {propName}
		{{
			get => ({typeName})GetValue({propName}Property);
			{setter}
		}}
	}}
}}
");
			File.AppendAllText("D:\\DepPropAutoGen\\log.log", "GenerateSource Pass4\n");

			return source.ToString();
		}
	}


	internal class SyntaxReceiver : ISyntaxReceiver
	{
		public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is ClassDeclarationSyntax PDS && PDS.AttributeLists.Count >= 0)
				CandidateClasses.Add(PDS);
		}
	}
}
