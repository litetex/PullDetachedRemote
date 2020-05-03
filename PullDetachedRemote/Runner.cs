using CoreFrameworkBase.IO;
using CoreFrameworkBase.Tasks;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using PullDetachedRemote.Config;
using PullDetachedRemote.Git;
using PullDetachedRemote.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Text;

namespace PullDetachedRemote
{
   public class Runner
   {
      protected Config.Configuration Config { get; set; }

      protected GitWorkflow GitWorkflow { get; set; }

      protected GithubWorkflow GitHubWorkflow { get; set; }

      protected string UpstreamRemoteName { get; set; }

      public Runner(Config.Configuration configuration)
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

         if (string.IsNullOrWhiteSpace(Config.DetachedRepo) && Uri.TryCreate(Config.DetachedRepo, UriKind.Absolute, out _))
            throw new ArgumentException($"{nameof(Config.DetachedRepo)}[='{Config.DetachedRepo}'] is invalid");

         if (string.IsNullOrWhiteSpace(Config.DetachedBranch))
            throw new ArgumentException($"{nameof(Config.DetachedBranch)}[='{Config.DetachedBranch}'] is invalid");

         if (string.IsNullOrWhiteSpace(Config.NameOfOriginUpdateBranch))
            Config.NameOfOriginUpdateBranch = $"{Config.DetachedRepo}-{Config.DetachedBranch}";

         Config.NameOfOriginUpdateBranch = GitBranchNormalizer.Clean(Config.NameOfOriginUpdateBranch);
         if (string.IsNullOrWhiteSpace(Config.NameOfOriginUpdateBranch))
            throw new ArgumentException($"{nameof(Config.NameOfOriginUpdateBranch)}[='{Config.NameOfOriginUpdateBranch}'] is invalid");

         Config.PathToWorkingRepo = Repository.Discover(Config.PathToWorkingRepo);
         if (Config.PathToWorkingRepo == null)
            throw new ArgumentException("No local repository found");

         if (string.IsNullOrWhiteSpace(Config.IdentityEmail))
            Config.IdentityEmail = "actions@github.com";

         if (string.IsNullOrWhiteSpace(Config.IdentityEmail))
            Config.IdentityUsername = $"Github Action - {Assembly.GetEntryAssembly().GetName().Name} {Assembly.GetEntryAssembly().GetName().Version}";
      }

      #endregion Init

      public void Run()
      {
         Log.Info("Starting run");

         using (GitWorkflow = new GitWorkflow(Config))
         using (GitHubWorkflow = new GithubWorkflow(Config))
         {
            var originCredentialsHandler =
              new CredentialsHandler(
                  (url, usernameFromUrl, types) =>
                      new UsernamePasswordCredentials()
                      {
                         Username = Config.GitHubToken,
                         Password = ""
                      });

            CredentialsHandler upstreamCredentialsHandler = null;
            if(Config.DetachedCredsUseGitHub)
            {
               upstreamCredentialsHandler = originCredentialsHandler;
               Log.Info($"Will auth upstream-remote with GITHUB_TOKEN");
            }
            else if (string.IsNullOrWhiteSpace(Config.DetachedCredsPrinicipal))
            {
               upstreamCredentialsHandler = new CredentialsHandler(
                   (url, usernameFromUrl, types) =>
                       new UsernamePasswordCredentials()
                       {
                          Username = Config.DetachedCredsPrinicipal,
                          Password = Config.DetachedCredsPassword ?? ""
                       });
               Log.Info($"Will auth upstream-remote with custom credentials");
            }

            GitWorkflow.Init(originCredentialsHandler, upstreamCredentialsHandler);

            GitWorkflow.CheckoutOriginUpdateBranch();

            var needsNewCommits = GitWorkflow.CheckIfOriginUpdateBranchNeedsNewCommits();

            if(needsNewCommits)
            {
               var success = GitWorkflow.RebaseFromUpstream();

               if(!success)
               {
                  //Handle failure!
               }
            }

            GitWorkflow.PushOriginUpdateBranch();

            //DoIt();
         }

         //Log.Info("All tasks successfully finished!");

         Log.Info("Finished run");
      }

      protected void DoIt()
      {

         
      }

      
   }
}
