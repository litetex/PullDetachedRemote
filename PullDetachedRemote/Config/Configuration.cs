using CoreFramework.Config;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace PullDetachedRemote.Config
{
   public class Configuration : YamlConfig
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
      /// PullRequest Meta Info
      /// </summary>
      /// <remarks>
      /// Required
      /// </remarks>
      public PullRequestMetaInfoConfig PRMetaInfo { get; set; } = new PullRequestMetaInfoConfig();

      /// <summary>
      /// Path to working repo
      /// </summary>
      /// <remarks>
      /// Required; default is workdir/gitrepo
      /// </remarks>
      public string PathToWorkingRepo { get; set; } = "workdir/gitrepo";

      /// <summary>
      /// Clonemode for the repo in <see cref="PathToWorkingRepo"/>
      /// </summary>
      public CloneMode CloneMode { get; set; } = CloneMode.DO_NOTHING;

      /// <summary>
      /// Only required for cloning
      /// </summary>
      /// <remarks>
      /// Optional
      /// </remarks>
      public string OriginRepo { get; set; } = null;

      /// <summary>
      /// The branch(name) to merge the PR into
      /// </summary>
      /// <remarks>
      /// Optional; default is Github-Repos default-Branch
      /// </remarks>
      public string OriginBranch { get; set; }

      /// <summary>
      /// Detached remote repository that should be used
      /// </summary>
      /// <remarks>
      /// Required
      /// </remarks>
      public string UpstreamRepo { get; set; }

      /// <summary>
      /// Detached remote branch that should be used.
      /// </summary>
      /// <remarks>
      /// Optional; WARNING: It's recommend to set this branch, otherwise the process may suffer poor performance
      /// </remarks>
      public string UpstreamBranch { get; set; }

      /// <summary>
      /// Name of the branch that will be created in the <see cref="OriginRepo"/> with the changes from <see cref="UpstreamRepo"/>/<see cref="UpstreamBranch"/>
      /// </summary>
      /// <remarks>
      /// Optional; default is a auto generated name from the upstreamRepo + Branch
      /// </remarks>
      public string OriginUpdateBranch { get; set; }

      /// <summary>
      /// GITHUB_TOKEN <para/>
      /// used for:
      /// <list type="bullet">
      ///   <item>communication with the Github-API (context: repo only; primary)</item>
      /// </list>
      /// <para/>
      /// NOTE: You can't write to the current repo
      /// </summary>
      /// <seealso cref="https://help.github.com/en/actions/configuring-and-managing-workflows/authenticating-with-the-github_token"/>
      /// <remarks>
      /// Optional; If not set, <see cref="Configuration.GitHubPAT"/> is used
      /// </remarks>
      [YamlIgnore]
      public string GitHubToken { get; set; } = null;

      /// <summary>
      /// GITHUB_Personal Access Key <para/>
      /// used for:
      /// <list type="bullet">
      ///   <item>read/write operations in Repositories</item>
      ///   <item>communication with the Github-API (general/fallback; NOTE: The token owner wil be the pull request creator)</item>
      /// </list>
      /// </summary>
      /// <remarks>
      /// Required
      /// </remarks>
      [YamlIgnore]
      public string GitHubPAT {get; set;} = null;

      /// <summary>
      /// <see cref="UpstreamRepoCredentialsMode"/>
      /// <remarks>
      /// Optional
      /// </remarks>
      public UpstreamRepoCredentialsMode UpstreamCredMode { get; set; } = UpstreamRepoCredentialsMode.AUTO;

      /// <summary>
      /// DETACHED_CREDS_PRINCIPAL <para/>
      /// Username or token for detached/remote repo <para/>
      /// </summary>
      /// <remarks>
      /// Optional
      /// </remarks>
      [YamlIgnore]
      public string DetachedCredsPrinicipal { get; set; } = null;

      /// <summary>
      /// DETACHED_CREDS_PW <para/>
      /// Password for detached/remote repo
      /// </summary>
      /// <remarks>
      /// Optional
      /// </remarks>
      [YamlIgnore]
      public string DetachedCredsPassword { get; set; } = null;

   }
}
