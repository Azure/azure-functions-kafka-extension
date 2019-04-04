// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaTests
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
        
        //[Fact]
        //public void GetBindingData_MultipleDispatch_ReturnsExpectedValue()
        //{
        //    var events = new KafkaEventData[3]
        //    {
        //        new KafkaEventData()
        //        {
        //            Key = 1,
        //            Value = "hello world"
        //        },
        //        new KafkaEventData()
        //        {
        //            Key = 2,
        //            Value = "hello world 2"
        //        },
        //        new KafkaEventData()
        //        {
        //            Key = 3,
        //            Value = "hello world 3"
        //        }
        //    };
            
        //    var input = new KafkaTriggerInput
        //    {
        //        Events = events
        //    };

        //    var strategy = new KafkaTriggerBindingStrategy();
        //    var bindingData = strategy.GetBindingData(input);

        //    // To be implemented

        //    Assert.Equal(3, bindingData.Count);
        //}
    }
}
