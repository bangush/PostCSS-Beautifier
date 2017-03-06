//------------------------------------------------------------------------------
// <copyright file="FormatMenuCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using PostCSSBeautifier.Compiler;
using System;
using System.ComponentModel.Design;

namespace PostCSSBeautifier
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class FormatMenuCommand
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0100;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("4e51b630-92a0-4d48-bda3-3f73ce4206b5");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private readonly Package package;

		/// <summary>
		/// Initializes a new instance of the <see cref="FormatMenuCommand"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		private FormatMenuCommand(Package package)
		{
			if (package == null)
			{
				throw new ArgumentNullException("package");
			}

			this.package = package;

			OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (commandService != null)
			{
				var menuCommandID = new CommandID(CommandSet, CommandId);
				var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
				commandService.AddCommand(menuItem);
			}
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static FormatMenuCommand Instance
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the service provider from the owner package.
		/// </summary>
		private IServiceProvider ServiceProvider
		{
			get
			{
				return this.package;
			}
		}

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public static void Initialize(Package package)
		{
			Instance = new FormatMenuCommand(package);
		}

		/// <summary>
		/// This function is the callback used to execute the command when the menu item is clicked.
		/// See the constructor to see how the menu item is associated with this function using
		/// OleMenuCommandService service and MenuCommand class.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private async void MenuItemCallback(object sender, EventArgs e)
		{
			var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
			if (dte != null)
			{
				var activeDoc = dte.ActiveDocument;
				var activeDocTextDoc = activeDoc.Object() as TextDocument;
				if (activeDocTextDoc != null)
				{
					var startPoint = activeDocTextDoc.CreateEditPoint(activeDocTextDoc.StartPoint);
					var fileContents = startPoint.GetText(activeDocTextDoc.EndPoint);

					var result = await Beautifier.ProcessString(fileContents, BeautifierTagger.CurrentContentType);

					startPoint.ReplaceText(activeDocTextDoc.EndPoint, result, (int)vsEPReplaceTextOptions.vsEPReplaceTextNormalizeNewlines);
				}

			}
		}
	}
}
