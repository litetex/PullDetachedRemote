using CommandLine;
using CoreFrameworkBase.Crash;
using CoreFrameworkBase.Logging.Initalizer;
using CoreFrameworkBase.Logging.Initalizer.Impl;
using PullDetachedRemote.CMD;
using System;
using System.Linq;

namespace PullDetachedRemote
{
   public static class Program
   {
      static void Main(string[] args)
      {
         Run(args);
      }

      public static void Run(string[] args)
      {
         CurrentLoggerInitializer.Set(new DefaultLoggerInitializer(new DefaultLoggerInitializerConfig()
         {
            WriteConsole = true,
            WriteFile = false,
            CreateLogFilePathOnStartup = false,
         }));
         InitLog();

         // TODO
         //#if !DEBUG
         try
         {
            new CrashDetector().Init();
            //#endif
            Parser.Default.ParseArguments<CmdOption>(args)
                     .WithParsed((opt) =>
                     {
                        // TODO
                        var starter = new StartUp(opt);
                        starter.Start();
                     })
                     .WithNotParsed((ex) =>
                     {
                        if (ex.All(err =>
                                new ErrorType[]
                                {
                                 ErrorType.HelpRequestedError,
                                 ErrorType.HelpVerbRequestedError
                                }.Contains(err.Tag))
                          )
                           return;

                        InitLog();
                        foreach (var error in ex)
                           Log.Error($"Failed to parse: {error.Tag}");
                     });
            //#if !DEBUG
         }
         catch (Exception ex)
         {
            InitLog(li => li.Config.WriteFile = true);
            Log.Fatal(ex);
         }
         //#endif
      }

      static void InitLog(Action<DefaultLoggerInitializer> initAction = null)
      {
         CurrentLoggerInitializer.InitLogging(il => initAction?.Invoke((DefaultLoggerInitializer)il));
      }
   }
}
