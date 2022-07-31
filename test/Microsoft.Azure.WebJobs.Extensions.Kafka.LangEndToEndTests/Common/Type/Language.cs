// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
	// Represents the supported languages for function apps using kafka extension.
	public enum Language
	{
		PYTHON,
		JAVASCRIPT,
		JAVA,
		TYPESCRIPT,
		DOTNETISOLATED,
		POWERSHELL,
		DOTNET
	}
}
