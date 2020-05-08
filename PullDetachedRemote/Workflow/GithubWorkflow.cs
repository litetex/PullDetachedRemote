using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PullDetachedRemote.Workflow
{
   public class GithubWorkflow : IDisposable
   {
      protected Config.Configuration Config { get; set; }

      protected GitHubClient Client { get; set; }

      protected Repository Repo { get; set; }

      public GithubWorkflow(Config.Configuration config)
      {
         Config = config;
      }

      public void Init(string originRemote)
      {
         var phv = new ProductHeaderValue(Assembly.GetEntryAssembly().GetName().Name, Assembly.GetEntryAssembly().GetName().Version.ToString());
         Client = new GitHubClient(phv)
         {
            Credentials = new Credentials(Config.GitHubToken)
         };
         Log.Info($"Created GitHubClient['{phv}']");

         var checkConTask = Client.Miscellaneous.GetRateLimits();
         if (!checkConTask.Wait(TimeSpan.FromSeconds(10)))
            throw new TimeoutException("GitHubClient-ConCheck timed out");

         if (checkConTask.Result == null)
            throw new InvalidOperationException("GitHubClient-ConCheck returned invalid data");

         Log.Info($"Connection tested succesfully; RateLimit: {Client.GetLastApiInfo()?.RateLimit?.Remaining}/{Client.GetLastApiInfo()?.RateLimit?.Limit}");


         if (!GetRepoFrom(originRemote))
            throw new ArgumentException("Unable to get GH-Repo from remote origin");

         Log.Info($"Using Repo[Owner='{Repo.Owner.Login}', Name='{Repo.Name}', URL='{Repo.CloneUrl}']");
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


      public void EnsurePullRequestCreated(string upstreamRepoUrl, string sourceBranchName)
      {
         var targetBranchname = Config.BaseOriginBranch ?? Repo.DefaultBranch;

         var existingPr = Client.PullRequest.GetAllForRepository(Repo.Id)
            .Result
            .FirstOrDefault(pr => 
               pr.Base.Ref == targetBranchname && 
               pr.Head.Ref == sourceBranchName);

         if (existingPr != null)
         {
            Log.Info($"There is already a PullRequest for '{sourceBranchName}'->'{targetBranchname}'");
            return;
         }

         Log.Info($"Creating PullRequest '{sourceBranchName}'->'{targetBranchname}'");
         var newPr = new NewPullRequest($"[UpstreamUpdate] from {upstreamRepoUrl}", sourceBranchName, targetBranchname);

         var pr = Client.PullRequest.Create(Repo.Id, newPr).Result;

         Log.Info($"Created PullRequest '{sourceBranchName}'->'{targetBranchname}' Title='{pr.Title}'");
      }

      public void Dispose()
      {
         Client = null;
      }
   }
}
