using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Core.ContentTypes;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PostCSSBeautifier.Compiler
{
	internal class Beautifier
	{
		public static async Task<string> Process(string tempFilePath)
		{
			var config = BeautifierTagger.CombConfigPath == BeautifierTagger.DefaultCombConfigPath
				? @"..\..\" + BeautifierTagger.CombConfigPath
				: BeautifierTagger.CombConfigPath;

			var nodeLocation =
				Path.Combine(Path.Combine(Path.GetDirectoryName(typeof(PostCssCommander).Assembly.Location), @"Resources"),
					@"nodejs\");

			var newConfigPath = Path.Combine(nodeLocation, "postcss.config.js");

			if (!File.Exists(newConfigPath))
			{
				File.Copy(config, newConfigPath);
			}

			var cmd = $"--no-map -c postcss.config.js -r \"{tempFilePath}\"";

			var ns = await PostCssCommander.CallCommandAsync(cmd);
			Logger.Log(ns);

			var formattedString = File.ReadAllText(tempFilePath);

			return formattedString;
		}

		public static string MakeTempFile(string cssContent, IContentType contentType)
		{
			var extension = ".css";
			switch (contentType.TypeName)
			{
				case ScssContentTypeDefinition.ScssLanguageName:
					extension = ".scss";
					break;


				case LessContentTypeDefinition.LessLanguageName:
					extension = ".less";
					break;
			}

			var path = MakeTempFilePath(extension);

			Logger.Log(path);
			File.WriteAllText(path, cssContent);

			return path;
		}

		private static string MakeTempFilePath(string extension)
		{
			return Path.Combine(Path.GetTempPath(), Guid.NewGuid() + extension);
		}
	}
}
