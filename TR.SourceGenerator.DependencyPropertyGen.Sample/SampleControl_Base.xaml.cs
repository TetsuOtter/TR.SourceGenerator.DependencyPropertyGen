using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using TR.SourceGenerator;

namespace TR.SourceGenerator.DependencyPropertyGen.Sample
{
	//[TR.SourceGenerator.DependencyPropertyGen(typeof(string), "MyText")]
	//[DependencyPropertyGen(typeof(Brush), "MyTextColor")]
	public partial class SampleControl_Base : Control //must set "partial" keyword
	{
		static SampleControl_Base() => DefaultStyleKeyProperty.OverrideMetadata(typeof(SampleControl_Base), new FrameworkPropertyMetadata(typeof(SampleControl_Base)));
	}
}
