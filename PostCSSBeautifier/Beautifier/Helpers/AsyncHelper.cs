using System;
using System.Threading;
using System.Threading.Tasks;

namespace PostCSSBeautifier.Helpers
{
	internal static class AsyncHelper
	{
		private static readonly TaskFactory MyTaskFactory = new
			TaskFactory(CancellationToken.None,
				TaskCreationOptions.None,
				TaskContinuationOptions.None,
				TaskScheduler.Default);

		public static TResult RunSync<TResult>(Func<Task<TResult>> func)
		{
			return AsyncHelper.MyTaskFactory
				.StartNew<Task<TResult>>(func)
				.Unwrap<TResult>()
				.GetAwaiter()
				.GetResult();
		}

		public static void RunSync(Func<Task> func)
		{
			AsyncHelper.MyTaskFactory
				.StartNew<Task>(func)
				.Unwrap()
				.GetAwaiter()
				.GetResult();
		}

		public static async Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout)
		{
			if (task == await Task.WhenAny(task, Task.Delay(timeout)))
			{
				return await task;
			}
			throw new TimeoutException();
		}
	}
}