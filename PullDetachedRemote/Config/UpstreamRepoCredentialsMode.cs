using CoreFramework.CrashLogging;
using System;
using System.Collections.Generic;
using System.Text;

namespace PullDetachedRemote.Config
{
   /// <summary>
   /// Determines which credentials are used for authetification at <see cref="Configuration.UpstreamRepo"/> (or how they are determined / used)
   /// </summary>
   public enum UpstreamRepoCredentialsMode
   {
      /// <summary>
      /// Automatic (default)<para/>
      /// Determine the credentials automatically<para/>
      /// </summary>
      AUTO,

      /// <summary>
      /// Use no credentials for authentification
      /// </summary>
      /// <remarks>
      /// Not recommend: You may ran into a rate limit (accessing GitHub-Repos)
      /// </remarks>
      NONE,

      /// <summary>
      /// Use the GitHub-Credentials <see cref="Configuration.GitHubPAT"/>
      /// </summary>
      GITHUB,

      /// <summary>
      /// Use the credentials set by <see cref="Configuration.DetachedCredsPrinicipal"/> and/or <see cref="Configuration.DetachedCredsPassword"/>
      /// </summary>
      CUSTOM

   }
}
