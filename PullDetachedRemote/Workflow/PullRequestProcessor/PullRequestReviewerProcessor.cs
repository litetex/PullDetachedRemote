using CoreFramework.Base.Tasks;
using Octokit;
using PullDetachedRemote.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PullDetachedRemote.Workflow.PullRequestProcessor
{
   public class PullRequestReviewerProcessor : PullRequestPartProcessor
   {
      public PullRequestReviewerProcessor(
         PullRequestMetaInfoConfig prMetaConfig,
         GitHubClient repoClient,
         Repository repo,
         PullRequest pullRequest,
         StatusReport statusReport) : 
         base(prMetaConfig, repoClient, repo, pullRequest, statusReport)
      {

      }

      public async Task Run()
      {
         if (PRMetaConfig.Reviewers == null || PRMetaConfig.Reviewers.Count == 0)
         {
            Log.Info("No reviewers to add");
            return;
         }

         try
         {
            Log.Info("Start processing reviewers");
            await ProcessReviewers();
         }
         catch (Exception ex)
         {
            Status.UncriticalErrors = true;
            Log.Error("Unable to process reviewers", ex);
         }
      }

      private async Task ProcessReviewers()
      {
         var reviewersToAdd = new List<string>();

         var requestedReviews = await RepoClient.PullRequest.ReviewRequest.Get(Repo.Id, PullRequest.Number);

         var alreadyExistingReviewers = requestedReviews.Users.Select(user => user.Login);

         Log.Info($"Trying to add {nameof(PRMetaConfig.Reviewers)}='{string.Join(", ", PRMetaConfig.Reviewers)}'");
         foreach (var reviewer in PRMetaConfig.Reviewers)
         {
            if (alreadyExistingReviewers.Contains(reviewer))
            {
               Log.Info($"Reviewer '{reviewer}' is a already assigned");
               continue;
            }

            reviewersToAdd.Add(reviewer);
         }

         var pr = await RepoClient.PullRequest.ReviewRequest.Create(Repo.Id, PullRequest.Number, new PullRequestReviewRequest(reviewersToAdd, null));

         IEnumerable<string> reviewersOfPR = pr.RequestedReviewers.Select(user => user.Login);

         foreach (var reviewer in reviewersToAdd.Where(r => !reviewersOfPR.Contains(r)))
         {
            var warnMsg = $"Reviewer '{reviewer}' was not added to PR";
            Log.Warn(warnMsg);
            Status.Messages.Add(warnMsg);
         }

         Log.Info($"Reviewers of PR: [{(pr.RequestedReviewers.Count > 0 ? $"'{string.Join("', '", reviewersOfPR)}'" : "")}]");
      }
   }
}
