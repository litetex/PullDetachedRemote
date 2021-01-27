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
   public class PullRequestAssigneeProcessor : PullRequestPartProcessor
   {
      public PullRequestAssigneeProcessor(
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
         if (issue == null || PRMetaConfig.Assignees == null || PRMetaConfig.Assignees.Count == 0)
         {
            Log.Info("No assignees to add");
            return;
         }

         try
         {
            Log.Info("Start processing assignees");
            var assignees = ProcessAssignees(issue);
            var resultIssue = await RepoClient.Issue.Assignee.AddAssignees(Repo.Owner.Login, Repo.Name, PullRequest.Number, new AssigneesUpdate(assignees));

            IEnumerable<string> assigneesOfPR = resultIssue.Assignees.Select(user => user.Login);

            foreach (var assignee in assignees.Where(r => !assigneesOfPR.Contains(r)))
            {
               var warnMsg = $"Assignee '{assignee}' was not added to PR";
               Log.Warn(warnMsg);
               Status.Messages.Add(warnMsg);
            }

            Log.Info($"Assignees of PR: [{(resultIssue.Assignees.Count > 0 ? $"'{string.Join("', '", assigneesOfPR)}'" : "")}]");
         }
         catch (Exception ex)
         {
            Status.UncriticalErrors = true;
            Log.Error("Unable to add assignees", ex);
         }
      }

      private List<string> ProcessAssignees(Issue issue)
      {
         List<Task> assigneesTasks = new List<Task>();
         List<string> validAssignees = new List<string>();

         var alreadyExistingAssignees = issue.Assignees.Select(user => user.Login);

         Log.Info($"Trying to add {nameof(PRMetaConfig.Assignees)}='{string.Join(", ", PRMetaConfig.Assignees)}'");
         foreach (var assignee in PRMetaConfig.Assignees)
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
                  Status.UncriticalErrors = true;

                  return;
               }

               if (!isAssigneeTask.Result)
               {
                  var warnMsg = $"Can not assign assignee '{assignee}': does not belong to current repo[ID={Repo.Id}]";

                  Log.Warn(warnMsg);
                  Status.Messages.Add(warnMsg);

                  return;
               }

               Log.Info($"Assignee '{assignee}' is a valid assignee");
               validAssignees.Add(assignee);

            }));
         }

         Log.Info("Waiting for Assignee-verification to end");
         TaskRunner.RunTasks(assigneesTasks.ToArray());

         return validAssignees;
      }
   }
}
