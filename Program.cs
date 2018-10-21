using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Foxpict.Service.Web {
  /// <summary>
  /// Main App entry point
  /// </summary>
  public class Program {
    /// <summary>
    /// Defines the entry point of the application.
    /// </summary>
    /// <param name="args">The arguments.</param>
    public static void Main (string[] args) {
      // NLog: setup the logger first to catch all errors
      var logger = NLog.Web.NLogBuilder.ConfigureNLog ("nlog.config").GetCurrentClassLogger ();
      try {
        BuildWebHost (args).Run ();
      } catch (Exception ex) {
        //NLog: catch setup errors
        logger.Error (ex, "Stopped program because of exception");
        throw;
      } finally {
        // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
        NLog.LogManager.Shutdown ();
      }
    }

    /// <summary>
    /// Builds the web host.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The web host, ready to be run.</returns>
    public static IWebHost BuildWebHost (string[] args) =>
      WebHost.CreateDefaultBuilder (args)
      .ConfigureAppConfiguration ((hostingContext, config) => {
        var env = hostingContext.HostingEnvironment;
        config.AddJsonFile ("appsettings.json", optional : false, reloadOnChange : true)
          .AddJsonFile ($"appsettings.{env.EnvironmentName}.json", optional : true, reloadOnChange : true);
        config.AddEnvironmentVariables ();
      })
      .ConfigureLogging ((hostingContext, logging) => {
        logging.ClearProviders ();
        logging.SetMinimumLevel (Microsoft.Extensions.Logging.LogLevel.Trace);
      })
      .UseUrls ("http://*:5001", "https://*:44321")
      .UseStartup<Startup> ()
      .Build ();
  }
}
