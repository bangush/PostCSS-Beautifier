using System.Collections.Generic;
using System.Linq;

namespace PostCSSBeautifier.Compiler.Result
{
	public class CompilerError
    {
        public string FileName { get; set; }
        public string Message { get; set; }
        public string FullMessage { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public class CompilerResult
    {
        public string SourceFileName { get; protected set; }
        public string TargetFileName { get; protected set; }
        public string MapFileName { get; protected set; }
        public bool IsSuccess { get; protected set; }
        public bool HasSkipped { get; protected set; }
        public IEnumerable<CompilerError> Errors { get; protected set; }
        public string Result { get; private set; }
        public string ResultMap { get; private set; }

        // RTL variants
        public string RtlSourceFileName { get; set; }
        public string RtlTargetFileName { get; set; }
        public string RtlMapFileName { get; set; }
        public string RtlResult { get; set; }
        public string RtlResultMap { get; set; }

        protected CompilerResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, string resultMap, IEnumerable<CompilerError> errors, bool hasSkipped)
        {
            SourceFileName = sourceFileName;
            TargetFileName = targetFileName;
            MapFileName = mapFileName;
            Errors = errors ?? Enumerable.Empty<CompilerError>();
            IsSuccess = isSuccess;
            Result = result;
            ResultMap = resultMap;
            HasSkipped = hasSkipped;
        }

        protected CompilerResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, string resultMap, IEnumerable<CompilerError> errors, bool hasSkipped, string rtlSourceFileName, string rtlTargetFileName, string rtlMapFileName, string rtlResult, string rtlResultMap)
            : this(sourceFileName, targetFileName, mapFileName, isSuccess, result, resultMap, errors, hasSkipped)
        {
            RtlSourceFileName = rtlSourceFileName;
            RtlTargetFileName = rtlTargetFileName;
            RtlMapFileName = rtlMapFileName;
            RtlResult = rtlResult;
            RtlResultMap = rtlResultMap;
        }

        public static CompilerResult GenerateResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, string resultMap, IEnumerable<CompilerError> errors, bool hasSkipped = false)
        {
            return new CompilerResult(sourceFileName, targetFileName, mapFileName, isSuccess, result, resultMap, errors, hasSkipped);
        }
    }
}