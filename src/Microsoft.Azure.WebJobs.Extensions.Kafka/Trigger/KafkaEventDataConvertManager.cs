// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avro.Generic;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaEventDataConvertManager : IConverterManager
    {
        private readonly IConverterManager converterManager;
        private readonly ILogger logger;

        internal KafkaEventDataConvertManager(ILogger logger)
        {
        }

        public KafkaEventDataConvertManager(IConverterManager converterManager, ILogger logger)
        {
            this.converterManager = converterManager;
            this.logger = logger;
        }

        public FuncAsyncConverter GetConverter<TAttribute>(Type typeSource, Type typeDest) where TAttribute : Attribute
        {
            if (typeof(IKafkaEventData).IsAssignableFrom(typeSource))
            {
                if (typeDest == typeSource)
                {
                    return ConvertToSame;
                }
                else if (typeDest == typeof(string))
                {
                    return ConvertToString;
                }
                else if (typeDest == typeof(byte[]))
                {
                    return ConvertToByteArray;
                }
                else if (typeof(IKafkaEventData).IsAssignableFrom(typeSource) && typeSource.IsGenericType)
                {
                    var sourceGenericTypes = typeSource.GetGenericArguments();
                    var sourceValueType = sourceGenericTypes.Last();
                    if (sourceValueType == typeDest)
                    {
                        return ConvertToSameGenericSourceType;
                    }
                    else if (typeof(IKafkaEventData).IsAssignableFrom(typeDest) && typeDest.IsGenericType && typeDest.GetGenericArguments().Last() == sourceValueType)
                    {
                        var genericMethod = this.GetType().GetMethod(nameof(ConvertKafkaEventDataType), BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(sourceValueType);
                        var converter = (FuncAsyncConverter)genericMethod.CreateDelegate(typeof(FuncAsyncConverter));
                        return converter;
                    }
                }
            }

            return this.converterManager.GetConverter<TAttribute>(typeSource, typeDest);
        }
        
        public static Task<object> ConvertKafkaEventDataType<TValue>(object src, Attribute attribute, ValueBindingContext context)
        {
            var srcEventData = (IKafkaEventData)src;
            return Task.FromResult<object>(new KafkaEventData<TValue>(srcEventData));
        }

        private Task<object> ConvertToSameGenericSourceType(object src, Attribute attribute, ValueBindingContext context)
        {
            return Task.FromResult(((IKafkaEventData)src).Value);
        }

        private Task<object> ConvertToByteArray(object src, Attribute attribute, ValueBindingContext context)
        {
            object result = null;
            var value = ((IKafkaEventData)src).Value;
            if (value is byte[] bytes)
            {
                result = bytes;
            }
            else if (value is string stringValue)
            {
                result = Encoding.UTF8.GetBytes(stringValue);
            }
            else
            {
                logger.LogWarning($@"Unable to convert incoming data to byte[] as underlying data stream was not byte[]. Returning [null]");
            }
            
            return Task.FromResult(result);
        }

        private Task<object> ConvertToSame(object src, Attribute attribute, ValueBindingContext context)
        {
            return Task.FromResult<object>(src);
        }

        private Task<object> ConvertToString(object src, Attribute attribute, ValueBindingContext context)
        {
            try
            {
                object result = null;
                var value = ((IKafkaEventData)src).Value;
                if (value is byte[] binaryContent)
                {
                    if (binaryContent != null)
                    {
                        result = Encoding.UTF8.GetString(binaryContent);
                    }
                    else
                    {
                        result = string.Empty;
                    }
                }
                else if (value is GenericRecord genericRecord)
                {
                    result = GenericRecord2String(genericRecord);
                }
                else
                {
                    result = JsonConvert.SerializeObject(src);
                }

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $@"Unable to convert incoming data to string.");
                throw;
            }
        }

        private string GenericRecord2String(GenericRecord record)
        {
            var props = new Dictionary<string, object>();
            foreach (var field in record.Schema.Fields)
            {
                if (record.TryGetValue(field.Name, out var value))
                {
                    props[field.Name] = value;
                }
            }

            return JsonConvert.SerializeObject(props);
        }
    }
}
