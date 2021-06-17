using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TR.SourceGenerator.DependencyPropertyGen.Sample
{
	[DependencyPropertyGen(typeof(string), "MyText")]
	[DependencyPropertyGen(typeof(Brush), "MyTextColor")]
	public partial class SampleControl_Base : Control //must set "partial" keyword
	{
		static SampleControl_Base() => DefaultStyleKeyProperty.OverrideMetadata(typeof(SampleControl_Base), new FrameworkPropertyMetadata(typeof(SampleControl_Base)));
	}
}
