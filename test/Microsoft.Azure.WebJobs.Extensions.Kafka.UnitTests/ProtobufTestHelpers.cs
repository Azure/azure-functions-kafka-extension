// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Google.Protobuf;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    internal static class ProtobufTestHelpers
    {
        internal static bool ContainsTopLevelField(byte[] bytes, int fieldNumber)
        {
            var input = new CodedInputStream(bytes);
            uint tag;

            while ((tag = input.ReadTag()) != 0)
            {
                if (WireFormat.GetTagFieldNumber(tag) == fieldNumber)
                {
                    return true;
                }

                input.SkipLastField();
            }

            return false;
        }
    }
}
