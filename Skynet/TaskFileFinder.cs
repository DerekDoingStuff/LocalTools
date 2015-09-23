using System;
using System.IO;
using WindowFocus;

namespace ConsoleApplication2
{
	class TaskFileFinder : IFilePather
	{
		public string getPath()
		{
			var path = String.Format("Tasks/tasks.txt");

			(new FileInfo(path)).Directory.Create();

			return path;
		}
	}
}
