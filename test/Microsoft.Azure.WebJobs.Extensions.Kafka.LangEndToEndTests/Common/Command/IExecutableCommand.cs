// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Interface for all executable commands.
public interface IExecutableCommand<Type>
{
	Task<Type> ExecuteCommandAsync();
}