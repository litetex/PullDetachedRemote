using System;
using System.Collections.Generic;
using System.Text;

namespace PullDetachedRemote.Config
{
   /// <summary>
   /// Mode that determines when to clone
   /// </summary>
   public enum CloneMode
   {
      /// <summary>
      /// Assumes that the repo already exists
      /// </summary>
      DO_NOTHING,
      /// <summary>
      /// Clone only if the repo was not found
      /// </summary>
      CLONE_IF_NOT_FOUND,
      /// <summary>
      /// Clone always. Remove and reclone existing repos
      /// </summary>
      CLONE_ALWAYS,
   }
}
