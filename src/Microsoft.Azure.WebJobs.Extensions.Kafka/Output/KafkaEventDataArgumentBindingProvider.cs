// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Bindings;
using System.Reflection;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaEventDataArgumentBindingProvider : IKafkaProducerBindingProvider
    {
        public IArgumentBinding<KafkaProducerEntity> TryCreate(ParameterInfo parameter)
        {
            if (!parameter.IsOut)
            {
                return null;
            }

            var parameterType = parameter.ParameterType;
            while (parameterType.HasElementType)
            {
                parameterType = parameterType.GetElementType();
            }

            if (!parameterType.IsGenericType || !typeof(IKafkaEventData).IsAssignableFrom(parameterType))
            {
                return null;
            }

            return new KafkaEventDataArgumentBinding();
        }
    }
}