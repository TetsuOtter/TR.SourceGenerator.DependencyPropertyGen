using System;
using System.Windows;

namespace TR.SourceGenerator.DependencyPropertyGen.Sample
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			Title = System.Reflection.Assembly.GetExecutingAssembly().FullName;
		}
	}
}
