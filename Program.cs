using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using NLog; // Use NLog for logging functionalities.

namespace FolderSync
{
    class Program
    {
        // Create a logger instance to log messages.
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            // Initialize NLog configuration for logging to a file.
            var config = new NLog.Config.LoggingConfiguration();
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "sync.log" };
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            LogManager.Configuration = config; 

            // The minimum required number of arguments.
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: FolderSync.exe <source> <replica> [--interval seconds] [--log logfilePath]");
                return; 
            }

            // Extract the source and replica folder paths from the arguments.
            string sourcePath = args[0];
            string replicaPath = args[1];
            int interval = 60; 

            // Iterate through optional arguments to adjust interval and log file path.
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--interval" && i + 1 < args.Length)
                {
                    // Update the synchronization interval based on user input.
                    interval = int.Parse(args[i + 1]);
                }
                if (args[i] == "--log" && i + 1 < args.Length)
                {
                    // Update the log file path based on user input.
                    logfile.FileName = args[i + 1];
                }
            }

            if (!Directory.Exists(sourcePath))
            {
                logger.Error($"Source directory does not exist: {sourcePath}");
                return;
            }

            if (!Directory.Exists(replicaPath))
            {
                Directory.CreateDirectory(replicaPath);
                logger.Info($"Replica directory created: {replicaPath}");
            }

            // Enter an infinite loop to continuously synchronize folders.
            while (true)
            {
                SyncFolders(sourcePath, replicaPath); 
                Thread.Sleep(interval * 1000);
            }
        }


        // Synchronizes files from the source directory to the replica directory.
        private static void SyncFolders(string source, string replica)
        {
            foreach (string srcFilePath in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                // Determine the relative path of the file from the source directory.
                string relativePath = srcFilePath.Substring(source.Length + 1);
                // Construct the corresponding file path in the replica directory.
                string dstFilePath = Path.Combine(replica, relativePath);

                string dstDirPath = Path.GetDirectoryName(dstFilePath);
                if (!Directory.Exists(dstDirPath))
                {
                    Directory.CreateDirectory(dstDirPath);
                    logger.Info($"Directory created: {dstDirPath}");
                }

                if (!File.Exists(dstFilePath) || File.GetLastWriteTime(srcFilePath) > File.GetLastWriteTime(dstFilePath))
                {
                    File.Copy(srcFilePath, dstFilePath, true);
                    logger.Info($"Copied/Updated: {srcFilePath} to {dstFilePath}");
                }
            }

            // Remove files in the replica directory that do not exist in the source directory.
            foreach (string dstFilePath in Directory.GetFiles(replica, "*", SearchOption.AllDirectories))
            {
                string relativePath = dstFilePath.Substring(replica.Length + 1);
                string srcFilePath = Path.Combine(source, relativePath);

                if (!File.Exists(srcFilePath))
                {
                    File.Delete(dstFilePath);
                    logger.Info($"Deleted: {dstFilePath}");
                }
            }

            // Remove empty directories in the replica directory that do not have corresponding directories in the source directory.
            foreach (string directory in Directory.GetDirectories(replica, "*", SearchOption.AllDirectories))
            {
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                    logger.Info($"Directory deleted: {directory}");
                }
            }
        }
    }
}
