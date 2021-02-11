// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaOptionsTest
    {
        [Fact]
        public void When_Pass_The_Value_Available_For_CompressionType()
        {
            var options = new KafkaOptions();
            Assert.Equal("None", options.CompressionType);
            options.CompressionType = "Gzip";
            Assert.True(true);
        }

        [Fact]
        public void When_Pass_The_Value_Not_Available_For_CompressionType()
        {
            var options = new KafkaOptions();
            Assert.Throws<InvalidOperationException>(() => {
                options.CompressionType = "Not Available";
            });
        }
    }
}
