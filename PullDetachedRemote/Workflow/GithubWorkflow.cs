using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace PullDetachedRemote.Workflow
{
   public class GithubWorkflow : IDisposable
   {
      protected Config.Configuration Config { get; set; }

      protected GitHubClient Client { get; set; }

      protected Repository Repo { get; set; }

      protected PullRequest PullRequest { get; set; }

      /// <summary>
      /// Targeted Branch to merge the PR into
      /// </summary>
      public string TargetPRBranchName { get; protected set; }

      protected CancellationTokenSource AsyncConstructCancel { get; } = new CancellationTokenSource();

      protected Task AsyncConstructTask { get; set; }

      public GithubWorkflow(Config.Configuration config)
      {
         Config = config;

         AsyncConstructTask = Task.Run(() => ConstructorAsyncInitTask(AsyncConstructCancel.Token), AsyncConstructCancel.Token);
      }

      protected void ConstructorAsyncInitTask(CancellationToken ct)
      {
         Dictionary<string, string> tokens = new Dictionary<string, string>()
         {
            // Use GithubToken primarily
            { nameof(Config.GitHubToken), Config.GitHubToken },
            { nameof(Config.GitHubPAT), Config.GitHubPAT },
         };

         bool success = false;

         foreach (KeyValuePair<string, string> token in tokens)
         {
            if(string.IsNullOrWhiteSpace(token.Value))
            {
               Log.Info($"Token '{token.Key}' is invalid using next one");
               continue;
            }

            Log.Info($"Trying to authenticated with '{token.Key}'=****");

            ct.ThrowIfCancellationRequested();
            try
            {
               var phv = new ProductHeaderValue(Assembly.GetEntryAssembly().GetName().Name, Assembly.GetEntryAssembly().GetName().Version.ToString());
               Client = new GitHubClient(phv)
               {
                  Credentials = new Credentials(token.Value)
               };
               Log.Info($"Created GitHubClient['{phv}']");

               var checkConTask = Client.Miscellaneous.GetRateLimits();
               if (!checkConTask.Wait((int)TimeSpan.FromSeconds(10).TotalMilliseconds, ct))
                  throw new TimeoutException("GitHubClient-ConCheck timed out");

               if (checkConTask.Result == null)
                  throw new InvalidOperationException("GitHubClient-ConCheck returned invalid data");

               var rateLimit = Client.GetLastApiInfo()?.RateLimit;
               Log.Info($"RateLimit: {rateLimit?.Remaining.ToString() ?? "N/A"}/{rateLimit?.Limit.ToString() ?? "N/A"} (will be reset at {rateLimit?.Reset.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"})");

               Log.Info($"Connection tested succesfully using '{token.Key}'");

               // Sucess
               success = true;
               break;
            }
            catch(OperationCanceledException)
            {
               throw;
            }
            catch(Exception ex)
            {
               Log.Warn("Authentification failed", ex);
            }
         }

         if (!success)
         {
            Log.Info("Init failed!");
            throw new InvalidOperationException("Failed to establish connectivity with api");
         }

         Log.Info("Done");
      }

      public void Init(string originRemote)
      {
         if (!AsyncConstructTask.IsCompleted)
         {
            Log.Info($"Waiting for {nameof(AsyncConstructTask)} to finish");

            try
            {
               if (!AsyncConstructTask.Wait(TimeSpan.FromSeconds(25)))
                  throw new TimeoutException($"{nameof(AsyncConstructTask)} took to long");
            }
            finally
            {
               AsyncConstructTask.Dispose();
               AsyncConstructTask = null;
               Log.Info($"Disposed {nameof(AsyncConstructTask)}");
            }
            Log.Info($"{nameof(AsyncConstructTask)} finished");
         }

         if (!GetRepoFrom(originRemote))
            throw new ArgumentException("Unable to get GH-Repo from remote origin");

         Log.Info($"Using Repo[Owner='{Repo.Owner.Login}', Name='{Repo.Name}', CloneUrl='{Repo.CloneUrl}']");

         TargetPRBranchName = Config.OriginBranch ?? Repo.DefaultBranch;

         Log.Info($"Targeting branch for PR is '{TargetPRBranchName}'");
      }

      protected bool GetRepoFrom(string originRemote)
      {
         originRemote = RemoveGitExt(originRemote);

         var remoteUri = new Uri(originRemote);
         var pathAndQuery = remoteUri.PathAndQuery;
         if (pathAndQuery.Contains('/') && !pathAndQuery.Contains('?') && !pathAndQuery.Contains('&') && !pathAndQuery.Contains('#'))
         {
            var parts = pathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && ValidateSet(parts[0], parts[1]))
               return true;
         }

         // Fallback
         Log.Warn("Couldn't parse origin remote. Using fallback");

         var userRepos = new List<Repository>();
         userRepos.AddRange(Client.Repository.GetAllForCurrent().Result);

         foreach (Organization org in Client.Organization.GetAllForCurrent().Result)
         {
            var orgResults = Client.Repository.GetAllForOrg(org.Login).Result;
            if (orgResults != null && orgResults.Count > 0)
               userRepos.AddRange(orgResults);
         }

         var repos = userRepos.FindAll(r => RemoveGitExt(r.CloneUrl) == originRemote);
         if (repos.Count != 1)
         {
            Log.Warn($"Found {repos.Count} Repos that could match '{originRemote}'");
            return false;
         }

         var repo = repos[0];
         if (!Validate(repo))
            return false;
         Repo = repo;

         return true;
      }

      private string RemoveGitExt(string remote)
      {
         return remote.EndsWith(".git") ? remote[0..^4] : remote;
      }

      protected bool ValidateSet(string owner, string repoName)
      {
         try
         {
            var repo = Client.Repository.Get(owner, repoName).Result;
            if (!Validate(repo))
               return false;

            Repo = repo;
            return true;
         }
         catch
         {
            return false;
         }
      }

      protected bool Validate(Repository repo)
      {
         return repo.Permissions.Push;
      }


      public bool EnsurePullRequestCreated(string upstreamRepoUrl, string sourceBranchName)
      {
         PullRequest = Client.PullRequest.GetAllForRepository(Repo.Id)
            .Result
            .FirstOrDefault(pr =>
               pr.Base.Ref == TargetPRBranchName &&
               pr.Head.Ref == sourceBranchName);

         if (PullRequest != null)
         {
            Log.Info($"There is already a PullRequest for '{sourceBranchName}'->'{TargetPRBranchName}'");
            return false;
         }

         Log.Info($"Creating PullRequest '{sourceBranchName}'->'{TargetPRBranchName}'");
         var newPr = new NewPullRequest($"UpstreamUpdate from {upstreamRepoUrl}", sourceBranchName, TargetPRBranchName);

         PullRequest = Client.PullRequest.Create(Repo.Id, newPr).Result;

         Log.Info($"Created PullRequest '{sourceBranchName}'->'{TargetPRBranchName}' Title='{PullRequest.Title}',ID='{PullRequest.Id}'");

         return true;
      }

      const string STATUS_START = "<span class='DON-NOT-MOFIY-automated-pullrequest-status-start'/>";
      const string STATUS_END = "<span class='DON-NOT-MOFIY-automated-pullrequest-status-end'/>";

      public void SetPRStatus(StatusReport status)
      {
         var prBody = PullRequest.Body ?? "";

         var beforeStatus = prBody + STATUS_START;
         var afterStatus = STATUS_END;

         if (prBody.Contains(STATUS_START))
         {
            int startIndex = prBody.IndexOf(STATUS_START);
            beforeStatus = prBody.Substring(0, startIndex) + STATUS_START;

            var strAfterBeforeStatus = prBody.Substring(startIndex + STATUS_START.Length);
            if (strAfterBeforeStatus.Contains(STATUS_END))
            {
               int endIndex = strAfterBeforeStatus.IndexOf(STATUS_END);
               afterStatus = strAfterBeforeStatus.Substring(endIndex);
            }
         }

         Log.Info($"Updating PR[ID='{PullRequest.Id}']");

         PullRequest = Client.PullRequest.Update(Repo.Id, PullRequest.Number, new PullRequestUpdate()
         {
            Body = $"{beforeStatus}" +
               $"\r\n" +
               $"<details>" +
               $"<summary class='automated-pullrequest-status'><b>Status [updated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC]</b></summary>" +
               $"<p>\r\n\r\n```\r\n{status}```\r\n</p>" +
               $"</details>\r\n" +
               $"{afterStatus}",
         }).Result;
         Log.Info($"Updated PR[ID='{PullRequest.Id}']");
      }

      public void Dispose()
      {
         Log.Info("Disposing");

         if (AsyncConstructTask != null)
         {
            AsyncConstructCancel.Cancel();

            if (AsyncConstructTask.IsCompleted)
               AsyncConstructTask.Dispose();
            AsyncConstructTask = null;
            Log.Info($"Disposed {nameof(AsyncConstructTask)}");
         }
         AsyncConstructCancel.Dispose();

         Client = null;
      }
   }
}
