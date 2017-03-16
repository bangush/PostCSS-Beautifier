using PostCSSBeautifier.Compiler;
using System.IO;

namespace PostCSSBeautifier.Helpers
{
	internal class NodePaths
	{
		private static string Assembly = Path.GetDirectoryName(typeof(PostCssCommander).Assembly.Location);
		public static string Resources = Path.Combine(Assembly, @"Resources");
		public static string Node = Path.Combine(Resources, @"nodejs\");
	}
}
