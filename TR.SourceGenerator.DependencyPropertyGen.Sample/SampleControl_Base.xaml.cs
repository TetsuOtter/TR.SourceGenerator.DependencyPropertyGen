using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TR.SourceGenerator.DependencyPropertyGen.Sample
{
	[GenerateDependencyProperty(typeof(string), "MyText")]
	[GenerateDependencyProperty(typeof(Brush), "MyTextColor")]
	[GenerateDependencyProperty(typeof(double), "MyFontSize", "private", null)]
	public partial class SampleControl_Base : Control //must set "partial" keyword
	{
		static SampleControl_Base() => DefaultStyleKeyProperty.OverrideMetadata(typeof(SampleControl_Base), new FrameworkPropertyMetadata(typeof(SampleControl_Base)));

		public SampleControl_Base() => MyFontSize = 16;//You can change value from here
	}
}
