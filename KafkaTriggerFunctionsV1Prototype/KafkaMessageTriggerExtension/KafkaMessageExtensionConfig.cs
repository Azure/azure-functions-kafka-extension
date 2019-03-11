using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Config;

namespace KafkaMessageTriggerExtension
{
    public class KafkaMessageExtensionConfig : IExtensionConfigProvider
    {
        private TraceWriter _tracer;

        /// <summary>
        ///     Initializes the extension. Initialization should register any extension bindings
        ///     with the <see cref="T:Microsoft.Azure.WebJobs.Host.IExtensionRegistry" /> instance, which can be obtained from the
        ///     <see cref="T:Microsoft.Azure.WebJobs.JobHostConfiguration" /> which is an <see cref="T:System.IServiceProvider" />.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.Azure.WebJobs.Host.Config.ExtensionConfigContext" /></param>
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (context.Trace == null)
                throw new ArgumentNullException("context.Trace");
            _tracer = context.Trace;

            context.Config.RegisterBindingExtensions(new KafkaMessageTriggerAttributeBindingProvider(this));
        }
    }
}