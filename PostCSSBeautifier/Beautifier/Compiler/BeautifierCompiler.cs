using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Core.ContentTypes;
using PostCSSBeautifier.Helpers;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PostCSSBeautifier.Compiler
{
	internal class BeautifierCompiler
	{
		public static string ProcessString(string toProcess, IContentType contentType)
		{
			return AsyncHelper.RunSync(() => ProcessStringAsync(toProcess, contentType));
		}

		public static async Task<string> ProcessStringAsync(string toProcess, IContentType contentType)
		{
			var unformattedCssTempFilePath = MakeTempFile(toProcess, contentType);
			return await Process(unformattedCssTempFilePath).WithTimeout(TimeSpan.FromSeconds(5));
		}

		public static async Task<string> Process(string tempFilePath)
		{
			OutputWriter.Write($"Resources: {NodePaths.Resources}");

			ConfigFileManager.UpdateOrCreateConfig();

			var cmd = $"--no-map -c postcss.config.js -r \"{tempFilePath}\"";

			var ns = await PostCssCommander.CallCommandAsync(cmd);
			OutputWriter.Write($"Command ouput: {ns}");

			var formattedString = File.ReadAllText(tempFilePath);

			formattedString = Regex.Replace(formattedString, "(?<!\r)\n", "\r\n");

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