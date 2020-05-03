using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PullDetachedRemote.Workflow
{
   public class GitWorkflow : IDisposable
   {
      protected Config.Configuration Config { get; set; }

      protected Identity Identity { get; set; }

      protected Repository Repo { get; set; }

      protected CredentialsHandler OriginCredentialsHandler { get; set; }

      protected CredentialsHandler UpstreamCredentialsHandler { get; set; }

      protected Branch UpstreamBranch { get; set; }

      public bool? CreatedOriginUpdateBranch { get; set; }

      protected Branch OriginUpdateBranch { get; set; }

      public GitWorkflow(Config.Configuration config)
      {
         Config = config;
      }

      public void Init(CredentialsHandler originCredentialsHandler, CredentialsHandler upstreamCredentialsHandler)
      {
         OriginCredentialsHandler = originCredentialsHandler;
         UpstreamCredentialsHandler = upstreamCredentialsHandler;

         // Configure identity
         Identity = new Identity(Config.IdentityUsername, Config.IdentityEmail);
         Log.Info($"Using Identity: Username='{Identity.Name}', Email='{Identity.Email}'");

         // Init Repo
         Log.Info($"Will use repo at '{Config.PathToWorkingRepo}'");
         Repo = new Repository(Config.PathToWorkingRepo);

         // Fetch
         var remoteOrigin = Repo.Network.Remotes["origin"];
         Log.Info("Fetching origin");
         // TODO: Don't fetch all!
         Commands.Fetch(Repo, remoteOrigin.Name, remoteOrigin.FetchRefSpecs.Select(x => x.Specification), new FetchOptions() { CredentialsProvider = OriginCredentialsHandler }, "");
         Log.Info("Fetched origin successfully");

         InitUpstreamBranch();
      }

      protected void InitUpstreamBranch()
      {
         var upstreamRemoteName = GenerateRemoteUpstreamName(Repo.Network.Remotes.Select(x => x.Name));

         Repo.Network.Remotes.Add(upstreamRemoteName, Config.BaseUpstreamRepo);
         Log.Info($"Using upstream-remote '{upstreamRemoteName}'<-'{Config.BaseUpstreamRepo}'");

         FetchOptions fetchOptions = new FetchOptions()
         {
            CredentialsProvider = UpstreamCredentialsHandler
         };

         // TODO: Don't fetch all!
         Commands.Fetch(
            Repo,
            upstreamRemoteName,
            Repo.Network.Remotes[upstreamRemoteName]
               .FetchRefSpecs
               .Select(x => x.Specification),
            fetchOptions,
            "");
         Log.Info($"Fetched upstream-remote successfully");

         UpstreamBranch = Repo.Branches.First(b => b.FriendlyName == $"{upstreamRemoteName}/{Config.BaseUpstreamBranch}");
      }

      protected string GenerateRemoteUpstreamName(IEnumerable<string> exisitingRemoteNames, string preferedName = "upstream", int maxtries = 1000)
      {
         if (!exisitingRemoteNames.Contains(preferedName))
            return preferedName;

         for (int i = 1; i < maxtries; i++)
            if (!exisitingRemoteNames.Contains($"{preferedName}-{i}"))
               return $"{preferedName}-{i}";

         throw new InvalidOperationException($"Could not generate remote upstream name for {preferedName} within {maxtries} times");
      }

      public void CheckoutOriginUpdateBranch()
      {
         CreatedOriginUpdateBranch = false;

         OriginUpdateBranch = Repo.Branches.FirstOrDefault(x => x.FriendlyName == Config.NameOfOriginUpdateBranch);
         if (OriginUpdateBranch == null)
         {
            var originBaseBranch = Repo.Branches.FirstOrDefault(x => x.FriendlyName == $"origin/{Config.OriginBaseBranch}");
            Log.Info($"Creating origin-update branch '{Config.NameOfOriginUpdateBranch}'{(originBaseBranch != null ? $" from '{originBaseBranch.FriendlyName}'" : "")}");
            OriginUpdateBranch = originBaseBranch != null ? 
               Repo.CreateBranch(Config.NameOfOriginUpdateBranch, originBaseBranch.Commits.Last()) :
               Repo.CreateBranch(Config.NameOfOriginUpdateBranch);
            Log.Info($"Created origin-update branch '{OriginUpdateBranch.FriendlyName}'[LatestCommit='{OriginUpdateBranch.Commits.Last()}']");

            CreatedOriginUpdateBranch = true;
         }

         Commands.Checkout(Repo, OriginUpdateBranch);
         Log.Info($"Checked out origin-update branch '{OriginUpdateBranch.FriendlyName}'");
      }

      public bool CheckIfOriginUpdateBranchNeedsNewCommits()
      {
         Log.Info($"Checking if upstream-remote branch upstream-remote branch has newer commits than origin-update branch '{OriginUpdateBranch.FriendlyName}'");
         var upstreamOriginCommitLog = Repo.Commits.QueryBy(new CommitFilter()
         {
            ExcludeReachableFrom = OriginUpdateBranch,
            IncludeReachableFrom = UpstreamBranch,
         });
         if (!upstreamOriginCommitLog.Any())
         {
            Log.Info($"No new commits on upstream-remote branch '{UpstreamBranch.FriendlyName}'");
            //TODO
            return false;
         }
         Log.Info($"Detected {upstreamOriginCommitLog.Count()} new commits on upstream-remote branch {UpstreamBranch.FriendlyName}':");
         foreach (var commit in upstreamOriginCommitLog)
            Log.Info($"{commit.Sha} | {commit.Message}");

         return true;
      }

      public bool RebaseFromUpstream()
      {
         Log.Info($"Rebasing origin-update branch '{OriginUpdateBranch.FriendlyName}' from upstream-remote branch '{UpstreamBranch.FriendlyName}'");
         var rebaseResult = Repo.Rebase.Start(OriginUpdateBranch, UpstreamBranch, null, Identity, null);
         if (rebaseResult.Status != RebaseStatus.Complete)
         {
            Repo.Rebase.Abort();
            return false;
         }
         Log.Info($"Rebasing['{UpstreamBranch.FriendlyName}'->'{OriginUpdateBranch.FriendlyName}'] successful: Completed {rebaseResult.CompletedStepCount} steps");
         return true;
      }

      public void PushOriginUpdateBranch()
      {
         //TODO
         //Repo.Network.Push(OriginUpdateBranch, new PushOptions() { CredentialsProvider = OriginCredentialsHandler });
      }

      public void Dispose()
      {
         
      }
   }
}
