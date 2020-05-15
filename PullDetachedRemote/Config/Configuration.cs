﻿using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace PullDetachedRemote.Config
{
   public class Configuration : YmlConfig
   {
      /// <summary>
      /// Email for commits, if not set "action@github.com"
      /// </summary>
      public string IdentityEmail { get; set; } = null;

      /// <summary>
      /// User for commits, if not set "GitHub Action - nameofThisProject Version"
      /// </summary>
      public string IdentityUsername { get; set; } = null;

      /// <summary>
      /// Path to working repo; default = executable is in repo
      /// </summary>
      public string PathToWorkingRepo { get; set; } = null;

      // TODO: Checkout working repo if not exists?

      /// <summary>
      /// Where to merge the PR into
      /// </summary>
      public string BaseOriginBranch { get; set; }

      /// <summary>
      /// Required; Detached remote repository that should be used
      /// </summary>
      public string BaseUpstreamRepo { get; set; }

      /// <summary>
      /// Detached remote branch that should be used
      /// </summary>
      public string BaseUpstreamBranch { get; set; }

      /// <summary>
      /// Optional; Name of the Branch that is used for merging / PullRequest
      /// </summary>
      public string NameOfOriginUpdateBranch { get; set; }

      /// <summary>
      /// GITHUB_TOKEN
      /// <para/>
      /// Config possible over:
      ///  - Commandline
      ///  - Environment
      /// </summary>
      /// <seealso cref="https://help.github.com/en/actions/configuring-and-managing-workflows/authenticating-with-the-github_token"/>
      [YamlIgnore]
      public string GitHubToken { get; set; } = null;

      /// <summary>
      /// if true uses <see cref="GitHubToken"/> for the <see cref="BaseUpstreamRepo"/>
      /// </summary>
      public bool UpstreamRepoUseGitHubCreds { get; set; } = true;

      /// <summary>
      /// DETACHED_CREDS_PRINCIPAL <para/>
      /// Username or token for detached/remote repo <para/>
      /// <para/>
      /// Config possible over:
      ///  - Commandline
      ///  - Environment
      /// </summary>
      [YamlIgnore]
      public string DetachedCredsPrinicipal { get; set; } = null;

      /// <summary>
      /// DETACHED_CREDS_PW <para/>
      /// Password for detached/remote repo
      /// <para/>
      /// Config possible over:
      ///  - Commandline
      ///  - Environment
      /// </summary>
      [YamlIgnore]
      public string DetachedCredsPassword { get; set; } = null;

   }
}
