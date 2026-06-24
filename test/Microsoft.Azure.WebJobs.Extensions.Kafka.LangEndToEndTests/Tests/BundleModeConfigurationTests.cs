// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;
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
		var configuration = new TestFunctionAppBuildConfiguration("bundle", "4.37.0");

		var command = new DockerRunCommand(BrokerType.EVENTHUB, Language.PYTHON, configuration);

		Assert.Contains("-e EXTENSION_SOURCE=bundle", command.CommandText);
		Assert.Contains("-e EXTENSION_BUNDLE_VERSION=4.37.0", command.CommandText);
	}

	[Fact]
	public void DockerRunCommand_DoesNotOverrideImageModeWhenConfigurationUsesDefaults()
	{
		var configuration = new TestFunctionAppBuildConfiguration(null, null);

		var command = new DockerRunCommand(BrokerType.EVENTHUB, Language.PYTHON, configuration);

		Assert.DoesNotContain("EXTENSION_SOURCE=", command.CommandText);
		Assert.DoesNotContain("EXTENSION_BUNDLE_VERSION=", command.CommandText);
	}
}
