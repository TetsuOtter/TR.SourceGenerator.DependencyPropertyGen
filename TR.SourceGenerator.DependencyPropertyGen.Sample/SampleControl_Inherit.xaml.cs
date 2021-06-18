using System.Windows;
using System.Windows.Media;

namespace TR.SourceGenerator.DependencyPropertyGen.Sample
{
	[DependencyPropertyGen(typeof(Brush), "FrameBrush")]
	[DependencyPropertyGenAttribute(typeof(Thickness), "FrameThickness")]
	public partial class SampleControl_Inherit : SampleControl_Base //must set "partial" keyword
	{
		static SampleControl_Inherit() => DefaultStyleKeyProperty.OverrideMetadata(typeof(SampleControl_Inherit), new FrameworkPropertyMetadata(typeof(SampleControl_Inherit)));
		//void abc() => MyFontSize = 10;// Cannot Access from here
	}
}
