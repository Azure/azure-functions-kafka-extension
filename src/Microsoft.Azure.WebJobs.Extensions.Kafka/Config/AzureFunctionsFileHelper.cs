// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Helper class for file related operations in functions running in Azure
    /// </summary>
    internal static class AzureFunctionsFileHelper
    {
        internal const string AzureHomeEnvVarName = "HOME";
        internal const string AzureWebJobsScriptRootEnvVarName = "AzureWebJobsScriptRoot";
        internal const string AzureDefaultFunctionPathPart1 = "site";
        internal const string AzureDefaultFunctionPathPart2 = "wwwroot";
        internal const string AzureFunctionWorkerRuntimeEnvVarName = "FUNCTIONS_WORKER_RUNTIME";
        internal const string AzureFunctionEnvironmentEnvVarName = "AZURE_FUNCTIONS_ENVIRONMENT";
        internal const string DevelopmentEnvironmentName = "Development";
        internal const string ProcessArchitecturex86Value = "x86";
        internal const string RuntimesFolderName = "runtimes";
        internal const string NativeFolderName = "native";
        internal const string BinFolderName = "bin";
        internal const string LibrdKafkaWindowsFileName = "librdkafka.dll";
        internal const string Windows32ArchFolderName = "win-x86";
        internal const string Windows64ArchFolderName = "win-x64";
        internal const string Linux64ArchFolderName = "linux-x64";
        internal const string OSEnvVarName = "OS";
        internal const string SiteBitnessEnvVarName = "SITE_BITNESS";

        /// <summary>
        /// Indicates if the current excecution environment is either a function hosted in Azure or 
        /// a function hosted in a container
        /// </summary>
        internal static bool IsRunningAsFunctionInAzureOrContainer()
        {
            // Not running in development and has a worker runtime
            return !string.Equals(DevelopmentEnvironmentName, Environment.GetEnvironmentVariable(AzureFunctionEnvironmentEnvVarName), StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AzureFunctionWorkerRuntimeEnvVarName));
        }

        /// <summary>
        /// Gets the function base folder when running as Azure Function App or as a container
        /// When running in Azure Function App default is D:\home\site\wwwroot
        /// When running in Azure Function container default is /home/site/wwwroot
        /// If not running in Azure or container returns null
        /// </summary>
        internal static string GetFunctionBaseFolder()
        {
            if (!IsRunningAsFunctionInAzureOrContainer())
            {
                return null;
            }

            // When running in container the path will be defined at AzureWebJobsScriptRoot
            var azureWebJobsScriptRoot = Environment.GetEnvironmentVariable(AzureWebJobsScriptRootEnvVarName, EnvironmentVariableTarget.Process);
            if (!string.IsNullOrWhiteSpace(azureWebJobsScriptRoot))
            {
                return azureWebJobsScriptRoot;
            }

            // In Azure HOME contains the main folder where the function is located (windows) = D:\home
            // By default the Azure function will be located under D:\home\site\wwwroot
            var homeDir = Environment.GetEnvironmentVariable(AzureHomeEnvVarName, EnvironmentVariableTarget.Process);
            if (!string.IsNullOrWhiteSpace(homeDir))
            {
                return Path.Combine(homeDir, AzureDefaultFunctionPathPart1, AzureDefaultFunctionPathPart2);
            }

            return null;
        }

        // Holds whether or not librdkafka has been initialized
        static bool librdkafkaInitialized;

        // Ensure that we return only once the initialization is finished
        static object librdkafkaInitializationLock = new object();

        /// <summary>
        /// Initializes the librdkafka library from a specific place
        /// This address the problem that running Functions in Azure won't have the current directory where the function code is.
        /// This way we need to specifically choose the location where librdkafka is located
        /// </summary>
        internal static void InitializeLibrdKafka(ILogger logger)
        {
            lock (librdkafkaInitializationLock)
            {
                if (librdkafkaInitialized)
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Librdkafka initialization: skipping as the initialization already happened");
                    }
                    return;
                }

                librdkafkaInitialized = true;

                if (!IsRunningAsFunctionInAzureOrContainer())
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Librdkafka initialization: skipping as we are not running in Azure or a container");
                    }

                    return;
                }

                var possibleLibrdKafkaLibraryPaths = new List<string>();

                var os = Environment.GetEnvironmentVariable(OSEnvVarName, EnvironmentVariableTarget.Process) ?? string.Empty;
                var isWindows = os.IndexOf("windows", 0, StringComparison.InvariantCultureIgnoreCase) != -1;

                if (isWindows)
                {
                    var websiteBitness = Environment.GetEnvironmentVariable(SiteBitnessEnvVarName) ?? string.Empty;
                    var is32 = websiteBitness.Equals(ProcessArchitecturex86Value, StringComparison.InvariantCultureIgnoreCase);
                    var architectureFolderName = is32 ? Windows32ArchFolderName : Windows64ArchFolderName;

                    var functionBaseFolder = GetFunctionBaseFolder();

                    // Functions v2 have the runtime under 'D:\home\site\wwwroot\runtimes'
                    possibleLibrdKafkaLibraryPaths.Add(Path.Combine(functionBaseFolder, RuntimesFolderName, architectureFolderName, NativeFolderName, LibrdKafkaWindowsFileName));

                    // Functions v3 have the runtime under 'D:\home\site\wwwroot\bin\runtimes'
                    possibleLibrdKafkaLibraryPaths.Add(Path.Combine(functionBaseFolder, BinFolderName, RuntimesFolderName, architectureFolderName, NativeFolderName, LibrdKafkaWindowsFileName));
                }
                else
                {
                    logger.LogInformation("Librdkafka initialization: running in non-Windows OS, expecting librdkafka to be there");
                }

                if (possibleLibrdKafkaLibraryPaths.Count > 0)
                {
                    foreach (var librdKafkaLibraryPath in possibleLibrdKafkaLibraryPaths)
                    {
                        if (File.Exists(librdKafkaLibraryPath))
                        {
                            logger.LogDebug("Librdkafka initialization: loading librdkafka from {librdkafkaPath}", librdKafkaLibraryPath);
                            Confluent.Kafka.Library.Load(librdKafkaLibraryPath);
                            return;
                        }
                    }

                    logger.LogError("Librdkafka initialization: did not attempt to load librdkafka because the desired file(s) does not exist: '{searchedPaths}'", string.Join(",", possibleLibrdKafkaLibraryPaths));
                }
                else
                {
                    logger.LogInformation("Librdkafka initialization: could not find dll location");
                }
            }            
        }

        /// <summary>
        /// Ensure file exists, checking for azure function base folder if not found in provided path
        /// </summary>
        /// <param name="filePath">The file path to validate</param>
        /// <param name="resultFilePath">The valid file path</param>
        /// <returns>True if the file returned by <paramref name="resultFilePath"/> exists, otherwise false</returns>
        internal static bool TryGetValidFilePath(string filePath, out string resultFilePath)
        {
            resultFilePath = null;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            if (File.Exists(filePath))
            {
                resultFilePath = filePath;
                return true;
            }

            // try to search for in Azure function or container folder
            var basePath = GetFunctionBaseFolder();
            if (string.IsNullOrWhiteSpace(basePath))
            {
                return false;
            }

            var filename = Path.GetFileName(filePath);
            var filePathInAzureFunctionFolder = Path.Combine(basePath, filename);
            if (File.Exists(filePathInAzureFunctionFolder))
            { 
                resultFilePath = filePathInAzureFunctionFolder;
                return true;
            }

            return false;
        }        
    }
}