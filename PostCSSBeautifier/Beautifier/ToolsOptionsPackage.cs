//------------------------------------------------------------------------------
// <copyright file="ToolsOptionsPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using PostCSSBeautifier.Compiler;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PostCSSBeautifier
{
	internal static class GuidList
	{
		public const string guidMyToolsOptionsPkgString = "01030911-e7a9-43de-bee7-e881eb784ac6";
	}

	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
	[ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]
	[Guid(ToolsOptionsPackage.PackageGuidString)]
	[ProvideOptionPage(typeof(OptionPageGrid), "PostCSS Beautifier", "Settings", 0, 0, true)]
	public sealed class ToolsOptionsPackage : Package
	{

		private FormatDocumentOnBeforeSave plugin;

		/// <summary>
		/// ToolsOptionsPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "5dc9a1ee-4aae-46a1-bb23-deb7c8498842";


		/// <summary>
		/// Initializes a new instance of the <see cref="ToolsOptionsPackage"/> class.
		/// </summary>
		public ToolsOptionsPackage()
		{
			// Inside this method you can place any initialization code that does not require
			// any Visual Studio service because at this point the package object is created but
			// not sited yet inside Visual Studio environment. The place to do all the other
			// initialization is the Initialize method.
		}

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		protected override async void Initialize()
		{
			var dte = (DTE) GetService(typeof(DTE));

			var runningDocumentTable = new RunningDocumentTable(this);
			var documentFormatService = new BeautifierFormatService(dte);
			plugin = new FormatDocumentOnBeforeSave(dte, runningDocumentTable, documentFormatService);
			runningDocumentTable.Advise(plugin);

			base.Initialize();
		}

		#endregion
	}

	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[Guid(GuidList.guidMyToolsOptionsPkgString)]
	public class OptionPageGrid : DialogPage
	{
		private static readonly Settings Settings = new Settings();

		public static string StyleFmtPath { get; set; } =
			!string.IsNullOrWhiteSpace(Settings.stylefmtJsonPath)
				? Settings.stylefmtJsonPath
				: ConfigFileManager.DefaultStyleFmtPath.ToLower();

		public static string PostCssPath { get; set; } =
			!string.IsNullOrWhiteSpace(Settings.postcssSortingJsonPath)
				? Settings.postcssSortingJsonPath
				: ConfigFileManager.DefaultPostCssSortingPath.ToLower();

		[Category("PostCSS Beautifier")]
		[DisplayName("StyleFmt JSON Path")]
		[Description("The location for your stylefmt json config file. See https://goo.gl/0Apmas for formatting tips.")]
		public string StyleFmtJson
		{
			get { return StyleFmtPath; }
			set
			{
				StyleFmtPath = value;
				Settings.stylefmtJsonPath = value;
				Settings.Save();
			}
		}

		[Category("PostCSS Beautifier")]
		[DisplayName("PostCSS Sorting JSON Path")]
		[Description(
			"The location for your postcss-sorting json config file. See https://goo.gl/hl3kQK for formatting tips.")]
		public string PostCssSortingJson
		{
			get { return PostCssPath; }
			set
			{
				PostCssPath = value;
				Settings.postcssSortingJsonPath = value;
				Settings.Save();
			}
		}
	}
}