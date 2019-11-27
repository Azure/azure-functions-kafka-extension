// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Helper class for file related operations in functions running in Azure
    /// </summary>
    internal sealed class AzureFunctionsFileHelper
    {
        internal const string AzureHomeEnvVarName = "HOME";
        internal const string AzureWebJobsScriptRootEnvVarName = "AzureWebJobsScriptRoot";
        internal const string AzureDefaultFunctionPathPart1 = "site";
        internal const string AzureDefaultFunctionPathPart2 = "wwwroot";
        internal const string AzureFunctionWorkerRuntimeEnvVarName = "FUNCTIONS_WORKER_RUNTIME";
        internal const string ProcessArchitecturex86Value = "x86";
        internal const string RuntimesFolderName = "runtimes";
        internal const string NativeFolderName = "native";
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
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AzureFunctionWorkerRuntimeEnvVarName));
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
        static int librdkafkaInitialized;

        /// <summary>
        /// Initializes the librdkafka library from a specific place
        /// This address the problem that running Functions in Azure won't have the current directory where the function code is.
        /// This way we need to specifically choose the location where librdkafka is located
        /// </summary>
        internal static void InitializeLibrdKafka(ILogger logger)
        {
            // Initialize a single time
            var originalInitializedValue = Interlocked.Exchange(ref librdkafkaInitialized, 1);
            if (originalInitializedValue == 1)
            {
                return;
            }

            if (!IsRunningAsFunctionInAzureOrContainer())
            {
                return;
            }

            string librdKafkaLibraryPath = null;

            var os = Environment.GetEnvironmentVariable(OSEnvVarName, EnvironmentVariableTarget.Process) ?? string.Empty;
            var isWindows = os.IndexOf("windows", 0, StringComparison.InvariantCultureIgnoreCase) != -1;
                
            if (isWindows)
            {
                var websiteBitness = Environment.GetEnvironmentVariable(SiteBitnessEnvVarName) ?? string.Empty;
                var is32 = websiteBitness.Equals(ProcessArchitecturex86Value, StringComparison.InvariantCultureIgnoreCase);
                var architectureFolderName = is32 ? Windows32ArchFolderName : Windows64ArchFolderName;
                librdKafkaLibraryPath = Path.Combine(GetFunctionBaseFolder(), RuntimesFolderName, architectureFolderName, NativeFolderName, LibrdKafkaWindowsFileName);
            }
            else
            {
                logger.LogInformation("Running in non-Windows OS, expecting librdkafka to be there");
            }

            if (librdKafkaLibraryPath != null)
            {
                if (File.Exists(librdKafkaLibraryPath))
                {
                    logger.LogInformation("Loading librdkafka from {librdkafkaPath}", librdKafkaLibraryPath);
                    Confluent.Kafka.Library.Load(librdKafkaLibraryPath);
                }
                else
                {
                    logger.LogError("Did not attempt to load librdkafka because the desired file does not exist: '{librdkafkaPath}'", librdKafkaLibraryPath);
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