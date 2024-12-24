using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

public enum LogLevel
{
    Debug,  // For debugging messages
    Info,   // For informational messages
    Warning, // For warnings
    Error,   // For error messages
    NoLog    // For disabling logging output
}

public enum LogOutput
{
    No,       //  No Output log
    Console,  // Output log to console
    File,     // Output log to file
    All       // Output log to both console and file
}

public class Logger
{
    private static readonly object _logLock = new object();  // Lock object to ensure thread-safe logging
    private static string _logFilePath = "orbbecsdk_csharp.log";  // Default log file path
    private static LogLevel _currentLogLevel = LogLevel.Error;  // Default log level (only errors are logged)
    private static long _maxLogFileSize = 10 * 1024 * 1024; // Default maximum log file size is 10MB
    private static LogOutput _currentLogOutpu = LogOutput.Console;  // Default output to console only
    private static uint _MaxBackupFiles = 5;  // Default maximum number of backup log files

    // Set the log level
    public static void SetLogLevel(LogLevel logLevel)
    {
        _currentLogLevel = logLevel;
    }

    // Set the log file path
    public static void SetLogFilePath(string path)
    {
        _logFilePath = path;
    }

    // Set the maximum log file size
    public static void SetMaxLogFileSize(long sizeInBytes)
    {
        _maxLogFileSize = sizeInBytes > 1024 ? sizeInBytes : _maxLogFileSize;
    }

    // Set the log output platform (console, file, or both)
    public static void SetLogOutput(LogOutput output)
    {
        _currentLogOutpu = output;
    }

    // Set the maximum number of backup log files
    public static void SetMaxBackupFiles(uint max)
    {
        _MaxBackupFiles = max > 10 ? 10 : max;  // Limit to a maximum of 10 backup files
        _MaxBackupFiles = max < 1 ? 1 : max;    // Ensure at least 1 backup file is retained
    }

    // Log the message according to the specified log level
    public static async void Log(LogLevel level, string message)
    {
        if (level < _currentLogLevel)
        {
            return; // Skip the log if its level is lower than the current log level
        }

        string logMessage = FormatLogMessage(level, message);

        var tasks = new List<Task>();

        // Add the write operations to the task list for each log output target
        if (LogOutput.File == _currentLogOutpu || LogOutput.All == _currentLogOutpu)
        {
            tasks.Add(Task.Run(() => WriteToFile(logMessage)));
        }

        if (LogOutput.Console == _currentLogOutpu || LogOutput.All == _currentLogOutpu)
        {
            tasks.Add(Task.Run(() => WriteToConsole(logMessage)));
        }

        if(0==tasks.Count)
            return;

        await Task.WhenAll(tasks); // Wait for all log outputs to complete
    }

    // Format the log message with timestamp and log level
    private static string FormatLogMessage(LogLevel level, string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        return $"[{timestamp}] [{level}] {message}";
    }

    // Write the log message to the log file
    private static void WriteToFile(string logMessage)
    {
        lock (_logLock)  // Ensure thread-safety
        {
            try
            {
                // Check if the log file exists, if not, create it
                if (!File.Exists(_logFilePath))
                {
                    using (var fs = File.Create(_logFilePath)) { }
                }

                // Check again if the file exists
                if (!File.Exists(_logFilePath))
                {
                    Console.WriteLine($"Failed to Create file: {_logFilePath}");
                    return;
                }

                // Check the current size of the log file
                if (new FileInfo(_logFilePath).Length > _maxLogFileSize)
                {
                    RotateLogFile(); // If the file size exceeds the limit, rotate the log file
                }

                // Append the log message to the log file
                File.AppendAllText(_logFilePath, logMessage + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // Error handling
                Console.WriteLine($"Failed to write log to file: {ex.Message}");
            }
        }
    }

    // Write the log message to the console
    private static void WriteToConsole(string logMessage)
    {
        try
        {
            // Output the log message to the console
            Console.WriteLine(logMessage);
        }
        catch (Exception ex)
        {
            // Error handling
            Console.WriteLine($"Failed to write log to console: {ex.Message}");
        }
    }

    // Perform log file rotation: rename the current log file and create a new one
    private static void RotateLogFile()
    {
        try
        {
            // Generate a timestamp for the backup file name
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFilePath = $"{_logFilePath}.{timestamp}";

            // If the backup file already exists, append a counter to ensure a unique name
            int counter = 1;
            while (File.Exists(backupFilePath))
            {
                backupFilePath = $"{_logFilePath}.{timestamp}_{counter}";
                counter++;
            }

            // Rename the current log file to the backup file
            File.Move(_logFilePath, backupFilePath);

            // Delete old backup files if there are more than the allowed limit
            CleanUpOldBackupFiles(backupFilePath);

            // Create a new log file
            using (File.Create(_logFilePath)) { } // Create new log file
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rotating log file: {ex.Message} - Unable to create file when it already exists.");
        }
    }

    // Clean up old backup log files
    private static void CleanUpOldBackupFiles(string backupFilePath)
    {
        try
        {
            var fullpath = new FileInfo(backupFilePath).FullName;
            string directoryPath = Path.GetDirectoryName(fullpath);

            // Get all backup file paths (excluding the file extension)
            string filePattern = $"{Path.GetFileNameWithoutExtension(backupFilePath)}.*";
            var backupFiles = Directory.GetFiles(directoryPath, filePattern).ToList();

            // If the number of backup files exceeds the maximum allowed, delete the oldest ones
            while (backupFiles.Count > _MaxBackupFiles)
            {
                string fileToDelete = backupFiles.First();
                File.Delete(fileToDelete);
                backupFiles.RemoveAt(0); // Remove the deleted file from the list
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up backup files: {ex.Message}");
        }
    }
}
