// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Common fixture for all test cases for Java using Eventhub as kafka provider
public class JavaEventhubE2EFixture : KafkaE2EFixture
{
	public JavaEventhubE2EFixture() : base(BrokerType.EVENTHUB, Language.JAVA) { }
}