using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace auth_tickets
{
  public class Program
  {
    public static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
      var host = Host.CreateDefaultBuilder(args);
      var port = Environment.GetEnvironmentVariable("PORT");

      if (port?.Length > 0) {
        var url = $"http://0.0.0.0:{port}";

        return host.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>().UseUrls(url));
      }

      return host.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
  }
}
