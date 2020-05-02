using CoreFrameworkBase.IO;
using CoreFrameworkBase.Tasks;
using PullDetachedRemote.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace PullDetachedRemote
{
   public class Runner
   {
      protected Configuration Config { get; set; }

      public Runner(Configuration configuration)
      {
         Config = configuration;

         Init();
      }

      #region Init

      private void Init()
      {
         // TODO
//#if !DEBUG
         if (string.IsNullOrWhiteSpace(Config.GitHubToken))
            throw new ArgumentException($"{nameof(Config.GitHubToken)}[='****'] is invalid");
//#endif
      }

      #endregion Init

      public void Run()
      {
         Log.Info("Starting run");
         //TODO
         //TaskRunner.RunTasks();
         Log.Info("All tasks successfully finished!");

         Log.Info("Finished run");
      }


   }
}
