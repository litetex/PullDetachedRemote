using System;
using System.Collections.Generic;
using System.Text;

namespace PullDetachedRemote.Config
{
   public class Configuration : YmlConfig
   {
      /// <summary>
      /// Detached remote repository that should be used
      /// </summary>
      public string DetachedRepo { get; set; }

      /// <summary>
      /// Detached remote branch that should be used
      /// </summary>
      public string DetachedBranch { get; set; } = "master";

      /// <summary>
      /// Optional; Name of the Branch that is used for merging / PullRequest
      /// </summary>
      public string NameOfOriginUpdateBranch { get; set; }
   }
}
