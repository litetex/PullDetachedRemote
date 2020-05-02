using CoreFrameworkBase.Config;
using PullDetachedRemote.CMD;
using PullDetachedRemote.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

namespace PullDetachedRemote
{
   public class StartUp
   {
      private CmdOption CmdOption { get; set; }

      private Configuration Config { get; set; } = new Configuration();

      public StartUp(CmdOption cmdOption)
      {
         CmdOption = cmdOption;
      }

      public void Start()
      {
         Contract.Requires(CmdOption != null);
         Log.Info($"Current directory is '{Directory.GetCurrentDirectory()}'");

         if (CmdOption.ConfigGenerationPath != null)
         {
            Log.Info("MODE: Write JSON Config");

            ReadCMDConfig();

            FillSampleData();

            WriteJsonConfig();
            return;
         }

         Log.Info("MODE: Normal start");
         if (CmdOption.ConfigPath != null)
            ReadJsonConfig();

         ReadCMDConfig();

         ReadEnvConfig();

         DoStart();
      }

      protected void FillSampleData()
      {
         // TODO?
      }

      protected void WriteJsonConfig()
      {
         Log.Info("Writing json config");

         if (!string.IsNullOrWhiteSpace(CmdOption.ConfigGenerationPath))
            Config.Config.SavePath = CmdOption.ConfigGenerationPath;

         Log.Info($"Saving '{Config.Config.SavePath}'");
         Config.Save();

         Log.Info($"Saving: success");
      }

      protected void ReadJsonConfig()
      {
         Log.Info("Reading json config");

         if (!string.IsNullOrWhiteSpace(CmdOption.ConfigPath))
            Config.Config.SavePath = CmdOption.ConfigPath;

         Log.Info($"Loading '{Config.Config.SavePath}'");
         Config.Load(LoadFileNotFoundAction.THROW_EX);

         Log.Info($"Loading: success");
      }

      protected void ReadCMDConfig()
      {
         Log.Info("Doing config over commandline-args");

      }

      protected void ReadEnvConfig()
      {
         Log.Info("Reading environment config");

         var envGithubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
         if (!string.IsNullOrWhiteSpace(envGithubToken))
         {
            Log.Info($"SetInp: {nameof(Config.GitHubToken)}='****'");
            Config.GitHubToken = envGithubToken;
         }
      }

      protected void DoStart()
      {
         Log.Info("Starting");
         new Runner(Config).Run();
         Log.Info("Done");
      }
   }
}
