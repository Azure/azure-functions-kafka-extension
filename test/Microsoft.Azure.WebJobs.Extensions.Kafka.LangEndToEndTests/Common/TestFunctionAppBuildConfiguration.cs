// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

public class TestFunctionAppBuildConfiguration
{
	public TestFunctionAppBuildConfiguration(string extensionSource, string extensionBundleVersion)
	{
		HasExtensionSource = !string.IsNullOrWhiteSpace(extensionSource);
		HasExtensionBundleVersion = !string.IsNullOrWhiteSpace(extensionBundleVersion);
		ExtensionSource = string.IsNullOrWhiteSpace(extensionSource) ? "package" : extensionSource.Trim();
		ExtensionBundleVersion = string.IsNullOrWhiteSpace(extensionBundleVersion)
			? "4.3.2"
			: extensionBundleVersion.Trim();
		UseExtensionBundle = string.Equals(ExtensionSource, "bundle", StringComparison.OrdinalIgnoreCase);
	}

	public static TestFunctionAppBuildConfiguration FromEnvironment()
	{
		return new TestFunctionAppBuildConfiguration(
			Environment.GetEnvironmentVariable(Constants.EXTENSION_SOURCE_ENV_VAR),
			Environment.GetEnvironmentVariable(Constants.EXTENSION_BUNDLE_VERSION_ENV_VAR));
	}

	public string ExtensionSource { get; }

	public string ExtensionBundleVersion { get; }

	public bool HasExtensionSource { get; }

	public bool HasExtensionBundleVersion { get; }

	public bool UseExtensionBundle { get; }
}
