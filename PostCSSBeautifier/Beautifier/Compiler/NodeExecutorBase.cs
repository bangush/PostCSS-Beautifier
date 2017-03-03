using PostCSSBeautifier.Compiler.Result;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PostCSSBeautifier.Compiler
{
	public abstract class NodeExecutorBase
	{
		protected virtual bool Previewing { get { return false; } }

		///<summary>Indicates whether this compiler will emit a source map file.  Will only return true if supported and enabled in user settings.</summary>
		public abstract bool MinifyInPlace { get; }
		public abstract bool GenerateSourceMap { get; }
		public abstract string TargetExtension { get; }
		public abstract string ServiceName { get; }

		// Don't try-catch this method: We need to "address" all the bugs,
		// which may occur as the (node.js-based) service implement changes.
		private async Task<CompilerResult> ProcessResult(CompilerResult result, string sourceFileName, string targetFileName)
		{
			if (result == null)
			{
				Logger.Log(ServiceName + ": " + Path.GetFileName(sourceFileName) + " compilation failed: The service failed to respond to this request\n\t\t\tPossible cause: Syntax Error!");
				return await CompilerResultFactory.GenerateResult(sourceFileName, targetFileName);
			}
			if (!result.IsSuccess)
			{
				var firstError = result.Errors.Where(e => e != null).Select(e => e.Message).FirstOrDefault();

				if (firstError != null)
					Logger.Log(firstError);

				return result;
			}

			Logger.Log(ServiceName + ": " + Path.GetFileName(sourceFileName) + " compiled.");
			
			return result;
		}

		public static string GetOrCreateGlobalSettings(string fileName)
		{
			var globalFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), fileName);

			if (!File.Exists(globalFile))
			{
				var extensionDir = Path.GetDirectoryName(typeof(NodeExecutorBase).Assembly.Location);
				var settingsFile = Path.Combine(extensionDir, @"Resources\settings-defaults\", fileName);
				File.Copy(settingsFile, globalFile);
			}

			return globalFile;
		}

		protected abstract string GetPath(string sourceFileName, string targetFileName);
	}
}