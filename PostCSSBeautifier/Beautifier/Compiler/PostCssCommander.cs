using PostCSSBeautifier.Helpers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PostCSSBeautifier.Compiler
{
	public enum CommandExitCode
	{
		Success,
		Error
	}

	public sealed class PostCssCommander
	{
		public static async Task<CommandExitCode> CallCommandAsync(string arguments)
		{
			OutputWriter.Write($"Command called: {arguments}");

			var assemblyLocation = Path.GetDirectoryName(typeof(PostCssCommander).Assembly.Location);
			var nodeLocation = Path.Combine(Path.Combine(assemblyLocation, @"Resources"), @"nodejs\");

			OutputWriter.Write($"Node: {nodeLocation}");

			var success = new StringBuilder();
			var error = new StringBuilder();
			try
			{
				using (var errorTw = new StringWriter(error))
				using (var successTw = new StringWriter(success))
				{
					var exitCode = await ProcessCreator.RunCommand(
						"postcss " + arguments,
						nodeLocation,
						successTw,
						errorTw,
						5000);

					if (!string.IsNullOrWhiteSpace(error.ToString()) && exitCode != 0)
					{
						OutputWriter.Write(error, true);
					}

					return exitCode == 0 
						? CommandExitCode.Success 
						: CommandExitCode.Error;
				}
			}
			catch (TaskCanceledException)
			{
				OutputWriter.Write("Process Timed Out! " + error);

				return CommandExitCode.Error;
			}
		}
	}
}