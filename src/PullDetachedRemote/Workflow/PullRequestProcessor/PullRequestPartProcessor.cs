using Octokit;
using PullDetachedRemote.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace PullDetachedRemote.Workflow.PullRequestProcessor
{
   public abstract class PullRequestPartProcessor
   {
      protected PullRequestMetaInfoConfig PRMetaConfig { get; set; }

      protected GitHubClient RepoClient { get; set; }

      protected Repository Repo { get; set; }

      protected PullRequest PullRequest { get; set; }

      protected StatusReport Status { get; set; }

      protected PullRequestPartProcessor(
         PullRequestMetaInfoConfig prMetaConfig,
         GitHubClient repoClient,
         Repository repo,
         PullRequest pullRequest,
         StatusReport statusReport)
      {
         PRMetaConfig = prMetaConfig;
         RepoClient = repoClient;
         Repo = repo;
         PullRequest = pullRequest;
         Status = statusReport;
      }
   }
}
