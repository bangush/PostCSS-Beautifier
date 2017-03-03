using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PostCSSBeautifier.Compiler
{
	public sealed class PostCssCommander
	{
		public static async Task<string> CallCommandAsync(string arguments)
		{
			var assemblyLocation = Path.GetDirectoryName(typeof(PostCssCommander).Assembly.Location);
			var nodeLocation = Path.Combine(Path.Combine(assemblyLocation, @"Resources"), @"nodejs\");

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
						MessageBox.Show(error.ToString());
					}

					return success.ToString();
				}
			}
			catch (TaskCanceledException)
			{
				MessageBox.Show("Process Timed Out! " + error);
			}

			if (!string.IsNullOrWhiteSpace(error.ToString()))
			{
				MessageBox.Show(error.ToString());
			}

			return "";
		}
	}
}