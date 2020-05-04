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

      protected Branch OriginUpdateBranch { get; set; }

      protected Branch UpstreamBranch { get; set; }


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
         var upstreamRemote = Repo.Network.Remotes.FirstOrDefault(x => x.PushUrl == Config.BaseUpstreamRepo);


         if (upstreamRemote != null)
         {
            Log.Info($"Found already existing upstream remote '{upstreamRemote.Name}'/'{upstreamRemote.PushUrl}'");
         }
         else
         {
            var upstreamRemoteName = GenerateRemoteUpstreamName(Repo.Network.Remotes.Select(x => x.Name));
            upstreamRemote = Repo.Network.Remotes.Add(upstreamRemoteName, Config.BaseUpstreamRepo);

            Log.Info($"Added upstream remote '{upstreamRemote.Name}'/'{upstreamRemote.PushUrl}'");
         }

         
         Log.Info($"Using upstream-remote '{upstreamRemote.Name}'<-'{Config.BaseUpstreamRepo}'");

         FetchOptions fetchOptions = new FetchOptions()
         {
            CredentialsProvider = UpstreamCredentialsHandler
         };

         // TODO: Don't fetch all!
         Commands.Fetch(
            Repo,
            upstreamRemote.Name,
            upstreamRemote
               .FetchRefSpecs
               .Select(x => x.Specification),
            fetchOptions,
            "");
         Log.Info($"Fetched upstream-remote successfully");

         UpstreamBranch = Repo.Branches.First(b => b.FriendlyName == $"{upstreamRemote.Name}/{Config.BaseUpstreamBranch}");
      }

      public string OriginRemote { get => Repo.Network.Remotes["origin"].PushUrl; }

      protected string GenerateRemoteUpstreamName(IEnumerable<string> exisitingRemoteNames, string preferedName = "upstream", int maxtries = 1000)
      {
         if (!exisitingRemoteNames.Contains(preferedName))
            return preferedName;

         for (int i = 1; i < maxtries; i++)
            if (!exisitingRemoteNames.Contains($"{preferedName}-{i}"))
               return $"{preferedName}-{i}";

         throw new InvalidOperationException($"Could not generate remote upstream name for {preferedName} within {maxtries} times");
      }

      public bool CheckoutOriginUpdateBranch()
      {

         OriginUpdateBranch = Repo.Branches.FirstOrDefault(x => x.FriendlyName == Config.NameOfOriginUpdateBranch);
         if (OriginUpdateBranch == null)
         {
            Log.Info($"Creating origin-update branch '{Config.NameOfOriginUpdateBranch}' from '{UpstreamBranch.FriendlyName}'");
            OriginUpdateBranch = Repo.CreateBranch(Config.NameOfOriginUpdateBranch, UpstreamBranch.Tip);
            Log.Info($"Created origin-update branch '{OriginUpdateBranch.FriendlyName}'[LatestCommit='{UpstreamBranch.Tip}']");

            return true;
         }

         Commands.Checkout(Repo, OriginUpdateBranch);
         Log.Info($"Checked out origin-update branch '{OriginUpdateBranch.FriendlyName}'");
         return false;
      }

      public bool HasUpstreamBranchNewCommitsForUpdateBranch()
      {
         Log.Info($"Checking if upstream-remote branch has newer commits than origin-update branch '{OriginUpdateBranch.FriendlyName}'");
         var upstreamCommitLog = Repo.Commits.QueryBy(new CommitFilter()
         {
            ExcludeReachableFrom = OriginUpdateBranch,
            IncludeReachableFrom = UpstreamBranch,
         });
         if (!upstreamCommitLog.Any())
         {
            Log.Info($"No new commits on upstream-remote branch '{UpstreamBranch.FriendlyName}'");
            //TODO
            return false;
         }
         Log.Info($"Detected {upstreamCommitLog.Count()} new commits on upstream-remote branch '{UpstreamBranch.FriendlyName}':");
         foreach (var commit in upstreamCommitLog)
            Log.Info($"{commit.Sha} | {commit.Message}");

         return true;
      }

      public bool HasUpdateBranchNewerCommitsThanUpstreamBranch()
      {
         Log.Info($"Checking if origin-update branch has newer commits than upstream-remote branch '{UpstreamBranch.FriendlyName}'");
         var updateOriginCommitLog = Repo.Commits.QueryBy(new CommitFilter()
         {
            ExcludeReachableFrom = UpstreamBranch,
            IncludeReachableFrom = OriginUpdateBranch,
         });
         if (!updateOriginCommitLog.Any())
         {
            Log.Info($"No new commits on origin-update branch '{UpstreamBranch.FriendlyName}'");
            //TODO
            return false;
         }
         Log.Info($"Detected {updateOriginCommitLog.Count()} new commits on origin-update branch '{UpstreamBranch.FriendlyName}':");
         foreach (var commit in updateOriginCommitLog)
            Log.Info($"{commit.Sha} | {commit.Message}");

         return true;
      }


      public bool UpdateBranchFromUpstream()
      {
         Log.Info($"Rebasing origin-update branch '{OriginUpdateBranch.FriendlyName}' from upstream-remote branch '{UpstreamBranch.FriendlyName}'");
         var rebaseResult = Repo.Rebase.Start(OriginUpdateBranch, UpstreamBranch, null, Identity, null);
         if (rebaseResult.Status != RebaseStatus.Complete)
         {
            Repo.Rebase.Abort();
            Log.Error($"Rebasing['{UpstreamBranch.FriendlyName}'->'{OriginUpdateBranch.FriendlyName}'] failed");
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
