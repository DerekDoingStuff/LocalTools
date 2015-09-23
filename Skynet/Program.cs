using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceHealthCheck;

namespace ConsoleApplication2
{
	class Program
	{
		static void Main(string[] args)
		{
			int[] top = { 0, 0 };//{-2501, 64};
			var output = "Hey Derek...~";

			var consoleWidth = findLengthOfLongestLine(output);

			Console.Title = "Skynet";
			setWindowSize(consoleWidth, 1);
			
			Program.MoveWindow(Process.GetCurrentProcess().MainWindowHandle, top[0], top[1], 500, 500, true);
			//Program.MoveWindow(Process.GetCurrentProcess().MainWindowHandle, -600, (-475 + 910) / 2, 500, 500, true);

			//set up and start tasks
			var getStatsTask = Task.Factory.StartNew<string>(() => { return getStats(); });
			var getTasksTask = Task.Factory.StartNew<string>(() => { return getTasks(); });
			var getServiceStatusTask = Task.Factory.StartNew<string>(() => { return getServiceStatuses(); });

			//hello message
			WriteStringToConsole(output, 1.0);

			//end points up and running
			if (getServiceStatusTask.Result != "")
			{
				consoleWidth = findLengthOfLongestLine(getServiceStatusTask.Result);
				setWindowSize(consoleWidth, getServiceStatusTask.Result.Split('|').Length);
				WriteStringToConsole(getServiceStatusTask.Result, 2.0);
			}

			//task tracker
			if (getTasksTask.Result != "")
			{
				consoleWidth = findLengthOfLongestLine(getTasksTask.Result);
				setWindowSize(consoleWidth, getTasksTask.Result.Split('|').Length);
				WriteStringToConsole(getTasksTask.Result, 4.0);
			}

			//previous day's window focus
			if (getStatsTask.Result != "")
			{
				consoleWidth = findLengthOfLongestLine(getStatsTask.Result);
				setWindowSize(consoleWidth, 1);
				WriteStringToConsole(getStatsTask.Result, 2.0);
			}
		}

		public static void setWindowSize(int width, int height)
		{
			var minWidth = 32;
			if (width < minWidth)
				width = minWidth;
			Console.SetWindowSize(width, height);
			Console.SetBufferSize(width, height);
		}

		public static string getStats()
		{
			try
			{
				WindowFocus.Record[] recs;
				TimeSpan span;
				var stats = new WindowFocus.Stats(new WindowFocus.DatedPathBuilder(DateTime.Now.AddDays(-1.0)));
				var result = stats.tryLoad(out recs, out span);
				if (result)
				{
					var sb = new StringBuilder();

					sb.Append(String.Format("Yesterday...~The computer was on for {0:0.00} hours...~", span.TotalHours));

					for (int i = 0; i < recs.Length; i++)
					{
						sb.Append(String.Format("You spent {0:0.00} minutes on {1}...~",
							recs[i].runningTime.TotalMinutes,
							recs[i].process));
					}

					return sb.ToString();
				}
				else
					return "";
			}
			catch (Exception e)
			{
				return e.Message;
			}
		}

		public static string getServiceStatuses()
		{
			var mc = new MeetballChecker(getServiceVersions());

			return "Service Health:||" + mc.checkAll() + "````~";
		}

		private static string[] getServiceVersions()
		{
			string[] versions = {"6.0", "5.4.2", "5.4.1", "5.4", "5.3", "+meetball.com"};
			return versions;
		}

		public static string getTasks()
		{
			TaskRecord[] recs;

			var success = new TaskManager(new TaskFileFinder()).tryLoad(out recs);
			if (success)
			{
				var sb = new StringBuilder();
				for (int i = 0; i < recs.Length; i++)
				{
					sb.Append(String.Format("Task {0}: {1}|{2}||", i+1, recs[i].name, recs[i].notes));
				}
				sb.Append("````````~");

				return sb.ToString();
			}
			else
				return "";
		}

		public static int findLengthOfLongestLine(string output)
		{
		 	var split = output.Split('~', '|');
			int x = -1;
			for (int i = 0; i < split.Length; i++)
				if (split[i].Length > x)
					x = split[i].Length;

			return x+1;
		}

		public static void WriteStringToConsole(string output, double SpeedMultiplier)
		{
			var r = new Random();

			for (int i = 0; i < output.Length; i++)
			{
				if (output[i] == '`')
				{
					Thread.Sleep((int)(4000 / SpeedMultiplier));
				}
				else if (output[i] == '~')
				{
					Thread.Sleep((int)(2000 / SpeedMultiplier));
					Console.Clear();
				}
				else if (output[i] == '|')
				{
					Console.WriteLine();
					Thread.Sleep(r.Next((int)(100 / SpeedMultiplier), (int)(200 / SpeedMultiplier)));
				}
				else
				{
					Console.Write(output[i]);
					Thread.Sleep(r.Next((int)(100 / SpeedMultiplier), (int)(200 / SpeedMultiplier)));
				}
			}
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

	}
}
