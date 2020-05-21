using CoreFramework.Config;
using PullDetachedRemote.CMD;
using PullDetachedRemote.Config;
using PullDetachedRemote.Util;
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

         var cps = new PropertySetter()
         {
            SetLog = "SetInp",
            SetFaultyLog = "SetInpFaulty"
         };

         cps.SetStringSecret(() => CmdOption.GITHUB_TOKEN, v => Config.GitHubToken = v, nameof(Config.GitHubToken));
         cps.SetStringSecret(() => CmdOption.DETACHED_CREDS_PRINCIPAL, v => Config.DetachedCredsPrinicipal = v, nameof(Config.DetachedCredsPrinicipal));
         cps.SetStringSecret(() => CmdOption.DETACHED_CREDS_PW, v => Config.DetachedCredsPassword = v, nameof(Config.DetachedCredsPassword));

         cps.SetString(() => CmdOption.IdentityEmail, v => Config.IdentityEmail = v, nameof(Config.IdentityEmail));
         cps.SetString(() => CmdOption.IdentityUsername, v => Config.IdentityUsername = v, nameof(Config.IdentityUsername));
         cps.SetString(() => CmdOption.PathToWorkingRepo, v => Config.PathToWorkingRepo = v, nameof(Config.PathToWorkingRepo));
         cps.SetEnum<CloneMode>(() => CmdOption.CloneMode, v => Config.CloneMode = v, nameof(Config.CloneMode));
         cps.SetString(() => CmdOption.OriginRepo, v => Config.OriginRepo = v, nameof(Config.OriginRepo));
         cps.SetString(() => CmdOption.OriginBranch, v => Config.OriginBranch = v, nameof(Config.OriginBranch));
         cps.SetString(() => CmdOption.UpstreamRepo, v => Config.UpstreamRepo = v, nameof(Config.UpstreamRepo));
         cps.SetString(() => CmdOption.UpstreamBranch, v => Config.UpstreamBranch = v, nameof(Config.UpstreamBranch));
         cps.SetString(() => CmdOption.OriginUpdateBranch, v => Config.OriginUpdateBranch = v, nameof(Config.OriginUpdateBranch));
         cps.SetBool(() => CmdOption.UpstreamRepoUseGitHubCreds, v => Config.UpstreamRepoUseGitHubCreds = v, nameof(Config.UpstreamRepoUseGitHubCreds));
      }


      protected void ReadEnvConfig()
      {
         Log.Info("Reading environment config");

         var cps = new PropertySetter()
         {
            SetLog = "SetEnv",
            SetFaultyLog = "SetEnvFaulty"
         };

         cps.SetStringSecret(() => Environment.GetEnvironmentVariable("GITHUB_TOKEN"), v => Config.GitHubToken = v, nameof(Config.GitHubToken));
         cps.SetStringSecret(() => Environment.GetEnvironmentVariable("DETACHED_CREDS_PRINCIPAL"), v => Config.DetachedCredsPrinicipal = v, nameof(Config.DetachedCredsPrinicipal));
         cps.SetStringSecret(() => Environment.GetEnvironmentVariable("DETACHED_CREDS_PW"), v => Config.DetachedCredsPassword = v, nameof(Config.DetachedCredsPassword));
      }

      protected void DoStart()
      {
         Log.Info("Starting");
         new Runner(Config).Run();
         Log.Info("Done");
      }
   }
}
