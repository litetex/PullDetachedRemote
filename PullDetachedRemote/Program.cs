using CommandLine;
using CoreFramework.CrashLogging;
using CoreFramework.Logging.Initalizer;
using CoreFramework.Logging.Initalizer.Impl;
using PullDetachedRemote.CMD;
using System;
using System.Linq;
using System.Text;

namespace PullDetachedRemote
{
   public static class Program
   {
      public const string EXPECT_ESCAPED_INPUT = "expectescapedinput";

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

#if !DEBUG
         try
         {
            new CrashDetector()
            {
               SupplyLoggerInitalizer = () => CurrentLoggerInitializer.Current
            }.Init();
#endif
         Parser.Default.ParseArguments<CmdOption>(args)
                     .WithParsed((opt) =>
                     {
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

                        Log.Fatal("Failure processing args");
                     });
#if !DEBUG
         }
         catch (Exception ex)
         {
            InitLog();
            Log.Fatal(ex);
         }
#endif
      }

      static void InitLog(Action<DefaultLoggerInitializer> initAction = null)
      {
         CurrentLoggerInitializer.InitLogging(il => initAction?.Invoke((DefaultLoggerInitializer)il));
      }
   }
}
