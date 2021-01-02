﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
	public class DependencyPropertyGenerator : ISourceGenerator
	{
		static readonly Encoding myEncording = Encoding.Default;

		const string attributeName = "DependencyPropertyGenAttribute";
		const string attributeFullName = "TR.SourceGenerator.DependencyPropertyGenAttribute";
		const string attributeArgName_metaD = "metaData";
		const string attributeArgName_hasSetter = "HasSetter";
		const string attributeArgName_SetterAccessibility = "SetterAccessibility";
		const string attributeText = @"using System;
namespace TR.SourceGenerator
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public sealed class DependencyPropertyGenAttribute : Attribute
	{
		public DependencyPropertyGenAttribute(Type _type, string _name) { }
		public bool HasSetter { get; set; } = true;
		public string SetterAccessibility { get; set; } = string.Empty;
		public string metaData { get; set; } = string.Empty;
	}
}
";
		/// <summary>DependencyPropertyGenAttribute SourceText</summary>
		static SourceText DPGAttributeSourceText { get => SourceText.From(attributeText, myEncording); }//インスタンスの使いまわしは無理っぽい?  パフォーマンスをそこまで気にする必要はないだろうし, 都度生成でいく.

		public void Execute(GeneratorExecutionContext context)
		{
			if (context.SyntaxReceiver is not SyntaxReceiver receiver) return;

			context.AddSource(attributeName, DPGAttributeSourceText);

			var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(DPGAttributeSourceText,
				(context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions));

			var attributeSymbol = compilation.GetTypeByMetadataName(attributeFullName);

			foreach (var candiate in receiver.CandidateClasses)
			{
				ISymbol typeSymbol = ModelExtensions.GetDeclaredSymbol(compilation.GetSemanticModel(candiate.SyntaxTree), candiate);
				if (!typeSymbol.ContainingSymbol.Equals(typeSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
					continue;

				ImmutableArray<AttributeData> attributes = typeSymbol.GetAttributes();
				foreach (AttributeData attributeData in attributes)
				{
					if (!attributeData.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default))
						continue;//属性がDependencyPropertyGenAttributeでないなら, SourceをGenerateしない

					string typeName = attributeData.ConstructorArguments[0].Value.ToString();
					string propName = attributeData.ConstructorArguments[1].Value as string;

					string fname = $"{typeSymbol.ToDisplayString()}_{propName}_dependency_prop";//fname一致でエラーを送出させたい

					string createdSource = GenerateSource(typeSymbol, typeName, propName, attributeData);

					context.AddSource(fname, SourceText.From(createdSource, myEncording));//自動生成コードを追加
				}
			}
		}

		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}


		static string GenerateSource(ISymbol typeSymbol, in string typeName, in string propName, in AttributeData attributeData)
		{
			var metaDataSetting = attributeData.NamedArguments.SingleOrDefault(kv_pair => (kv_pair.Key == attributeArgName_metaD));
			string metaD = metaDataSetting.Value.Value as string;
			metaD = string.IsNullOrWhiteSpace(metaD) ? string.Empty : (", " + metaD);

			string namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
			string className = typeSymbol.ToDisplayString().Split('.').Last();

			bool IsSetterAvailable = false;
			string setterAccessor = string.Empty;

			IsSetterAvailable = (bool?)attributeData.NamedArguments.SingleOrDefault(kv_pair => (kv_pair.Key == attributeArgName_hasSetter)).Value.Value ?? true;
			setterAccessor = (attributeData.NamedArguments.SingleOrDefault(kv_pair => (kv_pair.Key == attributeArgName_SetterAccessibility)).Value.Value as string) ?? string.Empty;

			string setter = string.Empty;
			if (IsSetterAvailable)
				setter = $"{setterAccessor} set => SetValue({propName}Property, value);";

			return $@"using System.Windows;
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
";
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
