// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class AzureFunctionsFileHelperTest : IDisposable
    {
        Dictionary<string, string> envVarsToRestore = new Dictionary<string, string>();

        private void SetEnvironmentVariable(string variable, string value)
        {
            if (!this.envVarsToRestore.ContainsKey(variable))
            {
                this.envVarsToRestore[variable] = Environment.GetEnvironmentVariable(variable);
            }

            Environment.SetEnvironmentVariable(variable, value, EnvironmentVariableTarget.Process);
        }

        void SetRunningInAzureEnvVars()
        {
            SetEnvironmentVariable(AzureFunctionsFileHelper.AzureFunctionRuntimeVersionEnvVarName, "~2");
            SetEnvironmentVariable(AzureFunctionsFileHelper.AzureWebSiteHostNameEnvVarName, "kafka.azurewebsites.net");
        }

        public void Dispose()
        {
            foreach (var kv in envVarsToRestore)
            {
                Environment.SetEnvironmentVariable(kv.Key, kv.Value, EnvironmentVariableTarget.Process);
            }

            this.envVarsToRestore.Clear();
        }

        [Fact]
        public void IsFunctionRunningInAzure_When_Does_Not_Have_Azure_EnvVars_Should_Returns_False()
        {
            Assert.False(AzureFunctionsFileHelper.IsFunctionRunningInAzure());
        }

        [Fact]
        public void IsFunctionRunningInAzure_When_Does_Have_Azure_EnvVars_Should_Returns_True()
        {
            SetRunningInAzureEnvVars();
            Assert.True(AzureFunctionsFileHelper.IsFunctionRunningInAzure());
        }

        [Fact]
        public void GetAzureFunctionBaseFolder_When_Not_Running_In_Azure_Should_Return_Null()
        {
            Assert.Null(AzureFunctionsFileHelper.GetAzureFunctionBaseFolder());
        }

        [Fact]
        public void GetAzureFunctionBaseFolder_When_Does_Not_Have_Home_EnvVar_Return_Null()
        {
            SetRunningInAzureEnvVars();
            SetEnvironmentVariable(AzureFunctionsFileHelper.AzureHomeEnvVarName, null);
            Assert.Null(AzureFunctionsFileHelper.GetAzureFunctionBaseFolder());
        }

        [Fact]
        public void GetAzureFunctionBaseFolder_When_Running_In_Azure_Should_Return_Not_Null()
        {
            SetRunningInAzureEnvVars();
            SetEnvironmentVariable(AzureFunctionsFileHelper.AzureHomeEnvVarName, @"d:\Home");

            var actual = AzureFunctionsFileHelper.GetAzureFunctionBaseFolder();
            Assert.NotEmpty(actual);
            Assert.Equal(@"d:\Home/site/wwwroot", actual);
        }
    }
}