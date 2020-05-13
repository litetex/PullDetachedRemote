﻿using CoreFrameworkBase.IO;
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

         if (string.IsNullOrWhiteSpace(Config.BaseUpstreamRepo) && Uri.TryCreate(Config.BaseUpstreamRepo, UriKind.Absolute, out _))
            throw new ArgumentException($"{nameof(Config.BaseUpstreamRepo)}[='{Config.BaseUpstreamRepo}'] is invalid");

         if (string.IsNullOrWhiteSpace(Config.BaseUpstreamBranch))
            throw new ArgumentException($"{nameof(Config.BaseUpstreamBranch)}[='{Config.BaseUpstreamBranch}'] is invalid");

         if (string.IsNullOrWhiteSpace(Config.NameOfOriginUpdateBranch))
            Config.NameOfOriginUpdateBranch = $"upstreamupdate/{Config.BaseUpstreamRepo}-{Config.BaseUpstreamBranch}";

         Config.NameOfOriginUpdateBranch = GitBranchNormalizer.Fix(Config.NameOfOriginUpdateBranch);
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

         var status = new Status()
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

            GitWorkflow.Init(originCredentialsHandler, upstreamCredentialsHandler);

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

            if(createdBranch || needsNewCommits)
            {
               Log.Info("Needs to be pushed");
               GitWorkflow.PushOriginUpdateBranch();

               status.Pushed = true;
            }

            GitHubWorkflow.EnsurePullRequestCreated(GitWorkflow.UpstreamRepoUrl, GitWorkflow.OriginUpdateBranchName);

            GitHubWorkflow.SetPRStatus(status);
         }

         Log.Info("Finished run");
      }
      
   }
}
