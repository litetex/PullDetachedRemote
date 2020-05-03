using System;
using System.Collections.Generic;
using System.Text;

namespace PullDetachedRemote.Workflow
{
   public class GithubWorkflow : IDisposable
   {
      protected Config.Configuration Config { get; set; }

      public GithubWorkflow(Config.Configuration config)
      {
         Config = config;

         Init();
      }

      protected void Init()
      {

      }

      public void Dispose()
      {
         
      }
   }
}
