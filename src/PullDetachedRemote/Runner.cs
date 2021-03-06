﻿using LibGit2Sharp;
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
         if (string.IsNullOrWhiteSpace(Config.GitHubPAT))
            throw new ArgumentException($"{nameof(Config.GitHubPAT)}[='****'] is invalid");

         if (string.IsNullOrWhiteSpace(Config.IdentityEmail))
            throw new ArgumentException($"{nameof(Config.IdentityEmail)}[='{Config.IdentityEmail}'] is invalid");

         if (string.IsNullOrWhiteSpace(Config.IdentityUsername))
            Config.IdentityUsername = $"{Assembly.GetEntryAssembly().GetName().Name} {Assembly.GetEntryAssembly().GetName().Version}";

         if (string.IsNullOrWhiteSpace(Config.UpstreamRepo) && Uri.TryCreate(Config.UpstreamRepo, UriKind.Absolute, out _))
            throw new ArgumentException($"{nameof(Config.UpstreamRepo)}[='{Config.UpstreamRepo}'] is invalid");

         if (string.IsNullOrWhiteSpace(Config.PathToWorkingRepo))
            throw new ArgumentException($"{nameof(Config.PathToWorkingRepo)}[='{Config.PathToWorkingRepo}'] is invalid");

         if (string.IsNullOrWhiteSpace(Config.UpstreamBranch))
            Config.UpstreamBranch = null; // Process it later!

         if (Config.PRMetaInfo == null)
         {
            Log.Warn($"{nameof(Config.PRMetaInfo)} was not set! Setting it to default value. PLEASE FIX the config");
            Config.PRMetaInfo = new PullRequestMetaInfoConfig();
         }
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
                         Username = Config.GitHubPAT,
                         Password = ""
                      });

            var upstreamCredentialsHandler = GetUpstreamCredentialsHandler(originCredentialsHandler);

            InitRepo();

            GitWorkflow.Init(originCredentialsHandler, upstreamCredentialsHandler, DoClone);

            ConfigureNameOfOriginUpdateBranch(GitWorkflow);

            GitWorkflow.InitUpstreamBranch();

            GitHubWorkflow.Init(GitWorkflow.OriginRepoUrl);

            var createdBranch = GitWorkflow.CheckoutOriginUpdateBranch();
            status.CreatedNewBranch = createdBranch;

            var needsNewCommits = GitWorkflow.HasUpstreamBranchNewCommitsForUpdateBranch();

            status.HasUpstreamUpdates = needsNewCommits;
            if(needsNewCommits)
            { 
               var hasCommitsFromNonUpstream = GitWorkflow.HasUpdateBranchNewerCommitsThanUpstreamBranch();

               if(hasCommitsFromNonUpstream)
               {
                  var msg = "There are commits from outside upstream on the origin-update branch AND new commits coming from upstram";

                  Log.Info($"{msg}. This may cause conflicts.");
                  status.Messages.Add(msg);
               }

               var success = GitWorkflow.UpdateBranchFromUpstream();

               // Handle failure
               if (!success)
               {
                  Log.Error("Unable to update branch from upstream");

                  status.Error = true;
                  status.Messages.Add("Unable to update branch from upstream; see logs for more details");
               }
            }

            GitWorkflow.DetachUpstreamRemote();

            if (!GitWorkflow.HasUpdateBranchNewerCommitsThanPRTargetedBranch(GitHubWorkflow.TargetPRBranchName))
            {
               var msg = "Can't create a PR when the pr-base branch has no newer commits than the targeted-branch";
               Log.Info(msg);
               status.Messages.Add(msg);

               status.NoNewCommitsOnBaseBranch = true;
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
              
               if (status.CreatedPR)
                  GitHubWorkflow.SetMetaToNewPR(status);

               GitHubWorkflow.SetPRStatus(status);

               status.UpdatedPRSuccessfully = true;
            }
         }

         Log.Info("Finished run");

         Log.Info($"=== STATUS REPORT ===\r\n{status}");
      }

      private CredentialsHandler GetUpstreamCredentialsHandler(CredentialsHandler originCredentialsHandler)
      {
         switch (Config.UpstreamCredMode)
         {
            case UpstreamRepoCredentialsMode.AUTO:
               Log.Info($"Automatically determining auth of upstream-remote");

#pragma warning disable S907 // "goto" statement should not be used
               if (Config.UpstreamRepo.StartsWith("https://github.com/"))
                  goto case UpstreamRepoCredentialsMode.GITHUB;
               else if (!string.IsNullOrWhiteSpace(Config.DetachedCredsPrinicipal))
                  goto case UpstreamRepoCredentialsMode.CUSTOM;
               else
                  goto default;
#pragma warning restore S907 // "goto" statement should not be used

            case UpstreamRepoCredentialsMode.GITHUB:
               Log.Info($"Will auth upstream-remote with GitHub credentials");

               return originCredentialsHandler;

            case UpstreamRepoCredentialsMode.CUSTOM:
               Log.Info($"Will auth upstream-remote with custom credentials");

               return new CredentialsHandler(
                (url, usernameFromUrl, types) =>
                    new UsernamePasswordCredentials()
                    {
                       Username = Config.DetachedCredsPrinicipal,
                       Password = Config.DetachedCredsPassword ?? ""
                    });

            default:
               Log.Info($"Will auth upstream-remote with NO credentials");

               return null;
         }
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
            Log.Warn($"{nameof(Config.UpstreamBranch)} is not set! Autodetecting default upstream branch... May take some time and memory");

            Config.UpstreamBranch = gitWorkflow.GetDefaultUpstreamBranch();

            Log.Info($"Got default upstream-branch[name='{Config.UpstreamBranch}'] of '{Config.UpstreamRepo}'");
         }

         if (string.IsNullOrWhiteSpace(Config.UpdateBranch))
         {
            var repoName = Config.UpstreamRepo;
            if (repoName.StartsWith("https:") || repoName.StartsWith("http:"))
            {
               var newRepoName = repoName[(repoName.IndexOf(':') + 1)..];
               if(!string.IsNullOrWhiteSpace(newRepoName)) 
                  repoName = newRepoName;
            }

            Config.UpdateBranch = $"upstreamupdate/{repoName}/{Config.UpstreamBranch}";
         }

         Config.UpdateBranch = GitBranchNormalizer.Fix(Config.UpdateBranch);
         if (string.IsNullOrWhiteSpace(Config.UpdateBranch))
            throw new ArgumentException($"{nameof(Config.UpdateBranch)}[='{Config.UpdateBranch}'] is invalid");

         Log.Info($"{nameof(Config.UpdateBranch)} is '{Config.UpdateBranch}'");
      }


   }
}
