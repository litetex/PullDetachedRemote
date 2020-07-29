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
      /// Assignees for the Pull Request on GitHub
      /// </summary>
      /// <remarks>
      /// Optional;
      /// max 10 Assignees
      /// </remarks>
      public ICollection<string> Assignees { get; set; } = new List<string>();

      /// <summary>
      /// Reviewers for the Pull Request on GitHub
      /// </summary>
      /// <remarks>
      /// Optional
      /// <list type="bullet">
      ///   <item>The user must be a </item>
      /// </list>
      /// </remarks>
      public ICollection<string> Reviewers { get; set; } = new List<string>();

      /// <summary>
      /// Labels for the Pull Request on GitHub
      /// </summary>
      /// <remarks>
      /// Optional
      /// </remarks>
      public ICollection<string> Labels { get; set; } = new List<string>();
   }
}
