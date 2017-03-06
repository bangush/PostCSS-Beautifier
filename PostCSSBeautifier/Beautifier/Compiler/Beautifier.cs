using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Core.ContentTypes;
using PostCSSBeautifier.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PostCSSBeautifier.Compiler
{
	internal class Beautifier
	{
		public static async Task<string> ProcessString(string toProcess, IContentType contentType)
		{
			var unformattedCssTempFilePath = MakeTempFile(toProcess, contentType);
			return await Compiler.Beautifier.Process(unformattedCssTempFilePath).WithTimeout(TimeSpan.FromSeconds(5));
		}

		public static async Task<string> Process(string tempFilePath)
		{
			OutputWriter.Write($"Resources: {NodePaths.Resources}");

			var configPath = BeautifierTagger.CombConfigPath == BeautifierTagger.DefaultCombConfigPath
				? Path.Combine(@"..\..\", BeautifierTagger.CombConfigPath)
				: BeautifierTagger.CombConfigPath;

			OutputWriter.Write($"Config path is {configPath}");

			var newConfigPath = Path.Combine(NodePaths.Node, "postcss.config.js");

			if (configPath != newConfigPath)
			{
				var filesAreDifferent = true;
				if (File.Exists(newConfigPath))
				{
					filesAreDifferent = !FileEquals(configPath, newConfigPath);
				}

				if (filesAreDifferent)
				{
					try
					{
						if (File.Exists(newConfigPath))
						{
							OutputWriter.Write($"Deleting existing temp config file at {newConfigPath}");
							File.Delete(newConfigPath);
						}

						OutputWriter.Write($"Writing config {newConfigPath}");
						File.Copy(configPath, newConfigPath);
					}
					catch (IOException io)
					{
						OutputWriter.Write($"An exception occured copying {configPath} to {newConfigPath}: {io}");
					}
					catch (Exception e)
					{
						OutputWriter.Write(e, true);
					}
				}
			}


			var cmd = $"--no-map -c postcss.config.js -r \"{tempFilePath}\"";

			var ns = await PostCssCommander.CallCommandAsync(cmd);
			OutputWriter.Write($"Command ouput: {ns}");

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