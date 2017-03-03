using PostCSSBeautifier.Compiler.Result;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PostCSSBeautifier.Compiler
{
	public static class CompilerResultFactory
    {
        public async static Task<CompilerResult> GenerateResult(string sourceFileName, string targetFileName)
        {
            return await GenerateResult(sourceFileName, targetFileName, null, true, null, null, null);
        }

        public async static Task<CompilerResult> GenerateResult(string sourceFileName, string targetFileName, bool isSuccess, string result, IEnumerable<CompilerError> errors)
        {
            return await GenerateResult(sourceFileName, targetFileName, null, isSuccess, result, null, errors);
        }

        public async static Task<CompilerResult> GenerateResult(string sourceFileName, string targetFileName, string mapFileName, bool isSuccess, string result, string resultMap, IEnumerable<CompilerError> errors, bool hasResult = false, string rtlSourceFileName = "", string rtlTargetFileName = "", string rtlMapFileName = "", string rtlResult = "", string rtlResultMap = "")
        {
            var instance = CompilerResult.GenerateResult(sourceFileName, targetFileName, mapFileName, isSuccess, result, resultMap, errors, hasResult);

            return instance;
        }
    }
}