using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue
{
	public class QueueRequest : IEnumerable<string>
	{
		public List<string> requestList { get; set; }
		public QueueRequest() { 
			requestList = new List<string>();
		}
		public int getLength() { 
			return requestList.Count;
		}

		public IEnumerator<string> GetEnumerator()
		{
			return requestList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
