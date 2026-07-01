using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace EsACaso
{
    /// <summary>
    /// Simple static logger that writes timestamped messages to a log file and optionally to Debug output.
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logFilePath;

        static Logger()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string logDir = Path.Combine(appData, "EsACaso", "logs");
                Directory.CreateDirectory(logDir);
                _logFilePath = Path.Combine(logDir, $"app_{DateTime.Now:yyyy-MM-dd}.log");
               // WriteInfo("Logger initialized.");
            }
            catch (Exception ex)
            {
                // If logging fails, we can't do much; at least output to Debug.
                Debug.WriteLine($"Logger initialization failed: {ex}");
            }
        }

        /// <summary>
        /// Writes an informational message.
        /// </summary>
        public static void Info(string message) => WriteLevel("INFO", message);

        /// <summary>
        /// Writes a warning message.
        /// </summary>
        public static void Warn(string message) => WriteLevel("WARN", message);

        /// <summary>
        /// Writes an error message.
        /// </>
        public static void Error(string message) => WriteLevel("ERROR", message);

        /// <summary>
        /// Writes an error message along with exception details.
        /// </summary>
        public static void Error(Exception ex) => WriteLevel("ERROR", $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");

        private static void WriteLevel(string level, string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // If file logging fails, try to output to Debug only.
                }
                Debug.WriteLine(line);
            }
        }
    }
}