//------------------------------------------------------------------------------
// <copyright file="ToolsOptionsPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using PostCSSBeautifier.Properties;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace PostCSSBeautifier
{
	internal static class GuidList
	{
		public const string guidMyToolsOptionsPkgString = "01030911-e7a9-43de-bee7-e881eb784ac6";
	}

	[ProvideMenuResource("Menus.ctmenu", 1)]
	[Guid(ToolsOptionsPackage.PackageGuidString)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
	[ProvideOptionPage(typeof(OptionPageGrid), "VS CSS Process", "Config Path", 0, 0, true)]
	[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling"), PackageRegistration(UseManagedResourcesOnly = true)]
	public sealed class ToolsOptionsPackage : Package
	{
		/// <summary>
		/// ToolsOptionsPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "5dc9a1ee-4aae-46a1-bb23-deb7c8498842";

		public string CombConfigPath
		{
			get
			{
				var page = (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
				return page.CombConfigPath;
			}
		}


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
		protected override void Initialize()
		{
			base.Initialize();
		}

		#endregion
	}

	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[Guid(GuidList.guidMyToolsOptionsPkgString)]
	public class OptionPageGrid : DialogPage
	{
		private Settings Settings { get; }

		public OptionPageGrid()
		{
			this.Settings = new Settings();
		}

		[Category("VS CSS Process")]
		[DisplayName("Config Path")]
		[Description("Set your own configuration")]
		public string CombConfigPath
		{
			set
			{
				this.Settings.BeautifierJSONConfigPath = value;
				BeautifierTagger.RefreshConfig(value);
			}
			get { return this.Settings.BeautifierJSONConfigPath; }
		}

		[Category("VS CSS Process")]
		[DisplayName("Debug Mode")]
		[Description("Enable Debug Mode")]
		public bool DebugMode
		{
			set
			{
				this.Settings.DebugMode = value;
			}
			get { return this.Settings.DebugMode; }
		}
	}
}
