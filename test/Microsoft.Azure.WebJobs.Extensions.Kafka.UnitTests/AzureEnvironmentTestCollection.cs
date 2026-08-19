// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    [CollectionDefinition(Name, DisableParallelization = true)]
    public class AzureEnvironmentTestCollection
    {
        public const string Name = "Azure environment tests";
    }
}
