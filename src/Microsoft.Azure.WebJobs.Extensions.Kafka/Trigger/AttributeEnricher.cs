// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using System;
using System.Data.Common;
using System.Globalization;
using System.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class AttributeEnricher
    {
        public virtual void Enrich<T>(T attribute, IConfiguration config, INameResolver nameResolver) where T : IAttributeWithConfigurationString
        {

            if (!string.IsNullOrWhiteSpace(attribute.ConfigurationString))
            {
                var builder = new DbConnectionStringBuilder();
                builder.ConnectionString = attribute.ConfigurationString;
                foreach (string key in builder.Keys)
                {
                    var prop = typeof(T).GetProperties().Where(x => x.CanWrite).Where(x => x.Name.Equals(key, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (prop != null)
                    {
                        var propertyType = prop.PropertyType;
                        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            propertyType = propertyType.GetGenericArguments()[0];
                        }
                        var value = Convert.ToString(builder[key]);
                        value = config.ResolveSecureSetting(nameResolver, value);
                        object propValue = null;
                        if (propertyType.IsEnum)
                        {
                            propValue = Enum.Parse(propertyType, value, true);
                        }
                        else
                        {
                            propValue = Convert.ChangeType(value, propertyType, CultureInfo.InvariantCulture);
                        }
                        prop.SetValue(attribute, propValue);
                    }
                }
            }
        }         
    }
}