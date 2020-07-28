using CoreFramework.Base.Tasks;
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

         var checkConTask = client.Miscellaneous.GetRateLimits();
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

         PullRequest = RepoClient.PullRequest.Update(Repo.Id, PullRequest.Number, new PullRequestUpdate()
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

      public void SetOrgaInfoToNewPR(StatusReport status)
      {
         Issue issue = null;
         if (Config.OrgaInfo.Assignees != null && Config.OrgaInfo.Assignees.Count > 0 &&
            Config.OrgaInfo.Labels != null && Config.OrgaInfo.Labels.Count > 0)
            issue = RepoClient.Issue.Get(Repo.Id, PullRequest.Number).Result;

         var assigneeTask = Task.Run(() =>
         {
            if(Config.OrgaInfo.Assignees == null || Config.OrgaInfo.Assignees.Count == 0)
            {
               Log.Info("No Assignees to add");
               return;
            }

            try
            {
               RepoClient.Issue.Assignee.AddAssignees(Repo.Owner.Name, Repo.Name, PullRequest.Number, new AssigneesUpdate(ProcessAssignees(issue)));
            }
            catch (Exception ex)
            {
               Log.Error("Unable to add assignees", ex);
            }
         });

         var labelsTask = Task.Run(() =>
         {
            if (Config.OrgaInfo.Labels == null || Config.OrgaInfo.Labels.Count == 0)
            {
               Log.Info("No Labels to add");
               return;
            }

            try
            {
               RepoClient.Issue.Labels.AddToIssue(Repo.Id, PullRequest.Number, ProcessLabels(issue).ToArray());
            }
            catch (Exception ex)
            {
               Log.Error("Unable to add labels", ex);
            }
         });



         var prTask = Task.Run(() =>
         {
            if (Config.OrgaInfo.Reviewers == null || Config.OrgaInfo.Reviewers.Count == 0)
            {
               Log.Info("No Labels to add");
               return;
            }

            try
            {
               ProcessReviewers();
            }
            catch (Exception ex)
            {
               Log.Error("Unable to process reviewers", ex);
            }
         });
      }

      private List<string> ProcessAssignees(Issue issue)
      {
         List<Task> assigneesTasks = new List<Task>();
         List<string> validAssignees = new List<string>();

         var alreadyExistingAssignees = issue.Assignees.Select(user => user.Login);

         Log.Info($"Trying to add {nameof(Config.OrgaInfo.Assignees)}='{string.Join(", ", Config.OrgaInfo.Assignees)}'");
         foreach (var assignee in Config.OrgaInfo.Assignees)
         {
            if (alreadyExistingAssignees.Contains(assignee))
            {
               Log.Info($"Assignee '{assignee}' is a already assigned");
               continue;
            }

            var checkIfIsAssigneeTask = RepoClient.Issue.Assignee.CheckAssignee(Repo.Id, assignee);

            assigneesTasks.Add(checkIfIsAssigneeTask.ContinueWith(isAssigneeTask =>
            {
               if (isAssigneeTask.IsFaulted)
               {
                  Log.Error($"{nameof(checkIfIsAssigneeTask)} failed", isAssigneeTask.Exception);
                  return;
               }
               if (!isAssigneeTask.Result)
               {
                  Log.Warn($"Assignee '{assignee}' is not a assigne of the current repo[ID={Repo.Id}]");
                  return;
               }

               Log.Info($"Assignee '{assignee}' is a valid assigne");
               validAssignees.Add(assignee);

            }));
         }

         Log.Info("Waiting for Assignee-verification to end");
         TaskRunner.RunTasks(assigneesTasks.ToArray());

         return validAssignees;
      }

      private List<string> ProcessLabels(Issue issue)
      {
         var labelsToAdd = new List<string>();

         var alreadyExistingLabels = issue.Labels.Select(lbl => lbl.Name);

         Log.Info($"Trying to add {nameof(Config.OrgaInfo.Labels)}='{string.Join(", ", Config.OrgaInfo.Labels)}'");

         foreach (var label in Config.OrgaInfo.Labels)
         {
            if (alreadyExistingLabels.Contains(label))
            {
               Log.Info($"Label '{label}' is a already assigned");
               continue;
            }

            labelsToAdd.Add(label);
         }

         return labelsToAdd;
      }

      private void ProcessReviewers()
      {
         var reviewersToAdd = new List<string>();

         var requestedReviews = RepoClient.PullRequest.ReviewRequest.Get(Repo.Id, PullRequest.Number).Result;

         var alreadyExistingReviewers = requestedReviews.Users.Select(user => user.Login);

         foreach (var reviewer in Config.OrgaInfo.Reviewers)
         {
            if (alreadyExistingReviewers.Contains(reviewer))
            {
               Log.Info($"Reviewer '{reviewer}' is a already assigned");
               continue;
            }

            reviewersToAdd.Add(reviewer);
         }

         RepoClient.PullRequest.ReviewRequest.Create(Repo.Id, PullRequest.Number, new PullRequestReviewRequest(, null));
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

         RepoClient = null;
         GeneralClient = null;
      }
   }
}
