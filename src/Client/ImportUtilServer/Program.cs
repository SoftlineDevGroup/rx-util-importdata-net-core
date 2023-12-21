using ImportUtilServer.Models;
using System.Text.Json;

namespace ImportUtilServer
{
  public class Program
  {

		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			builder.Services.AddControllers();
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				//app.UseSwagger();
				//app.UseSwaggerUI();
			}

			//app.UseAuthorization();

			app.MapControllers();

			app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

			var api = "/api";
			app.MapGet($"{api}/import", async context =>
			{
				var arguments = context.Request.Query["arguments"].ToString();
				Task.Run(() => Import.ExecuteAsync(arguments));
			});
			app.MapGet($"{api}/getoutputdata", () =>
			{
				var result = JsonSerializer.Serialize(Import.GetOutputData());
				Import.ClearOutputData();
				return result;
			});
			app.MapGet($"{api}/isactive", () => Import.IsActive());

			app.Run();
		}
	}
}
