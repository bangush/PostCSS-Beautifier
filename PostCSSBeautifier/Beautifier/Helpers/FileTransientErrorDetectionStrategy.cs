using Microsoft.Practices.TransientFaultHandling;
using System;

namespace PostCSSBeautifier.Helpers
{
	public class FileTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        public bool IsTransient(Exception ex)
        {
            return true;
        }
    }
}