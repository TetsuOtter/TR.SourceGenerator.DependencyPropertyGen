﻿using System.Windows;
using System.Windows.Media;

namespace TR.SourceGenerator.DependencyPropertyGen.Sample
{
	[DependencyPropertyGen(typeof(Brush), "FrameBrush")]
	[DependencyPropertyGen(typeof(Thickness), "FrameThickness")]
	public partial class SampleControl_Inherit : SampleControl_Base //must set "partial" keyword
	{
		static SampleControl_Inherit() => DefaultStyleKeyProperty.OverrideMetadata(typeof(SampleControl_Inherit), new FrameworkPropertyMetadata(typeof(SampleControl_Inherit)));
	}
}