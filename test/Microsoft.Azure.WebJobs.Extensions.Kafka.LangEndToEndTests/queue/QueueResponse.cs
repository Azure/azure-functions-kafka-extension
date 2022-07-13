﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue
{
	/* Common class for responses received via different Queue Types(External Resources).
	*/
	public class QueueResponse
	{
		private List<string> responseList;
		public QueueResponse()
		{
			responseList = new List<string>();
		}
		public int getLength()
		{
			return responseList.Count;
		}
		public void AddString(string input)
		{ 
			responseList.Add(input);
		}

		public List<string> GetResponseList() { return responseList; }
	}
}