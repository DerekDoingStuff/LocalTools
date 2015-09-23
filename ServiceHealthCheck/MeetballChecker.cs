using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServiceHealthCheck
{
	public class MeetballChecker
	{
		private string _url;
		private string[] _versions;

		public MeetballChecker(string[] versions)
		{
			_url = "Https://ws.meetball.com/{0}/Service.svc/json/test/test";
			_versions = versions;
		}

		public string checkAll()
		{
			var sb = new StringBuilder();

			//check if is up and running by calling /test/test
			for (int i = 0; i < _versions.Length; i++)
			{
				var isPing = _versions[i][0] == '+';
				if(isPing)
					_versions[i] = _versions[i].Substring(1);
				var result =  isPing ?
					ping(_versions[i]) :
					running(string.Format(_url, _versions[i]));
				if (result)
					sb.Append(String.Format("{0}: UP AND RUNNING|", _versions[i]));
				else
					sb.Append(String.Format("{0}: DOWN|", _versions[i]));
			}

			return sb.ToString();
		}

		private bool running(string url)
		{
			try
			{
				var json = "";
				var result = jsonHttpCall(url, json, "GET");
				return result != "";
			}
			catch (Exception)
			{
				return false;
			}
		}

		private bool ping(string url)
		{
			var ping = new System.Net.NetworkInformation.Ping();

			var result = ping.Send(url);

			if (result.Status != System.Net.NetworkInformation.IPStatus.Success)
				return true;
			return false;
		}

		private string jsonHttpCall(string url, string json, string methodType)
		{
			var req = (HttpWebRequest)WebRequest.Create(url);
			req.ContentType = "application/json";
			req.Method = methodType;


			if (methodType.Equals("POST"))
			{
				using (var sw = new StreamWriter(req.GetRequestStream()))
				{
					sw.Write(json);
					sw.Flush();
					sw.Close();

					var res = (HttpWebResponse)req.GetResponse();
					using (var sr = new StreamReader(res.GetResponseStream()))
					{
						return sr.ReadToEnd();
					}
				}
			}
			else if (methodType.Equals("GET"))
			{
				var sb = new StringBuilder();
				req.Method = "GET";
				using (WebResponse response = req.GetResponse())
				{
					using (Stream stream = response.GetResponseStream())
					{
						Encoding encode = Encoding.GetEncoding("utf-8");

						// Pipe the stream to a higher level stream reader with the required encoding format. 
						StreamReader readStream = new StreamReader(stream, encode);
						Char[] read = new Char[256];

						// Read 256 charcters at a time.     
						int count = readStream.Read(read, 0, 256);

						while (count > 0)
						{
							// Dump the 256 characters on a string and display the string onto the console.
							String str = new String(read, 0, count);
							sb.Append(str);
							count = readStream.Read(read, 0, 256);

						}
					}
				}
				return sb.ToString();
			}
			else
				throw new ArgumentException("methodType needs to be POST or GET");

			return "";
		}
	}
}
