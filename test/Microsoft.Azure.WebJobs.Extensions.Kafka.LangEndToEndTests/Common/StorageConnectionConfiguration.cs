// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

internal static class StorageConnectionConfiguration
{
	public const string QueueTestConnectionEnvironmentVariable = "AzureStorageQueueTestConnection";
	public const string DevelopmentStorageConnectionString = "UseDevelopmentStorage=true";

	private const string AzuriteAccountName = "devstoreaccount1";
	[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Well known public Azurite emulator key. Used for local testing only.")]
	private const string AzuriteAccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

	public static string GetFunctionAppConnectionString()
	{
		var configuredConnectionString = Environment.GetEnvironmentVariable(Constants.AZURE_WEBJOBS_STORAGE);
		if (string.Equals(configuredConnectionString, DevelopmentStorageConnectionString, StringComparison.OrdinalIgnoreCase))
		{
			return BuildAzuriteConnectionString(Constants.AZURITE_CONTAINER_HOSTNAME);
		}

		return configuredConnectionString ?? BuildAzuriteConnectionString(Constants.AZURITE_CONTAINER_HOSTNAME);
	}

	public static string GetQueueTestConnectionString()
	{
		return Environment.GetEnvironmentVariable(QueueTestConnectionEnvironmentVariable)
			?? Environment.GetEnvironmentVariable(Constants.AZURE_WEBJOBS_STORAGE)
			?? BuildAzuriteConnectionString(Constants.AZURITE_HOSTNAME_FROM_TEST_PROCESS);
	}

	private static string BuildAzuriteConnectionString(string hostName)
	{
		return $"DefaultEndpointsProtocol=http;AccountName={AzuriteAccountName};AccountKey={AzuriteAccountKey};BlobEndpoint=http://{hostName}:10000/{AzuriteAccountName};QueueEndpoint=http://{hostName}:10001/{AzuriteAccountName};TableEndpoint=http://{hostName}:10002/{AzuriteAccountName};";
	}
}