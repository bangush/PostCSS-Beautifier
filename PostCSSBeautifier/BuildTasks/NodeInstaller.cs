using Microsoft.Build.Framework;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DirectoryInfo = Pri.LongPath.DirectoryInfo;
using FileInfo = Pri.LongPath.FileInfo;
using IO = System.IO;

namespace PostCSSBeautifier.BuildTasks
{
	public class NodeInstaller : Microsoft.Build.Utilities.Task
	{
		public static string NodeVersion = "7.7.0";
		public static string NodeUrl = "https://nodejs.org/dist/v" + NodeVersion + "/node-v" + NodeVersion + "-win-x64.zip";
		public static string NodeFolder = "node-v" + NodeVersion + "-win-x64";

		static DateTime GetSourceVersion([CallerFilePath] string path = null)
		{
			return IO.File.GetLastWriteTimeUtc(path);
		}


		// Stores the timestamp of the last successful build.  This file will be deleted
		// at the beginning of each non-cached build, so there is no risk of caching the
		// results of a failed build.
		const string VersionStampFileName = @"resources\nodejs\tools\node_modules\successful-version-timestamp.txt";

		public override bool Execute()
		{
			DateTime existingVersion;
			if (IO.File.Exists(VersionStampFileName)
			    && DateTime.TryParse(
				    IO.File.ReadAllText(VersionStampFileName),
				    CultureInfo.InvariantCulture,
				    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal,
				    out existingVersion)
			    && existingVersion > DateTime.UtcNow - TimeSpan.FromDays(14)
			    && existingVersion > GetSourceVersion())
			{
				Log.LogMessage(MessageImportance.High, "Reusing existing installed Node modules from " + existingVersion);
				return true;
			}
			if (IO.Directory.Exists(@"resources\nodejs\tools\node_modules"))
				ClearPath(@"resources\nodejs\tools\node_modules");

			Task.WaitAll(
				DownloadNodeZipAsync()
			);

			const string configPath = @"resources\postcss.config.js";
			const string copyToPath = @"resources\nodejs\postcss.config.js";
			if (!IO.File.Exists(copyToPath))
			{
				IO.File.Copy(configPath, copyToPath);
			}

			var moduleResults = Task.WhenAll(
					InstallModuleAsync("postcss-cli", "postcss-cli"),
					InstallModuleAsync("postcss-scss", "postcss-scss"),
					InstallModuleAsync("postcss-sorting", "postcss-sorting"),
					InstallModuleAsync("stylelint", "stylelint"),
					InstallModuleAsync("stylelint-config-standard", "stylelint-config-standard"),
					InstallModuleAsync("stylefmt", "stylefmt")
				)
				.Result.Where(r => r != ModuleInstallResult.AlreadyPresent);

			if (moduleResults.Contains(ModuleInstallResult.Error))
				return false;

			if (!moduleResults.Any())
				return true;

			Log.LogMessage(MessageImportance.High, "Installed " + moduleResults.Count() + " modules.  Flattening...");

			if (!DedupeAsync().Result)
				return false;

			FlattenNodeModules(@"resources\nodejs");

			return true;
		}

		private void ClearPath(string path)
		{
			var dirs = IO.Directory.GetDirectories(path);
			foreach (var dir in dirs)
			{
				Log.LogMessage(MessageImportance.Low, "Removing " + dir + "...");
				IO.Directory.Delete(dir, true);
			}

			var files = IO.Directory.GetFiles(path);
			foreach (var file in files)
			{
				Log.LogMessage(MessageImportance.Low, "Removing " + file + "...");
				IO.File.Delete(file);
			}
		}

		async Task DownloadNodeZipAsync()
		{
			var file = new FileInfo(@"resources\nodejs\node.exe");

			if (file.Exists && file.Length > 0)
				return;

			var nodeZip = await WebClientDoAsync(wc => wc.OpenReadTaskAsync(NodeUrl));

			ExtractZipWithOverwrite(nodeZip, @"resources");

			Log.LogMessage(MessageImportance.High, "Extracted nodejs zip");
			IO.Directory.Move(IO.Path.Combine(@"resources\", NodeFolder), @"resources\nodejs");
			Log.LogMessage(MessageImportance.High, "Moved to nodejs folder");
		}

		async Task<T> WebClientDoAsync<T>(Func<WebClient, Task<T>> transactor)
		{
			try
			{
				return await transactor(new WebClient());
			}
			catch (WebException e)
			{
				Log.LogWarningFromException(e);
				if (!IsHttpStatusCode(e, HttpStatusCode.ProxyAuthenticationRequired))
					throw;
			}

			return await transactor(CreateWebClientWithProxyAuthSetup());
		}

		static bool IsHttpStatusCode(WebException e, HttpStatusCode status)
		{
			HttpWebResponse response;
			return e.Status == WebExceptionStatus.ProtocolError
			       && (response = e.Response as HttpWebResponse) != null
			       && response.StatusCode == status;
		}

		static WebClient CreateWebClientWithProxyAuthSetup(IWebProxy proxy = null, ICredentials credentials = null)
		{
			var wc = new WebClient {Proxy = proxy ?? WebRequest.GetSystemWebProxy()};
			wc.Proxy.Credentials = credentials ?? CredentialCache.DefaultCredentials;
			return wc;
		}

		enum ModuleInstallResult
		{
			AlreadyPresent,
			Installed,
			Error
		}

		async Task<ModuleInstallResult> InstallModuleAsync(string cmdName, string moduleName)
		{
			if (string.IsNullOrEmpty(cmdName))
			{
				if (IO.File.Exists(@"resources\nodejs\node_modules\" + moduleName + @"\package.json"))
					return ModuleInstallResult.AlreadyPresent;
			}
			else
			{
				if (IO.File.Exists(@"resources\nodejs\node_modules\.bin\" + cmdName + ".cmd"))
					return ModuleInstallResult.AlreadyPresent;
			}

			Log.LogMessage(MessageImportance.High, "npm install " + moduleName + " ...");

			var output = await ExecWithOutputAsync(@"cmd", @"/c npm install -g " + moduleName + "@latest", @"resources\nodejs\");

			if (output != null)
			{
				Log.LogError("npm install " + moduleName + " error: " + output);
				return ModuleInstallResult.Error;
			}

			return ModuleInstallResult.Installed;
		}

		async Task<bool> DedupeAsync()
		{
			var output = await ExecWithOutputAsync(@"cmd", @"/c npm.cmd dedup ", @"resources\nodejs");

			if (output != null)
				Log.LogError("npm dedup error: " + output);

			return output == null;
		}

		/// <summary>
		/// Due to the way node_modues work, the directory depth can get very deep and go beyond MAX_PATH (260 chars). 
		/// Therefore grab all node_modues directories and move them up to baseNodeModuleDir. Node's require() will then 
		/// traverse up and find them at the higher level. Should be fine as long as there are no versioning conflicts.
		/// </summary>
		void FlattenNodeModules(string baseNodeModuleDir)
		{
			var baseDir = new DirectoryInfo(baseNodeModuleDir);

			var modules = from dir in new DirectoryInfo(baseNodeModuleDir).GetDirectories("*", IO.SearchOption.AllDirectories)
				where dir.Name.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
				orderby dir.FullName.Count(c => c == IO.Path.DirectorySeparatorChar) descending // Get deepest first
				select dir;

			foreach (var nodeModule in modules)
			{
				foreach (var module in nodeModule.EnumerateDirectories())
				{
					// If the package uses a non-default main file,
					// add a redirect in index.js so that require()
					// can find it without package.json.
					if (module.Name != ".bin" && !IO.File.Exists(IO.Path.Combine(module.FullName, "index.js")))
					{
						dynamic package = JsonConvert.DeserializeObject(IO.File.ReadAllText(module.FullName + "\\package.json"));
						string main = package.main;

						if (!string.IsNullOrEmpty(main))
						{
							if (!main.StartsWith("."))
								main = "./" + main;
							IO.File.WriteAllText(
								IO.Path.Combine(module.FullName, "index.js"),
								"module.exports = require(" + JsonConvert.ToString(main) + ");"
							);
						}
					}

					// If this is already a top-level module, don't move it.
					if (module.Parent.Parent.FullName == baseDir.FullName)
						continue;
					else if (module.Name == ".bin")
					{
						// We don't care about any .bin folders in nested modules (we do need the top-level one)
						module.Delete(recursive: true);
						continue;
					}

					var intermediatePath = baseDir.FullName;
					dynamic sourcePackage = JsonConvert.DeserializeObject(IO.File.ReadAllText(module.FullName + @"\package.json"));
					// Try to move the module to the node_modules folder in the
					// base directory, then to that same folder in every parent
					// module up to this module's immediate parent.
					foreach (var part in module.Parent.Parent.FullName.Substring(intermediatePath.Length)
						.Split(new[] {@"\node_modules\"}, StringSplitOptions.None))
					{
						if (!string.IsNullOrEmpty(part))
							intermediatePath += @"\node_modules\" + part;
						var targetDir = IO.Path.Combine(intermediatePath, "node_modules", module.Name);
						if (IO.Directory.Exists(targetDir))
						{
							dynamic targetPackage = JsonConvert.DeserializeObject(IO.File.ReadAllText(targetDir + @"\package.json"));
							// If the existing package is a different version, keep
							// going, and move it to a different folder. Otherwise,
							// delete it and keep the other one, then stop looking.
							if (targetPackage.version != sourcePackage.version)
								continue;
							Log.LogMessage(MessageImportance.High, "Deleting " + module.FullName + " in favor of " + targetDir);
							module.Delete(recursive: true);
							break;
						}
						module.MoveTo(targetDir);
						break;
					}
					if (module.Exists)
						Log.LogMessage(MessageImportance.High, "Not collapsing conflicting module " + module.FullName);
				}

				if (!nodeModule.GetFileSystemInfos().Any())
					nodeModule.Delete();
			}
		}

		/// <summary>Invokes a command-line process asynchronously, capturing its output to a string.</summary>
		/// <returns>Null if the process exited successfully; the process' full output if it failed.</returns>
		static async Task<string> ExecWithOutputAsync(string filename, string args, string workingDirectory = null)
		{
			var error = new IO.StringWriter();
			var result = await ExecAsync(filename, args, workingDirectory, null, error);

			return result == 0 ? null : error.ToString().Trim();
		}

		/// <summary>Invokes a command-line process asynchronously.</summary>
		static Task<int> ExecAsync(string filename, string args, string workingDirectory = null, IO.TextWriter stdout = null,
			IO.TextWriter stderr = null)
		{
			stdout = stdout ?? IO.TextWriter.Null;
			stderr = stderr ?? IO.TextWriter.Null;

			var p = new Process
			{
				StartInfo = new ProcessStartInfo(filename, args)
				{
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardError = true,
					RedirectStandardOutput = true,
					WorkingDirectory = workingDirectory == null ? null : IO.Path.GetFullPath(workingDirectory),
				},
				EnableRaisingEvents = true,
			};

			p.OutputDataReceived += (sender, e) =>
			{
				stdout.WriteLine(e.Data);
			};
			p.ErrorDataReceived += (sender, e) =>
			{
				stderr.WriteLine(e.Data);
			};

			p.Start();
			p.BeginErrorReadLine();
			p.BeginOutputReadLine();
			var processTaskCompletionSource = new TaskCompletionSource<int>();

			p.EnableRaisingEvents = true;
			p.Exited += (s, e) =>
			{
				p.WaitForExit();
				processTaskCompletionSource.TrySetResult(p.ExitCode);
			};

			return processTaskCompletionSource.Task;
		}

		void ExtractZipWithOverwrite(IO.Stream sourceZip, string destinationDirectoryName)
		{
			using (var source = new ZipArchive(sourceZip, ZipArchiveMode.Read))
			{
				source.ExtractToDirectory(destinationDirectoryName);
			}
		}
	}
}