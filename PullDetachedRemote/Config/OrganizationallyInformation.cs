using System;
using System.Collections.Generic;
using System.Text;

namespace PullDetachedRemote.Config
{
   public class OrganizationallyInformation
   {
      /// <summary>
      /// Email for commits
      /// </summary>
      /// <remarks>
      /// Required
      /// </remarks>
      public string IdentityEmail { get; set; } = null;

      /// <summary>
      /// User for commits
      /// </summary>
      /// <remarks>
      /// Optional; default "nameofThisProject Version"
      /// </remarks>
      public string IdentityUsername { get; set; } = null;

      /// <summary>
      /// Assignees for the Pull Request on GitHub
      /// </summary>
      /// <remarks>
      /// Optional;
      /// max 10 Assignees
      /// </remarks>
      public List<string> Assignees { get; set; } = new List<string>();

      /// <summary>
      /// Reviewers for the Pull Request on GitHub
      /// </summary>
      /// <remarks>
      /// Optional
      /// <list type="bullet">
      ///   <item>The user must be a </item>
      /// </list>
      /// </remarks>
      public List<string> Reviewers { get; set; } = new List<string>();

      /// <summary>
      /// Labels for the Pull Request on GitHub
      /// </summary>
      /// <remarks>
      /// Optional
      /// </remarks>
      public List<string> Labels { get; set; } = new List<string>();
   }
}
