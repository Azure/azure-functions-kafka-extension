// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests.Helpers
{
    internal static class AzureEnvironment
    {
        private static Dictionary<string, string> envVarsToRestore = new Dictionary<string, string>();

        internal static void SetEnvironmentVariable(string variable, string value)
        {
            if (!envVarsToRestore.ContainsKey(variable))
            {
                envVarsToRestore[variable] = Environment.GetEnvironmentVariable(variable);
            }

            Environment.SetEnvironmentVariable(variable, value, EnvironmentVariableTarget.Process);
        }

        internal static void SetRunningInAzureEnvVars()
        {
            SetEnvironmentVariable(AzureFunctionsFileHelper.AzureFunctionWorkerRuntimeEnvVarName, "dotnet");
        }

        internal static void ClearEnvironmentVariables()
        {
            foreach (var kv in envVarsToRestore)
            {
                Environment.SetEnvironmentVariable(kv.Key, kv.Value, EnvironmentVariableTarget.Process);
            }

            envVarsToRestore.Clear();
        }
    }
}
