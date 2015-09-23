using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowFocus;

namespace ConsoleApplication2
{
	class TaskManager
	{
		private IFilePather _pather;

		public TaskManager(IFilePather pather)
		{
			_pather = pather;
		}

		public bool tryLoad(out TaskRecord[] recs)
		{
			var list = new List<TaskRecord>();
			bool success;
			try
			{
				var lines = File.ReadAllLines(_pather.getPath());

				for (int i = 0; i < lines.Length; i++)
				{
					var split = lines[i].Split(';');

					var name = split[0];
					var notes = split[1];
					var createDateTicks = Int64.Parse(split[2]);
					var modifyDateTicks = Int64.Parse(split[3]);

					list.Add(new TaskRecord
					{
						createDate = new DateTime(createDateTicks),
						modifyDate = new DateTime(modifyDateTicks),
						name = name,
						notes = notes
					});
				}

				success = true;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				success = false;
			}
			
			recs = list.ToArray();
			return success;
		}

	}
}
