// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaTriggerBindingStrategyTest
    {
        [Fact]
        public void GetStaticBindingContract_ReturnsExpectedValue()
        {
            var strategy = new KafkaTriggerBindingStrategy();
            var contract = strategy.GetBindingContract();

            Assert.Equal(4, contract.Count);
            Assert.Equal(typeof(object), contract["Key"]);
            Assert.Equal(typeof(int), contract["Partition"]);
            Assert.Equal(typeof(string), contract["Topic"]);
            Assert.Equal(typeof(DateTime), contract["Timestamp"]);
        }

        [Fact]
        public void GetBindingContract_SingleDispatch_ReturnsExpectedValue()
        {
            var strategy = new KafkaTriggerBindingStrategy();
            var contract = strategy.GetBindingContract(true);

            Assert.Equal(4, contract.Count);
            Assert.Equal(typeof(object), contract["Key"]);
            Assert.Equal(typeof(int), contract["Partition"]);
            Assert.Equal(typeof(string), contract["Topic"]);
            Assert.Equal(typeof(DateTime), contract["Timestamp"]);
        }
    }
}
