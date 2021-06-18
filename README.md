# TR.SourceGenerator.DependencyPropertyGen
DependencyPropertyを追加するC# Source Generatorです

By using this project, you can easily implement DependencyProperty in any class.

## How to install
[Get it from nuget.org](https://www.nuget.org/packages/TR.SourceGenerator.DependencyPropertyGen)

nugetにてパッケージ参照を追加してください.  それだけで導入が完了します.

## How to use
DependencyPropertyを実装したいクラスに, partialキーワードとDependencyPropertyGen Attributeを付けてください.

DependencyPropertyGen Attributeは, 次のコンストラクタを持っています.

- DependencyPropertyGenAttribute(Type _type, string _name)
- DependencyPropertyGenAttribute(Type _type, string _name, bool hasSetter)
- DependencyPropertyGenAttribute(Type _type, string _name, string metaDVarName, bool hasSetter = true)
- DependencyPropertyGenAttribute(Type _type, string _name, string metaDVarName, bool hasSetter, string setterAccessibility)

それぞれの引数の意味は, 次の通りです.

|Type|Name|Description|
|---|---|---|
|Type|_type|プロパティの型を指定する|
|string|_name|プロパティの名前を指定する|
|string|metaDVarName|Propertyetadata型フィールドの名前を指定する|
|bool|hasSetter|プロパティがsetterを持つかどうか|
|string|setterAccessibility|プロパティのsetterのアクセス修飾子|

## Example 01
例えば, 次のようなコードを作成したとします.
```
using System.Windows.Controls;

namespace TR.SourceGenerator.DependencyPropertyGen.Sample
{
	[DependencyPropertyGen(typeof(string), "MyText")]
	public partial class SampleControl : Control
	{
	}
}
```

すると, DependencyPropertyGeneratorは次のようなコードを生成します.
```
using System.Windows;
namespace TR.SourceGenerator.DependencyPropertyGen.Sample
{
	public partial class SampleControl
	{
		public static readonly DependencyProperty MyTextProperty = DependencyProperty.Register(nameof(MyText), typeof(string), typeof(SampleControl) );

		public string MyText
		{
			get => (string)GetValue(MyTextProperty);
			 set => SetValue(MyTextProperty, value);
		}
	}
}
```

## Example 02 (Read-Only Property)
例えば, 次のようなコードを作成したとします.
```
using System.Windows.Controls;

namespace TR.SourceGenerator.DependencyPropertyGen.Sample
{
	[DependencyPropertyGen(typeof(string), "MyTexts", null, true, "private")]
	public partial class SampleControl : Control
	{
	}
}
```

すると, DependencyPropertyGeneratorは次のようなコードを生成します.
```
using System.Windows;
namespace TR.SourceGenerator.DependencyPropertyGen.Sample
{
	public partial class SampleControl
	{
		private static readonly DependencyPropertyKey MyTextPropertyKey = DependencyProperty.RegisterReadOnly(nameof(MyText), typeof(string), typeof(SampleControl) , new());
		public static readonly DependencyProperty MyTextProperty = MyTextPropertyKey.DependencyProperty;


		public string MyText
		{
			get => (string)GetValue(MyTextProperty);
			private set => SetValue(MyTextPropertyKey, value);
		}
	}
}
```

## License
MITライセンスの下で自由に使用/改造等することができます.  
なお, 本SourceGeneratorで生成されたコードには著作権は発生しないものと考えておりますが, 仮に発生する場合はCC0ライセンスを適用するものとします.
