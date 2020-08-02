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
   public class PullRequestLabelProcessor : PullRequestPartProcessor
   {
      public PullRequestLabelProcessor(
         PullRequestMetaInfoConfig prMetaConfig,
         GitHubClient repoClient,
         Repository repo,
         PullRequest pullRequest,
         StatusReport statusReport) : 
         base(prMetaConfig, repoClient, repo, pullRequest, statusReport)
      {

      }

      public async Task Run(Issue issue)
      {
         if (issue == null || PRMetaConfig.Labels == null || PRMetaConfig.Labels.Count == 0)
         {
            Log.Info("No labels to add");
            return;
         }

         try
         {
            Log.Info("Start processing labels");
            var labels = ProcessLabels(issue);
            var resultLabels = await RepoClient.Issue.Labels.AddToIssue(Repo.Id, PullRequest.Number, labels.ToArray());

            IEnumerable<string> labelsOfPR = resultLabels.Select(lbl => lbl.Name);

            foreach (var label in labels.Where(r => !labelsOfPR.Contains(r)))
            {
               var warnMsg = $"Label '{label}' was not added to PR";
               Log.Warn(warnMsg);
               Status.Messages.Add(warnMsg);
            }

            Log.Info($"Labels of PR: [{(resultLabels.Count > 0 ? $"'{string.Join("', '", labelsOfPR)}'" : "")}]");
         }
         catch (Exception ex)
         {
            Status.UncriticalErrors = true;
            Log.Error("Unable to add labels", ex);
         }
      }

      private List<string> ProcessLabels(Issue issue)
      {
         var labelsToAdd = new List<string>();

         var alreadyExistingLabels = issue.Labels.Select(lbl => lbl.Name);

         Log.Info($"Trying to add {nameof(PRMetaConfig.Labels)}='{string.Join(", ", PRMetaConfig.Labels)}'");

         foreach (var label in PRMetaConfig.Labels)
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
   }
}
