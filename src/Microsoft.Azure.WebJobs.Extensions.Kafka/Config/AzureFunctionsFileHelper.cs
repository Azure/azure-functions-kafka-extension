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
        internal const string AzureDefaultFunctionPath = "site/wwwroot";
        internal const string AzureFunctionRuntimeVersionEnvVarName = "FUNCTIONS_EXTENSION_VERSION";
        internal const string AzureWebSiteHostNameEnvVarName = "WEBSITE_HOSTNAME";
        internal const string ProcessArchitecturex86Value = "x86";
        internal const string RuntimesFolderName = "runtimes";
        internal const string NativeFolderName = "native";
        internal const string LibrdKafkaWindowsFileName = "librdkafka.dll";
        internal const string LibrdKafkaLinuxFileName = "librdkafka.so";
        internal const string Windows32ArchFolderName = "win-x86";
        internal const string Windows64ArchFolderName = "win-x64";
        internal const string Linux64ArchFolderName = "linux-x64";
        internal const string OSEnvVarName = "OS";
        internal const string SiteBitnessEnvVarName = "SITE_BITNESS";

        /// <summary>
        /// Indicates if the current excecution environment is inside a Function running in Azure
        /// </summary>
        internal static bool IsFunctionRunningInAzure()
        {
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AzureFunctionRuntimeVersionEnvVarName)) &&
                   !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AzureWebSiteHostNameEnvVarName));
        }

        /// <summary>
        /// Gets the Azure Function base folder
        /// Default is D:\home\site\wwwroot
        /// If not running in Azure as a function returns a different value
        /// </summary>
        internal static string GetAzureFunctionBaseFolder()
        {
            if (!IsFunctionRunningInAzure())
            {
                return null;
            }
                
            // In Azure HOME contains the main folder where the function is located (windows) = D:\home
            // By default the Azure function will be located under D:\home\site\wwwroot
            var homeDir = Environment.GetEnvironmentVariable(AzureHomeEnvVarName, EnvironmentVariableTarget.Process);
            if (!string.IsNullOrWhiteSpace(homeDir))
            {
                return Path.Combine(homeDir, AzureDefaultFunctionPath);
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

            if (IsFunctionRunningInAzure())
            {
                string librdKafkaLibraryPath = null;

                var os = Environment.GetEnvironmentVariable(OSEnvVarName, EnvironmentVariableTarget.Process) ?? string.Empty;
                var isWindows = os.IndexOf("windows", 0, StringComparison.InvariantCultureIgnoreCase) != -1;
                
                if (isWindows)
                {
                    var websiteBitness = Environment.GetEnvironmentVariable(SiteBitnessEnvVarName) ?? string.Empty;
                    var is32 = websiteBitness.Equals(ProcessArchitecturex86Value, StringComparison.InvariantCultureIgnoreCase);
                    var architectureFolderName = is32 ? Windows32ArchFolderName : Windows64ArchFolderName;
                    librdKafkaLibraryPath = Path.Combine(GetAzureFunctionBaseFolder(), RuntimesFolderName, architectureFolderName, NativeFolderName, LibrdKafkaWindowsFileName);
                }
                else
                {
                    // for now we just assume that is linux
                    librdKafkaLibraryPath = Path.Combine(GetAzureFunctionBaseFolder(), RuntimesFolderName, Linux64ArchFolderName, NativeFolderName, LibrdKafkaLinuxFileName);
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
        }
    }
}