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

            WriteConfig();
            return;
         }

         Log.Info("MODE: Normal start");
         if (CmdOption.ConfigPath != null)
            ReadConfig();

         ReadCMDConfig();

         ReadEnvConfig();

         DoStart();
      }

      protected void FillSampleData()
      {
         // TODO?
      }

      protected void WriteConfig()
      {
         Log.Info("Writing json config");

         if (!string.IsNullOrWhiteSpace(CmdOption.ConfigGenerationPath))
            Config.Config.SavePath = CmdOption.ConfigGenerationPath;

         Log.Info($"Saving '{Config.Config.SavePath}'");
         Config.Save();

         Log.Info($"Saving: success");
      }

      protected void ReadConfig()
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

         if (!string.IsNullOrWhiteSpace(CmdOption.GITHUB_TOKEN))
         {
            Log.Info($"SetInp: {nameof(Config.GitHubToken)}='****'");
            Config.GitHubToken = CmdOption.GITHUB_TOKEN;
         }
         if (!string.IsNullOrWhiteSpace(CmdOption.DETACHED_CREDS_PRINCIPAL))
         {
            Log.Info($"SetInp: {nameof(Config.DetachedCredsPrinicipal)}='****'");
            Config.DetachedCredsPrinicipal = CmdOption.DETACHED_CREDS_PRINCIPAL;
         }
         if (!string.IsNullOrWhiteSpace(CmdOption.DETACHED_CREDS_PW))
         {
            Log.Info($"SetInp: {nameof(Config.DetachedCredsPassword)}='****'");
            Config.DetachedCredsPassword = CmdOption.DETACHED_CREDS_PW;
         }

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
