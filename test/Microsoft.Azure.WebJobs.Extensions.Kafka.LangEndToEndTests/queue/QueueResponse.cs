using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue
{
	public class QueueResponse
	{
		public List<string> responseList { get; set; }
		public QueueResponse()
		{
			responseList = new List<string>();
		}
		public int getLength()
		{
			return responseList.Count;
		}
	}
}
