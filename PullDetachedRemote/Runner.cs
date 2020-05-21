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
      protected bool DoClone { get; set; } = false;

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
         if (string.IsNullOrWhiteSpace(Config.GitHubToken))
            throw new ArgumentException($"{nameof(Config.GitHubToken)}[='****'] is invalid");

         if (string.IsNullOrWhiteSpace(Config.UpstreamRepo) && Uri.TryCreate(Config.UpstreamRepo, UriKind.Absolute, out _))
            throw new ArgumentException($"{nameof(Config.UpstreamRepo)}[='{Config.UpstreamRepo}'] is invalid");

         if (string.IsNullOrWhiteSpace(Config.PathToWorkingRepo))
            throw new ArgumentException($"{nameof(Config.PathToWorkingRepo)}[='{Config.PathToWorkingRepo}'] is invalid");

         if (string.IsNullOrWhiteSpace(Config.UpstreamBranch))
            Config.UpstreamBranch = null; // Process it later!

         if (string.IsNullOrWhiteSpace(Config.IdentityEmail))
            Config.IdentityEmail = "actions@github.com";

         if (string.IsNullOrWhiteSpace(Config.IdentityEmail))
            Config.IdentityUsername = $"Github Action - {Assembly.GetEntryAssembly().GetName().Name} {Assembly.GetEntryAssembly().GetName().Version}";


      }

      #endregion Init

      public void Run()
      {
         Log.Info("Starting run");

         var status = new StatusReport()
         {
            ResolvedConfig = Config
         };

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
            if(Config.UpstreamRepoUseGitHubCreds)
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

            InitRepo();

            GitWorkflow.Init(originCredentialsHandler, upstreamCredentialsHandler, DoClone);

            ConfigureNameOfOriginUpdateBranch(GitWorkflow);

            GitWorkflow.InitUpstreamBranch();

            GitHubWorkflow.Init(GitWorkflow.OriginRepoUrl);

            var createdBranch = GitWorkflow.CheckoutOriginUpdateBranch();
            status.CreatedBranch = createdBranch;

            var needsNewCommits = GitWorkflow.HasUpstreamBranchNewCommitsForUpdateBranch();

            status.HasUpstreamUpdates = needsNewCommits;
            if(needsNewCommits)
            { 
               var hasCommitsFromNonUpstream = GitWorkflow.HasUpdateBranchNewerCommitsThanUpstreamBranch();

               if(hasCommitsFromNonUpstream)
               {
                  Log.Info("There are commits from outside upstream on the origin-update branch AND new commits coming from upstram. This may cause conflicts...");
                  status.Messages.Add("There are commits from outside upstream on the origin-update branch AND new commits coming from upstram");
               }

               var success = GitWorkflow.UpdateBranchFromUpstream();

               if(!success)
               {
                  //Handle failure!
                  Log.Error("Unable to update branch from upstream");

                  status.Error = true;
                  status.Messages.Add("Unable to update branch from upstream; see logs for more details");
               }
            }

            GitWorkflow.DetachUpstreamRemote();

            if (!GitWorkflow.HasUpdateBranchNewerCommitsThanPRTargetedBranch(GitHubWorkflow.TargetPRBranchName))
            {
               Log.Info("Can't create a PR when the pr-base branch has no newer commits than the targeted-branch");
               status.Messages.Add("Can't create a PR when the pr-base branch has no newer commits than the targeted-branch");

               status.PRBaseNotBeforeTarget = true;
            }
            else
            {
               if (createdBranch || needsNewCommits)
               {
                  Log.Info("Needs to be pushed");
                  GitWorkflow.PushOriginUpdateBranch();

                  status.Pushed = true;
               }

               status.CreatedPR = GitHubWorkflow.EnsurePullRequestCreated(GitWorkflow.UpstreamRepoUrl, GitWorkflow.OriginUpdateBranchName);

               status.UpdatedPRSuccessfully = true;
               try
               {
                  GitHubWorkflow.SetPRStatus(status);
               }
               catch
               {
                  status.UpdatedPRSuccessfully = false;
                  throw;
               }
            }
         }

         Log.Info("Finished run");

         Log.Info($"=== STATUS REPORT ===\r\n{status}");
      }

      private void InitRepo()
      {
         var discoveredRepo = Repository.Discover(Config.PathToWorkingRepo);
         if (discoveredRepo == null && Config.CloneMode == CloneMode.DO_NOTHING)
            throw new ArgumentException("No local repository found");

         if (discoveredRepo == null || Config.CloneMode == CloneMode.CLONE_ALWAYS)
         {
            DoClone = true;

            if (string.IsNullOrWhiteSpace(Config.OriginRepo))
               throw new ArgumentException($"{nameof(Config.OriginRepo)}[='{Config.OriginRepo}'] is invalid");
         }
         else
            Config.PathToWorkingRepo = discoveredRepo;
      }

      private void ConfigureNameOfOriginUpdateBranch(GitWorkflow gitWorkflow)
      {
         if(Config.UpstreamBranch == null)
         {
            Log.Info("Auto detecting default upstream branch... May took some time");

            Config.UpstreamBranch = gitWorkflow.GetDefaultUpstreamBranch();

            Log.Info($"Got default upstream-branch[name='{Config.UpstreamBranch}'] of '{Config.UpstreamRepo}'");
         }

         if (string.IsNullOrWhiteSpace(Config.OriginUpdateBranch))
            Config.OriginUpdateBranch = $"upstreamupdate/{Config.UpstreamRepo}/{Config.UpstreamBranch}";

         Config.OriginUpdateBranch = GitBranchNormalizer.Fix(Config.OriginUpdateBranch);
         if (string.IsNullOrWhiteSpace(Config.OriginUpdateBranch))
            throw new ArgumentException($"{nameof(Config.OriginUpdateBranch)}[='{Config.OriginUpdateBranch}'] is invalid");
      }


   }
}
