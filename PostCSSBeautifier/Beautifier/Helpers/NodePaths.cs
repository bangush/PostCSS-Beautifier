using PostCSSBeautifier.Compiler;
using System.IO;

namespace PostCSSBeautifier.Helpers
{
	internal class NodePaths
	{
		private static string _assembly = Path.GetDirectoryName(typeof(PostCssCommander).Assembly.Location);
		public static string Resources = Path.Combine(_assembly, @"Resources");
		public static string Node = Path.Combine(Resources, @"nodejs\");
	}
}
