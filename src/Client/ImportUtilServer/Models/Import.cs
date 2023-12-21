using System.Diagnostics;

namespace ImportUtilServer.Models
{
	public static class Import
	{
		private static List<string> OutputData { get; } = new();
		private static bool IsImportActive { get; set; } = false;
		public static async Task ExecuteAsync(string arguments)
		{
			if (IsImportActive)
				return;

			var exePath = @"C:\Users\danil\Desktop\Serega\Serega\bin\Debug\net6.0\Serega.exe";
			Console.WriteLine($"Import.ExecuteAsync: exePath - {exePath} arguments - {arguments}");

			IsImportActive = true;
			using Process process = new();
			{
				process.StartInfo.FileName = exePath;
				//process.StartInfo.WorkingDirectory = @"c:\temp\";
				process.StartInfo.Arguments = arguments;
				process.StartInfo.CreateNoWindow = false;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.OutputDataReceived += DataReceivedEventHandler; //обработчик события при получении очередной строки с данными
				process.ErrorDataReceived += ErrorReceivedEventHandler; //обработчик события при получении ошибки

				process.Start(); //запускаем процесс
				process.BeginOutputReadLine(); //начинаем считывать данные из потока 
				process.BeginErrorReadLine(); //начинаем считывать данные об ошибках 
				await process.WaitForExitAsync(); //ожидаем окончания работы приложения, чтобы очистить буфер
				process.Close(); //завершает процесс
			};
			IsImportActive = false;
		}
		public static IEnumerable<string> GetOutputData() => OutputData;
		public static void ClearOutputData() => OutputData.Clear();
		public static bool IsActive() => IsImportActive;

		private static void DataReceivedEventHandler(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				Console.WriteLine(e.Data);
				OutputData.Add(e.Data);
			}
		}
		private static void ErrorReceivedEventHandler(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				OutputData.Add(e.Data);
			}
		}
	}
}
