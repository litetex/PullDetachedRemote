﻿using CoreFramework.Base.Tasks;
using Octokit;
using PullDetachedRemote.Workflow.PullRequestProcessor;
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

      protected GitHubClient GeneralClient { get; set; }

      protected GitHubClient RepoClient { get; set; }

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
         Log.Info("Starting Init task");

         GeneralClient = AuthenticateClient(Config.GitHubPAT, ct);

         if (!string.IsNullOrWhiteSpace(Config.GitHubToken))
         {
            try
            {
               RepoClient = AuthenticateClient(Config.GitHubToken, ct);
            }
            catch (OperationCanceledException)
            {
               throw;
            }
            catch (Exception ex)
            {
               Log.Warn($"Authentification failed for '{nameof(Config.GitHubToken)}'", ex);
            }
         }
         RepoClient ??= GeneralClient;

         Log.Info("Done");
      }

      protected GitHubClient AuthenticateClient(string token, CancellationToken ct)
      {
         if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException($"Token is invalid using next one");

         ct.ThrowIfCancellationRequested();

         var phv = new ProductHeaderValue(Assembly.GetEntryAssembly().GetName().Name, Assembly.GetEntryAssembly().GetName().Version.ToString());
         var client = new GitHubClient(phv)
         {
            Credentials = new Credentials(token)
         };
         Log.Info($"Created GitHubClient['{phv}']");

         var checkConTask = client.RateLimit.GetRateLimits();
         if (!checkConTask.Wait((int)TimeSpan.FromSeconds(10).TotalMilliseconds, ct))
            throw new TimeoutException("GitHubClient-ConCheck timed out");

         if (checkConTask.Result == null)
            throw new InvalidOperationException("GitHubClient-ConCheck returned invalid data");

         var rateLimit = client.GetLastApiInfo()?.RateLimit;
         Log.Info($"RateLimit: {rateLimit?.Remaining.ToString() ?? "N/A"}/{rateLimit?.Limit.ToString() ?? "N/A"} (will be reset at {rateLimit?.Reset.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"})");

         return client;
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
         userRepos.AddRange(GeneralClient.Repository.GetAllForCurrent().Result);

         foreach (Organization org in GeneralClient.Organization.GetAllForCurrent().Result)
         {
            var orgResults = GeneralClient.Repository.GetAllForOrg(org.Login).Result;
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
            var repo = GeneralClient.Repository.Get(owner, repoName).Result;
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
         PullRequest = RepoClient.PullRequest.GetAllForRepository(Repo.Id)
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

         PullRequest = RepoClient.PullRequest.Create(Repo.Id, newPr).Result;

         Log.Info($"Created PullRequest '{sourceBranchName}'->'{TargetPRBranchName}' Title='{PullRequest.Title}',ID='{PullRequest.Id}'");

         return true;
      }

      public void SetMetaToNewPR(StatusReport status)
      {
         Issue issue = null;

         if (Config.PRMetaInfo.Assignees != null && Config.PRMetaInfo.Assignees.Count > 0 ||
            Config.PRMetaInfo.Labels != null && Config.PRMetaInfo.Labels.Count > 0)
            issue = RepoClient.Issue.Get(Repo.Id, PullRequest.Number).Result;

         Log.Info("Waiting for post processing tasks of PR to end");
         TaskRunner.RunTasks(
            Task.Run(() => new PullRequestAssigneeProcessor(Config.PRMetaInfo, RepoClient, Repo, PullRequest, status).Run(issue)),
            Task.Run(() => new PullRequestLabelProcessor(Config.PRMetaInfo, RepoClient, Repo, PullRequest, status).Run(issue)),
            Task.Run(() => new PullRequestReviewerProcessor(Config.PRMetaInfo, RepoClient, Repo, PullRequest, status).Run())
         );
         Log.Info("All tasks are done");
      }

      const string STATUS_START_OLD = "<span class='DON-NOT-MOFIY-automated-pullrequest-status-start'/>";
      const string STATUS_START = "<span class='DON-NOT-MOFIY-automated-pr-status-start'/>";
      const string STATUS_END_OLD = "<span class='DON-NOT-MOFIY-automated-pullrequest-status-end'/>";
      const string STATUS_END = "<span class='DON-NOT-MOFIY-automated-pr-status-end'/>";

      public void SetPRStatus(StatusReport status)
      {
         var prBody = PullRequest.Body ?? "";

         var beforeStatus = prBody + STATUS_START + "\r\n";
         var afterStatus = "\r\n" + STATUS_END;

         if (prBody.Contains(STATUS_START) || prBody.Contains(STATUS_START_OLD))
         {
            int startIndex = prBody.IndexOf(STATUS_START);

            if (startIndex == -1)
               startIndex = prBody.IndexOf(STATUS_START_OLD);

            beforeStatus = prBody.Substring(0, startIndex) + STATUS_START;

            var strAfterBeforeStatus = prBody.Substring(startIndex + STATUS_START.Length);
            if (strAfterBeforeStatus.Contains(STATUS_END) || strAfterBeforeStatus.Contains(STATUS_END_OLD))
            {
               int endIndex = strAfterBeforeStatus.IndexOf(STATUS_END) + STATUS_END.Length;
               if(endIndex == -1)
                  endIndex = strAfterBeforeStatus.IndexOf(STATUS_END_OLD) + STATUS_END_OLD.Length;

               afterStatus = STATUS_END + strAfterBeforeStatus.Substring(endIndex);
            }
         }

         string createdPRMessage = "";
         if (status.CreatedPR)
            createdPRMessage = $"\r\nIncoming upstream update from [{Config.UpstreamRepo}]({Config.UpstreamRepo}) {(!string.IsNullOrWhiteSpace(Config.UpstreamBranch) ? $" *branch=``{Config.UpstreamBranch}``*" :"")}";

         var statusMsg = "";
         if (!Config.HidePRStatus)
            statusMsg = $"\r\n" +
               $"<details>" +
               $"<summary class='automated-pullrequest-status'><b>Status [updated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC]</b></summary>" +
               $"<p>\r\n\r\n```\r\n{status}```\r\n</p>" +
               $"</details>\r\n";

         var credits = "";
         if (!Config.HideCredits)
            credits = $"<sub>Automatically created by " +
               $"<a href=\"https://github.com/litetex/pull-detached-remote\">" +
               $"<img src=\"https://raw.githubusercontent.com/litetex/PullDetachedRemote/develop/logo.png\" height=15></img>" +
               $" {nameof(PullDetachedRemote)}</a></sub>";

         Log.Info($"Updating PR[ID='{PullRequest.Id}']");

         PullRequest = RepoClient.PullRequest.Update(Repo.Id, PullRequest.Number, new PullRequestUpdate()
         {
            Body = $"{createdPRMessage}{beforeStatus}{statusMsg}{credits}{afterStatus}",
         }).Result;
         Log.Info($"Updated PR[ID='{PullRequest.Id}']");
      }

      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
         if (disposing)
            Log.Info("Disposing");
         else
            Log.Info("Disposing via deconstructor");

         if (AsyncConstructTask != null)
         {
            AsyncConstructCancel.Cancel();

            if (AsyncConstructTask.IsCompleted)
               AsyncConstructTask.Dispose();

            AsyncConstructTask = null;
            Log.Info($"Disposed {nameof(AsyncConstructTask)}");
         }
         AsyncConstructCancel.Dispose();

         RepoClient = null;
         GeneralClient = null;
      }

      ~GithubWorkflow()
      {
         Dispose(false);
      }
   }
}
