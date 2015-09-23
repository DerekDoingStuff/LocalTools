using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace WindowFocus
{
	class Program
	{
		static void Main(string[] args)
		{
			IFilePather pather = new DatedPathBuilder(DateTime.Now.AddYears(1));
			_s = new Stats(pather);

			Automation.AddAutomationFocusChangedEventHandler(OnFocusChangedHandler);
			AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

			Console.Clear();
			Console.WriteLine("Monitoring... Hit enter to end.");

			while (true)
			{
				try
				{
					Reader.ReadLine(10000);
					break;
				}
				catch (Exception)
				{
					Console.WriteLine("Writing to file...");
					_s.writeToFile();
					Console.WriteLine("Finished...");
					_s = new Stats(pather);
				}
			}
		}

		private static Stats _s;

		private static void OnProcessExit(object sender, EventArgs e)
		{
			Console.WriteLine("Writing to file...");
			_s.writeToFile();
		}

		private static void OnFocusChangedHandler(object src, AutomationFocusChangedEventArgs args)
		{
			AutomationElement element = src as AutomationElement;
			if (element != null)
			{
				string id = element.Current.AutomationId;
				int processId = element.Current.ProcessId;
				
				using (Process process = Process.GetProcessById(processId))
				{
					_s.record(process.ProcessName);
				}
			}
		}
	}

	public class Stats
	{
		private DateTime beginning;
		private IFilePather pather;
		private DateTime lastStart;
		private string lastProcess;
		private bool isRecording;
		private List<Record> records;

		public Stats(IFilePather pather)
		{
			this.pather = pather;
			isRecording = false;
			records = new List<Record>();
			beginning = DateTime.Now;
		}

		public void record(string process)
		{
			if (process != lastProcess)
			{
				Console.Clear();
				endRecording();
				isRecording = true;
				lastStart = DateTime.Now;
				lastProcess = process;
				Console.WriteLine("Focused on Process: {0}", process);
				Console.WriteLine("Monitoring... Hit enter to end.");
			}
		}

		public void endRecording()
		{
			if (isRecording)
			{
				string process = lastProcess;
				var timesFocused = 0;
				TimeSpan runningTime;

				isRecording = false;
				var span = DateTime.Now - lastStart;
				var index = records.FindIndex(x => x.process == lastProcess);
				if (index >= 0)
				{
					var rec = records[index];
					rec.runningTime += span;
					rec.timesFocused++;
					records[index] = rec;
					if (records[index].timesFocused != rec.timesFocused)
						throw new Exception("value wasn't updated");

					runningTime = rec.runningTime;
					timesFocused = rec.timesFocused;
				}
				else
				{
					runningTime = span;
					timesFocused = 1;

					var rec = new Record
					{
						process = lastProcess,
						runningTime = span,
						timesFocused = 1
					};

					records.Add(rec);
				}
				Console.WriteLine("UnFocused Process: {0}\n\tRunning Time: {1:N2}min\n\tTimes Focused: {2}", process, runningTime.TotalMinutes, timesFocused);
			}
		}

		public void writeToFile()
		{
			Record[] loadedRecs;
			TimeSpan previousSpan;

			var success = tryLoad(out loadedRecs, out previousSpan);

			if(success)
				for(int i = 0; i < loadedRecs.Length; i++)
					addRecord(loadedRecs[i]);

			var span = DateTime.Now - beginning + previousSpan;
			var ms = span.TotalMilliseconds;

			var sb = new StringBuilder();

			records = records.OrderBy(x => -x.runningTime).ToList();

			sb.Append(span.Ticks);
			sb.Append(Environment.NewLine);

			for (int i = 0; i < records.Count; i++)
			{
				var thing = records[i].runningTime.TotalMilliseconds/ms*100;
				sb.Append(String.Format("{0:N2}%,{1},{2},{3}", 
					thing,
					records[i].process, 
					records[i].runningTime.Ticks, 
					records[i].timesFocused));
				sb.Append(Environment.NewLine);
			}
			
			File.WriteAllText(pather.getPath(), sb.ToString());
		}

		public bool tryLoad(out Record[] recs, out TimeSpan previousSpan)
		{
			var list = new List<Record>();
			previousSpan = TimeSpan.Zero;
			try
			{
				var lines = File.ReadAllLines(pather.getPath());

				if (lines.Length > 0)
					previousSpan = TimeSpan.FromTicks(Int64.Parse(lines[0]));

				for (int i = 1; i < lines.Length; i++)
				{
					var split = lines[i].Split(',');
					
					if (split.Length == 4)
					{
						list.Add(new Record
						{
							process = split[1],
							runningTime = TimeSpan.FromTicks(Int64.Parse(split[2])),
							timesFocused = Int32.Parse(split[3])
						});
					}

				}
				recs = list.ToArray();
				return true;
			}
			catch (FileNotFoundException e)
			{
				recs = list.ToArray();
				return true;
			}
		}

		public void addRecord(Record rec)
		{
			var index = records.FindIndex(x => x.process == rec.process);
			if (index >= 0)
			{
				var temp = records[index];
				temp.runningTime += rec.runningTime;
				temp.timesFocused += rec.timesFocused;
				records[index] = temp;
				if (records[index].timesFocused != temp.timesFocused)
					throw new Exception("value wasn't updated");
			}
			else
			{
				records.Add(rec);
			}
		}
	}

	public class DatedPathBuilder : IFilePather
	{
		private DateTime processStartDate;

		public DatedPathBuilder(DateTime processStartDate)
		{
			this.processStartDate = processStartDate;
		}

		public string getPath()
		{
			var shortDate = processStartDate.ToShortDateString().Replace('/', '_');

			var path = String.Format("FocusLog/Log{0}.txt",
				shortDate);

			(new FileInfo(path)).Directory.Create();

			return path;
		}
	}

	public interface IFilePather
	{
		string getPath();
	}

	public struct Record
	{
		public TimeSpan runningTime;
		public string process;
		public int timesFocused;
	}

	class Reader
	{
		private static Thread inputThread;
		private static AutoResetEvent getInput, gotInput;
		private static string input;

		static Reader()
		{
			getInput = new AutoResetEvent(false);
			gotInput = new AutoResetEvent(false);
			inputThread = new Thread(reader);
			inputThread.IsBackground = true;
			inputThread.Start();
		}

		private static void reader()
		{
			while (true)
			{
				getInput.WaitOne();
				input = Console.ReadLine();
				gotInput.Set();
			}
		}

		public static string ReadLine(int timeOutMillisecs)
		{
			getInput.Set();
			bool success = gotInput.WaitOne(timeOutMillisecs);
			if (success)
				return input;
			else
				throw new TimeoutException("User did not provide input within the timelimit.");
		}
	}
}
