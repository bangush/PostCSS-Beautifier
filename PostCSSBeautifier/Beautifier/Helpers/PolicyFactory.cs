using Microsoft.Practices.TransientFaultHandling;
using System;

namespace PostCSSBeautifier.Helpers
{
	public static class PolicyFactory
    {
        public static RetryPolicy GetPolicy(ITransientErrorDetectionStrategy strategy, int retryCount)
        {
            var policy = new RetryPolicy(strategy, retryCount, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));

            return policy;
        }
    }
}