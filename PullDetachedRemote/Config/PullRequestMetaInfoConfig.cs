using System;
using System.Collections.Generic;
using System.Text;

namespace PullDetachedRemote.Config
{
   /// <summary>
   /// Meta-Information for PullRequests
   /// </summary>
   public class PullRequestMetaInfoConfig
   {
      /// <summary>
      /// Assignees for the Pull Request on GitHub (mutally exclusive with Reviewers)
      /// </summary>
      /// <remarks>
      /// Optional;
      /// max 10 Assignees
      /// </remarks>
      public ICollection<string> Assignees { get; set; } = new List<string>();

      /// <summary>
      /// Reviewers for the Pull Request on GitHub (mutally exclusive with Assignees)
      /// </summary>
      /// <remarks>
      /// Optional
      /// </remarks>
      public ICollection<string> Reviewers { get; set; } = new List<string>();

      /// <summary>
      /// Labels for the Pull Request on GitHub
      /// </summary>
      /// <remarks>
      /// Optional;
      /// if don't exists will create a label
      /// </remarks>
      public ICollection<string> Labels { get; set; } = new List<string>();
   }
}
