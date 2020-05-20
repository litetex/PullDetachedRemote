using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

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

      public void Init(CredentialsHandler originCredentialsHandler, CredentialsHandler upstreamCredentialsHandler, bool doClone)
      {
         OriginCredentialsHandler = originCredentialsHandler;
         UpstreamCredentialsHandler = upstreamCredentialsHandler;

         // Configure identity
         Identity = new Identity(Config.IdentityUsername, Config.IdentityEmail);
         Log.Info($"Using Identity: Username='{Identity.Name}', Email='{Identity.Email}'");

         // Init Repo
         if (doClone)
         {
            if (Directory.Exists(Config.PathToWorkingRepo))
            {
               Log.Info($"Deleting existing folder {nameof(Config.PathToWorkingRepo)}='{Config.PathToWorkingRepo}'");
               DeleteGitRepoSafe(Config.PathToWorkingRepo);
               Log.Info($"Deleted {nameof(Config.PathToWorkingRepo)}='{Config.PathToWorkingRepo}'");
            }

            Log.Info($"Cloning '{Config.OriginRepo}' into '{Config.PathToWorkingRepo}'");
            // NOTE: The path will be created if it doesn't exist. Git does this natively ;)
            Config.PathToWorkingRepo = Repository.Clone(Config.OriginRepo, Config.PathToWorkingRepo, new CloneOptions() { CredentialsProvider = OriginCredentialsHandler });
            Log.Info($"Cloned successfully into '{Config.PathToWorkingRepo}'");
         }

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
      }

      // NOTE: Directory.Delete is not working because some file are read-only...
      protected void DeleteGitRepoSafe(string directory)
      {
         foreach (string subdirectory in Directory.EnumerateDirectories(directory))
         {
            DeleteGitRepoSafe(subdirectory);
         }

         foreach (string fileName in Directory.EnumerateFiles(directory))
         {
            var fileInfo = new FileInfo(fileName)
            {
               Attributes = FileAttributes.Normal
            };
            fileInfo.Delete();
         }

         //Something was not fast enough to delete, so let's wait a moment
         if (Directory.EnumerateFiles(directory).Any())
         {
            Thread.Sleep(10);

            //Again? - Wait a moment longer
            if (Directory.EnumerateFiles(directory).Any())
            {
               Thread.Sleep(100);
            }
         }

         int[] waitMs = new int[] { 10, 100, 1000, 5000 };
         var waitStartIndex = 0;
         while (Directory.EnumerateFiles(directory).Any() || Directory.EnumerateDirectories(directory).Any())
         {
            if (waitStartIndex > waitMs.Length - 1)
               throw new TimeoutException($"Failed to delete '{directory}' in given time");
            Thread.Sleep(waitMs[waitStartIndex]);
            waitStartIndex++;
         }


         Directory.Delete(directory, true);
      }

      // NOTE: Not optimized
      public string GetDefaultUpstreamBranch()
      {
         var tempFilesystemStructure = Path.GetTempFileName();
         File.Delete(tempFilesystemStructure);
         
         Log.Info($"Getting default branch of upstream; Using templocation='{tempFilesystemStructure}'");
         Directory.CreateDirectory(tempFilesystemStructure);

         try
         {
            using var repo = new Repository(
               Repository.Clone(Config.UpstreamRepo, tempFilesystemStructure, new CloneOptions()
               {
                  Checkout = false,
                  CredentialsProvider = UpstreamCredentialsHandler
               })
            );
            return repo.Branches.SingleOrDefault(x => x.IsCurrentRepositoryHead)?.FriendlyName;
         }
         finally
         {
            DeleteGitRepoSafe(tempFilesystemStructure);
            Log.Info($"Deleted templocation='{tempFilesystemStructure}'");
         }
      }

      public void InitUpstreamBranch()
      {
         UpstreamRemote = Repo.Network.Remotes.FirstOrDefault(x => x.PushUrl == Config.UpstreamRepo);


         if (UpstreamRemote != null)
         {
            Log.Info($"Found already existing upstream remote '{UpstreamRemote.Name}'/'{UpstreamRemote.PushUrl}'");
         }
         else
         {
            var upstreamRemoteName = GenerateRemoteUpstreamName(Repo.Network.Remotes.Select(x => x.Name));
            UpstreamRemote = Repo.Network.Remotes.Add(upstreamRemoteName, Config.UpstreamRepo);

            Log.Info($"Added upstream remote '{UpstreamRemote.Name}'/'{UpstreamRemote.PushUrl}'");
         }


         Log.Info($"Using upstream-remote '{UpstreamRemote.Name}'<-'{Config.UpstreamRepo}'");

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

         UpstreamBranch = Repo.Branches.First(b => b.FriendlyName == $"{UpstreamRemote.Name}/{Config.UpstreamBranch}");
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
         return HasNewerCommitsThanWithLogging("upstream-remote", UpstreamBranch, "origin-update", OriginUpdateBranch);
      }

      public bool HasUpdateBranchNewerCommitsThanUpstreamBranch()
      {
         return HasNewerCommitsThanWithLogging("origin-update", OriginUpdateBranch, "upstream-remote", UpstreamBranch);
      }

      public bool HasUpdateBranchNewerCommitsThanPRTargetedBranch(string prTargetedBranchName)
      {
         var prTarget = Repo.Branches.FirstOrDefault(b => b.FriendlyName == $"origin/{prTargetedBranchName}");

         return HasNewerCommitsThanWithLogging("origin-update", OriginUpdateBranch, "pull-request-target/origin", prTarget, false);
      }

      /// <summary>
      /// Checks if base has newer commits than target (with logging)
      /// </summary>
      protected bool HasNewerCommitsThanWithLogging(
         string actualName,
         Branch actual,
         string targetName,
         Branch target,
         bool logCommitDiff = true)
      {
         Log.Info($"Checking if {actualName} branch has newer commits than {targetName} branch '{target.FriendlyName}'");
         var updateOriginCommitLog = Repo.Commits.QueryBy(new CommitFilter()
         {
            IncludeReachableFrom = actual,
            ExcludeReachableFrom = target,
         });
         if (!updateOriginCommitLog.Any())
         {
            Log.Info($"No new commits on {actualName} branch '{actual.FriendlyName}'");
            return false;
         }
         Log.Info($"Detected {updateOriginCommitLog.Count()} new commits on {actualName} branch '{actual.FriendlyName}':");
         if(logCommitDiff)
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
         if (toDetachremote == null)
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
         Log.Info("Disposing");
         if (Repo != null)
         {
            DetachUpstreamRemote();
            Repo.Dispose();

            Log.Info($"Disposed {nameof(Repo)}");

            Repo = null;
         }
      }
   }
}
