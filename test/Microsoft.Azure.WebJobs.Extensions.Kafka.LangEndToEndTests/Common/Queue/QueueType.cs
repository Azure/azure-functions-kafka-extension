// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
	/* External resources required for the test are abstracted as queues.
	* Collection of possible queue types.
	*/
	public enum QueueType
	{
		EventHub,
		AzureStorageQueue,
		Kafka
	}
}
