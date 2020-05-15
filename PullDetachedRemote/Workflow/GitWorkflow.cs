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

      protected Remote OriginRemote { get => Repo.Network.Remotes["origin"]; }

      protected Remote UpstreamRemote { get; set; }

      protected Branch OriginUpdateBranch { get; set; }

      protected Branch UpstreamBranch { get; set; }

      public string OriginRepoUrl { get => OriginRemote.PushUrl; }

      public string UpstreamRepoUrl { get => UpstreamRemote.PushUrl; }

      public string OriginUpdateBranchName { get => OriginUpdateBranch?.FriendlyName; }


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
         Commands.Fetch(
           Repo,
           remoteOrigin.Name,
           remoteOrigin
              .FetchRefSpecs
              .Select(x => x.Specification),
           new FetchOptions() { CredentialsProvider = OriginCredentialsHandler },
           "");
         Log.Info("Fetched origin successfully");

         InitUpstreamBranch();
      }

      protected void InitUpstreamBranch()
      {
         UpstreamRemote = Repo.Network.Remotes.FirstOrDefault(x => x.PushUrl == Config.BaseUpstreamRepo);


         if (UpstreamRemote != null)
         {
            Log.Info($"Found already existing upstream remote '{UpstreamRemote.Name}'/'{UpstreamRemote.PushUrl}'");
         }
         else
         {
            var upstreamRemoteName = GenerateRemoteUpstreamName(Repo.Network.Remotes.Select(x => x.Name));
            UpstreamRemote = Repo.Network.Remotes.Add(upstreamRemoteName, Config.BaseUpstreamRepo);

            Log.Info($"Added upstream remote '{UpstreamRemote.Name}'/'{UpstreamRemote.PushUrl}'");
         }

         
         Log.Info($"Using upstream-remote '{UpstreamRemote.Name}'<-'{Config.BaseUpstreamRepo}'");

         FetchOptions fetchOptions = new FetchOptions()
         {
            CredentialsProvider = UpstreamCredentialsHandler
         };

         // TODO: Don't fetch all!
         Commands.Fetch(
            Repo,
            UpstreamRemote.Name,
            UpstreamRemote
               .FetchRefSpecs
               .Select(x => x.Specification),
            fetchOptions,
            "");
         Log.Info($"Fetched upstream-remote successfully");

         UpstreamBranch = Repo.Branches.First(b => b.FriendlyName == $"{UpstreamRemote.Name}/{Config.BaseUpstreamBranch}");
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
            Log.Error($"Rebasing['{UpstreamBranch.FriendlyName}'->'{OriginUpdateBranch.FriendlyName}'] failed: {rebaseResult.Status}");
            return false;
         }
         Log.Info($"Rebasing['{UpstreamBranch.FriendlyName}'->'{OriginUpdateBranch.FriendlyName}'] successful: Completed {rebaseResult.CompletedStepCount} steps");

         return true;
      }

      public void DetachUpstreamRemote()
      {
         var toDetachremote = Repo.Network.Remotes.FirstOrDefault(r => r.Name == UpstreamRemote.Name);
         if(toDetachremote == null)
         {
            Log.Debug($"{nameof(UpstreamRemote)} is already removed");
            return;
         }

         Repo.Network.Remotes.Remove(toDetachremote.Name);
         Log.Info($"Removing {nameof(UpstreamRemote)} '{toDetachremote.Name}'");
      }

      public void PushOriginUpdateBranch()
      {
         if (OriginUpdateBranch.RemoteName != null && OriginUpdateBranch.RemoteName != OriginRemote.Name)
            throw new ArgumentException($"Will not push: {nameof(OriginUpdateBranch)}.{nameof(OriginUpdateBranch.RemoteName)}'{OriginUpdateBranch.RemoteName}' != {nameof(OriginRemote)}{OriginRemote.Name}");

         if (OriginUpdateBranch.RemoteName == null)
         {
            Repo.Branches.Update(OriginUpdateBranch,
               b => b.Remote = OriginRemote.Name,
               b => b.UpstreamBranch = OriginUpdateBranch.CanonicalName);

            Log.Info($"Set remote of {nameof(OriginUpdateBranch)}['{OriginUpdateBranch.CanonicalName}'] to '{OriginRemote.Name}/{OriginRepoUrl}'");
         }

         Log.Info($"Pushing {nameof(OriginUpdateBranch)} '{OriginUpdateBranch.FriendlyName}'->'{OriginUpdateBranch.RemoteName}'");
         Repo.Network.Push(OriginUpdateBranch, new PushOptions() { CredentialsProvider = OriginCredentialsHandler });
         Log.Info($"Pushed {nameof(OriginUpdateBranch)}");
      }

      public void Dispose()
      {
         DetachUpstreamRemote();
      }
   }
}
