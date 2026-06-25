// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;
using System;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests;

public class BundleModeConfigurationTests
{
	[Theory]
	[InlineData("package", "4.0.0", false)]
	[InlineData("bundle", "4.3.2", true)]
	[InlineData("BUNDLE", "4.37.0", true)]
	public void BuildConfiguration_UsesBundleModeWhenRequested(string extensionSource, string extensionBundleVersion, bool expected)
	{
		var configuration = new TestFunctionAppBuildConfiguration(extensionSource, extensionBundleVersion);

		Assert.Equal(expected, configuration.UseExtensionBundle);
		Assert.Equal(extensionBundleVersion, configuration.ExtensionBundleVersion);
	}

	[Fact]
	public void BuildConfiguration_TracksWhetherValuesWereExplicitlyProvided()
	{
		var configuration = new TestFunctionAppBuildConfiguration(null, null);

		Assert.False(configuration.HasExtensionSource);
		Assert.False(configuration.HasExtensionBundleVersion);
		Assert.Equal("package", configuration.ExtensionSource);
		Assert.Equal("4.3.2", configuration.ExtensionBundleVersion);
	}

	[Fact]
	public void DockerRunCommand_PassesBundleConfigurationAsEnvironmentVariableValues()
	{
		var previousBundleVersionRange = Environment.GetEnvironmentVariable(Constants.EXTENSION_BUNDLE_VERSION_RANGE_ENV_VAR);
		var previousBundleSourceUri = Environment.GetEnvironmentVariable(Constants.FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI);
		var configuration = new TestFunctionAppBuildConfiguration("bundle", "4.37.0");

		try
		{
			Environment.SetEnvironmentVariable(Constants.EXTENSION_BUNDLE_VERSION_RANGE_ENV_VAR, null);
			Environment.SetEnvironmentVariable(Constants.FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI, "https://cdn-staging.functions.azure.com/public");

			var command = new DockerRunCommand(BrokerType.EVENTHUB, Language.PYTHON, configuration);

			Assert.Contains("docker run -d", command.CommandText);
			Assert.Contains($"-e \"{Constants.FUNCTIONS_WORKER_RUNTIME}={Constants.PYTHONAPP_WORKER_RUNTIME}\"", command.CommandText);
			Assert.Contains("-e \"EXTENSION_SOURCE=bundle\"", command.CommandText);
			Assert.Contains("-e \"EXTENSION_BUNDLE_VERSION=4.37.0\"", command.CommandText);
			Assert.Contains("-e \"EXTENSION_BUNDLE_VERSION_RANGE=[4.37.0,5.0.0)\"", command.CommandText);
			Assert.Contains("-e \"FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI=https://cdn-staging.functions.azure.com/public\"", command.CommandText);
		}
		finally
		{
			Environment.SetEnvironmentVariable(Constants.EXTENSION_BUNDLE_VERSION_RANGE_ENV_VAR, previousBundleVersionRange);
			Environment.SetEnvironmentVariable(Constants.FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI, previousBundleSourceUri);
		}
	}

	[Fact]
	public void DockerRunCommand_UsesExplicitBundleVersionRangeWhenProvided()
	{
		var previousBundleVersionRange = Environment.GetEnvironmentVariable(Constants.EXTENSION_BUNDLE_VERSION_RANGE_ENV_VAR);

		try
		{
			Environment.SetEnvironmentVariable(Constants.EXTENSION_BUNDLE_VERSION_RANGE_ENV_VAR, "[4.37.0, 4.37.1)");

			var command = new DockerRunCommand(BrokerType.CONFLUENT, Language.JAVA,
				new TestFunctionAppBuildConfiguration("bundle", "4.37.0"));

			Assert.Contains("-e \"EXTENSION_BUNDLE_VERSION_RANGE=[4.37.0, 4.37.1)\"", command.CommandText);
		}
		finally
		{
			Environment.SetEnvironmentVariable(Constants.EXTENSION_BUNDLE_VERSION_RANGE_ENV_VAR, previousBundleVersionRange);
		}
	}

	[Fact]
	public void DockerRunCommand_DoesNotOverrideImageModeWhenConfigurationUsesDefaults()
	{
		var configuration = new TestFunctionAppBuildConfiguration(null, null);

		var command = new DockerRunCommand(BrokerType.EVENTHUB, Language.PYTHON, configuration);

		Assert.DoesNotContain("EXTENSION_SOURCE=", command.CommandText);
		Assert.DoesNotContain("EXTENSION_BUNDLE_VERSION=", command.CommandText);
	}

	[Fact]
	public void DockerRunCommand_UsesLocalKafkaAndAzuriteForConfluentPathByDefault()
	{
		var previousBrokerList = Environment.GetEnvironmentVariable(Constants.CONFLUENT_BROKERLIST_VAR);
		var previousStorage = Environment.GetEnvironmentVariable(Constants.AZURE_WEBJOBS_STORAGE);

		try
		{
			Environment.SetEnvironmentVariable(Constants.CONFLUENT_BROKERLIST_VAR, null);
			Environment.SetEnvironmentVariable(Constants.AZURE_WEBJOBS_STORAGE, null);

			var command = new DockerRunCommand(BrokerType.CONFLUENT, Language.PYTHON, new TestFunctionAppBuildConfiguration(null, null));

			Assert.Contains($"--network {Constants.LOCAL_DOCKER_NETWORK}", command.CommandText);
			Assert.Contains($"-e \"{Constants.FUNCTIONS_WORKER_RUNTIME}={Constants.PYTHONAPP_WORKER_RUNTIME}\"", command.CommandText);
			Assert.Contains($"-e \"{Constants.CONFLUENT_BROKERLIST_VAR}={Constants.LOCAL_KAFKA_BROKER_LIST}\"", command.CommandText);
			Assert.Contains($"-e \"{Constants.AZURE_WEBJOBS_STORAGE}=DefaultEndpointsProtocol=http", command.CommandText);
			Assert.Contains($"QueueEndpoint=http://{Constants.AZURITE_CONTAINER_HOSTNAME}:10001/devstoreaccount1", command.CommandText);
			Assert.DoesNotContain(Constants.CONFLUENT_USERNAME_VAR, command.CommandText);
			Assert.DoesNotContain(Constants.CONFLUENT_PASSWORD_VAR, command.CommandText);
		}
		finally
		{
			Environment.SetEnvironmentVariable(Constants.CONFLUENT_BROKERLIST_VAR, previousBrokerList);
			Environment.SetEnvironmentVariable(Constants.AZURE_WEBJOBS_STORAGE, previousStorage);
		}
	}

	[Fact]
	public void StorageConnectionConfiguration_UsesHostAzuriteEndpointForQueueVerificationByDefault()
	{
		var previousStorage = Environment.GetEnvironmentVariable(Constants.AZURE_WEBJOBS_STORAGE);
		var previousTestStorage = Environment.GetEnvironmentVariable(StorageConnectionConfiguration.QueueTestConnectionEnvironmentVariable);

		try
		{
			Environment.SetEnvironmentVariable(Constants.AZURE_WEBJOBS_STORAGE, null);
			Environment.SetEnvironmentVariable(StorageConnectionConfiguration.QueueTestConnectionEnvironmentVariable, null);

			var connectionString = StorageConnectionConfiguration.GetQueueTestConnectionString();

			Assert.Contains($"QueueEndpoint=http://{Constants.AZURITE_HOSTNAME_FROM_TEST_PROCESS}:10001/devstoreaccount1", connectionString);
		}
		finally
		{
			Environment.SetEnvironmentVariable(Constants.AZURE_WEBJOBS_STORAGE, previousStorage);
			Environment.SetEnvironmentVariable(StorageConnectionConfiguration.QueueTestConnectionEnvironmentVariable, previousTestStorage);
		}
	}

	[Fact]
	public void StorageConnectionConfiguration_ExpandsDevelopmentStorageForFunctionContainers()
	{
		var previousStorage = Environment.GetEnvironmentVariable(Constants.AZURE_WEBJOBS_STORAGE);

		try
		{
			Environment.SetEnvironmentVariable(Constants.AZURE_WEBJOBS_STORAGE,
				StorageConnectionConfiguration.DevelopmentStorageConnectionString);

			var connectionString = StorageConnectionConfiguration.GetFunctionAppConnectionString();

			Assert.Contains($"QueueEndpoint=http://{Constants.AZURITE_CONTAINER_HOSTNAME}:10001/devstoreaccount1", connectionString);
		}
		finally
		{
			Environment.SetEnvironmentVariable(Constants.AZURE_WEBJOBS_STORAGE, previousStorage);
		}
	}

	[Fact]
	public void EventHubTests_AreSkippedByDefault()
	{
		var previousDisable = Environment.GetEnvironmentVariable("DisableEventHubsTestsFlag");
		var previousEnable = Environment.GetEnvironmentVariable("EnableEventHubsTestsFlag");

		try
		{
			Environment.SetEnvironmentVariable("DisableEventHubsTestsFlag", null);
			Environment.SetEnvironmentVariable("EnableEventHubsTestsFlag", null);

			var attribute = new IgnoreOnDisableEventHubsTestsFlagFact();

			Assert.NotNull(attribute.Skip);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DisableEventHubsTestsFlag", previousDisable);
			Environment.SetEnvironmentVariable("EnableEventHubsTestsFlag", previousEnable);
		}
	}

	[Fact]
	public void EventHubTests_RunWhenExplicitlyEnabled()
	{
		var previousDisable = Environment.GetEnvironmentVariable("DisableEventHubsTestsFlag");
		var previousEnable = Environment.GetEnvironmentVariable("EnableEventHubsTestsFlag");

		try
		{
			Environment.SetEnvironmentVariable("DisableEventHubsTestsFlag", null);
			Environment.SetEnvironmentVariable("EnableEventHubsTestsFlag", "true");

			var attribute = new IgnoreOnDisableEventHubsTestsFlagFact();

			Assert.Null(attribute.Skip);
		}
		finally
		{
			Environment.SetEnvironmentVariable("DisableEventHubsTestsFlag", previousDisable);
			Environment.SetEnvironmentVariable("EnableEventHubsTestsFlag", previousEnable);
		}
	}
}
