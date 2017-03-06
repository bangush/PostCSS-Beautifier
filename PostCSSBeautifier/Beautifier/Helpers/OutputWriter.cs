using System;
using System.Diagnostics;
using System.Windows;

namespace PostCSSBeautifier.Helpers
{
	internal class OutputWriter
	{
		public static void Write(object writeThis, bool isImportant = false)
		{
			var showBox = isImportant || writeThis.GetType() == typeof(Exception);

			if (showBox)
			{
				MessageBox.Show(writeThis.ToString());
			}
			else if (Debugger.IsAttached)
			{
				Debug.WriteLine(writeThis);
			}
		}
	}
}
