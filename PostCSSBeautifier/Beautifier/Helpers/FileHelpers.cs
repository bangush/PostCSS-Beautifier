using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PostCSSBeautifier.Helpers
{
	public static class FileHelpers
    {

        /// <summary>
        /// Opens a text file,
        /// tries reading file 500 times before throwing IO Exception,
        /// and then closes the file.
        /// </summary>
        /// <param name="fileName">The file to open for reading.</param>
        /// <returns>Task which ultimately returns a string containing all lines of the file.</returns>
        public static async Task<string> ReadAllTextRetry(string fileName, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            // If the file doesn't exists we don't need to try 500 time to open it.
            if (!File.Exists(fileName))
                return null;

            const int retryCount = 500;

            try
            {
                // Keep the previous behavior if no encoding is provided.
                return await PolicyFactory.GetPolicy(new FileTransientErrorDetectionStrategy(), retryCount)
                    .ExecuteAsync(() =>
                        encoding == null
                            ? Task.FromResult(File.ReadAllText(fileName))
                            : Task.FromResult(File.ReadAllText(fileName, encoding))
                    );
            }
            catch (IOException)
            {
                Logger.Log("Exception: Tried " + retryCount + " times for reading, but the file " + fileName +
                           " is still in use. Exiting gracefully.");
            }

            return string.Empty;
        }

        /// <summary>
        /// Creates a new file, writes the specified string to the file, and then closes
        /// the file. If the target file already exists, it is overwritten. If the target
        /// file is in use, try 500 times before throwing IO Exception.
        /// </summary>
        /// <param name="fileName">The file to open for reading.</param>
        /// <param name="contents">The string to write to the file.</param>
        public static async Task WriteAllTextRetry(string fileName, string contents, bool withBOM = true)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            const int retryCount = 500;

            try
            {
                await PolicyFactory.GetPolicy(new FileTransientErrorDetectionStrategy(), retryCount)
                    .ExecuteAsync(() => Task.Run(() => File.WriteAllText(fileName, contents, new UTF8Encoding(withBOM))));
            }
            catch (IOException)
            {
                Logger.Log("Exception: Tried " + retryCount + " times for writing, but the file " + fileName +
                           " is still in use. Exiting gracefully.");
            }
        }

    }
}