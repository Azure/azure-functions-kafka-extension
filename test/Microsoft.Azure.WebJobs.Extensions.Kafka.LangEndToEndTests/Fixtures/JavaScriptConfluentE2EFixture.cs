// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Common fixture for all test cases for JavaScript using Confluent as kafka provider
public class JavaScriptConfluentE2EFixture : KafkaE2EFixture
{
	public JavaScriptConfluentE2EFixture() : base(BrokerType.CONFLUENT, Language.JAVASCRIPT) { }
}