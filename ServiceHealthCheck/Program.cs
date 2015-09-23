using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;

namespace ServiceHealthCheck
{
	class Program
	{
		static void Main(string[] args)
		{
			string[] versions = {"6.0", "5.4.2", "5.4.1", "5.4", "5.3"};

			var mb = new MeetballChecker(versions);

			var output = mb.checkAll();

			Console.Write(output.Replace("|", Environment.NewLine));

			Console.ReadKey();
		}
	}
}
