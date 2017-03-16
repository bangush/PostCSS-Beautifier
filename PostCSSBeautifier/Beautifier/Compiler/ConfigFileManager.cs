using PostCSSBeautifier.Helpers;
using System;
using System.IO;

namespace PostCSSBeautifier.Compiler
{
	internal class ConfigFileManager
	{
		public static string DefaultPostCssSortingPath = Path.Combine(NodePaths.Resources, "postcss-sorting.json");
		public static string DefaultStyleFmtPath = Path.Combine(NodePaths.Resources, "stylefmt.json");
		public static string ConfigTemplatePath = Path.Combine(NodePaths.Resources, "postcss-config-template.js");

		public static string PostCssSortingPath = 
			OptionPageGrid.PostCssPath == DefaultPostCssSortingPath || string.IsNullOrEmpty(OptionPageGrid.PostCssPath)
			? DefaultPostCssSortingPath
			: OptionPageGrid.PostCssPath;

		public static string StyleFmtPath = 
			OptionPageGrid.StyleFmtPath == DefaultStyleFmtPath || string.IsNullOrEmpty(OptionPageGrid.StyleFmtPath)
			? DefaultStyleFmtPath
			: OptionPageGrid.StyleFmtPath;

		public static string ResultantPostCssSortingPath = Path.Combine(NodePaths.Node, "postcss-sorting.json");
		public static string ResultantStyleFmtPath = Path.Combine(NodePaths.Node, "stylefmt.json");
		public static string ResultantConfigPath = Path.Combine(NodePaths.Node, "postcss.config.js");

		public static void UpdateOrCreateConfig()
		{
			var configsChanged = CopyConfigFile(PostCssSortingPath, ResultantPostCssSortingPath);
			configsChanged = CopyConfigFile(StyleFmtPath, ResultantStyleFmtPath) || configsChanged;

			if (configsChanged)
			{
				CreateFinalConfig();
			}
		}

		private static bool CopyConfigFile(string startPath, string resultPath)
		{
			var filesAreDifferent = false;

			if (startPath != resultPath)
			{
				filesAreDifferent = true;
				if (File.Exists(resultPath))
				{
					filesAreDifferent = !FileEquals(startPath, resultPath);
				}

				if (filesAreDifferent)
				{
					try
					{
						if (File.Exists(resultPath))
						{
							OutputWriter.Write($"Deleting existing temp config file at {resultPath}");
							File.Delete(resultPath);
						}

						OutputWriter.Write($"Writing config {resultPath}");
						File.Copy(startPath, resultPath);
					}
					catch (IOException io)
					{
						OutputWriter.Write($"An exception occured copying {startPath} to {resultPath}: {io}");
					}
					catch (Exception e)
					{
						OutputWriter.Write(e, true);
					}
				}
			}

			return filesAreDifferent;
		}

		private static void CreateFinalConfig()
		{
			var styleFmtConfig = File.ReadAllText(ResultantStyleFmtPath);
			var postCssSortingConfig = File.ReadAllText(ResultantPostCssSortingPath);
			var configTemplate = File.ReadAllText(ConfigTemplatePath);

			var newConfig = configTemplate.Replace("$$$STYLEFMT$$$", styleFmtConfig);
			newConfig = newConfig.Replace("$$$POSTCSSSORTING$$$", postCssSortingConfig);

			File.WriteAllText(ResultantConfigPath, newConfig);
		}

		private static bool FileEquals(string path1, string path2)
		{
			byte[] file1 = File.ReadAllBytes(path1);
			byte[] file2 = File.ReadAllBytes(path2);
			if (file1.Length == file2.Length)
			{
				for (int i = 0; i < file1.Length; i++)
				{
					if (file1[i] != file2[i])
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}
	}
}