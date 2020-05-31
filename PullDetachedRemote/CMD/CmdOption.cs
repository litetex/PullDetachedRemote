using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace PullDetachedRemote.CMD
{
   /// <summary>
   /// Possible options that can be used when calling over commandline
   /// </summary>
   public class CmdOption
   {
      #region JSON based Config
      [Option('c', "config", HelpText = "path to the YML configuration file")]
      public string ConfigPath { get; set; } = null;

      [Option("genconf", HelpText = "generates default config YML in mentioned path")]
      public string ConfigGenerationPath { get; set; } = null;
      #endregion JSON based Config

      #region SetableBuildProperties
      /// <summary>
      /// <see cref="Config.Configuration.GitHubToken"/>
      /// </summary>
      [Option('g', "GITHUB_TOKEN", HelpText = "Preferred way: Set them via environment")]
      public string GITHUB_TOKEN { get; set; } = null;

      /// <summary>
      /// <see cref="Config.Configuration.DetachedCredsPrinicipal"/>
      /// </summary>
      [Option("DETACHED_CREDS_PRINCIPAL", HelpText = "Preferred way: Set them via environment")]
      public string DETACHED_CREDS_PRINCIPAL { get; set; } = null;

      /// <summary>
      /// <see cref="Config.Configuration.DetachedCredsPassword"/>
      /// </summary>
      [Option("DETACHED_CREDS_PW", HelpText = "Preferred way: Set them via environment")]
      public string DETACHED_CREDS_PW { get; set; } = null;

      /// <summary>
      /// <see cref="Config.Configuration.IdentityEmail"/>
      /// </summary>
      [Option("identitymail")]
      public string IdentityEmail { get; set; }

      /// <summary>
      /// <see cref="Config.Configuration.IdentityUsername"/>
      /// </summary>
      [Option("identityuser")]
      public string IdentityUsername { get; set; } 

      /// <summary>
      /// <see cref="Config.Configuration.PathToWorkingRepo"/>
      /// </summary>
      [Option('w',"workingrepopath")]
      public string PathToWorkingRepo { get; set; }

      /// <summary>
      /// <see cref="Config.Configuration.CloneMode"/>
      /// </summary>
      [Option("clonemode", HelpText = "Expected values are the enum-keys name")]
      public string CloneMode { get; set; }

      /// <summary>
      /// <see cref="Config.Configuration.OriginRepo"/>
      /// </summary>
      [Option("originrepo")]
      public string OriginRepo { get; set; } = null;

      /// <summary>
      /// <see cref="Config.Configuration.OriginBranch"/>
      /// </summary>
      [Option("originbranch")]
      public string OriginBranch { get; set; }

      /// <summary>
      /// <see cref="Config.Configuration.UpstreamRepo"/>
      /// </summary>
      [Option("upstreamrepo", HelpText = "Required")]
      public string UpstreamRepo { get; set; }

      /// <summary>
      /// <see cref="Config.Configuration.UpstreamBranch"/>
      /// </summary>
      [Option("upstreambranch")]
      public string UpstreamBranch { get; set; }

      /// <summary>
      /// <see cref="Config.Configuration.OriginUpdateBranch"/>
      /// </summary>
      [Option("nameoforiginupdatebranch")]
      public string OriginUpdateBranch { get; set; }

      /// <summary>
      /// <see cref="Config.Configuration.UpstreamRepoUseGitHubCreds"/>
      /// </summary>
      [Option("upstreamrepousegithubcreds", HelpText = "Valid inputs are true/false or 0/1")]
      public string UpstreamRepoUseGitHubCreds { get; set; }

      #endregion SetableBuildProperties

      #region Integration
      // Expect that the ARGS are something like this >>--arg \"value\"<< insteadof normally >>--arg "value"<< and fix them
      [Option(Program.EXPECT_ESCAPED_INPUT, HelpText = "Expect that the ARGS are something like this >>--arg \\\"value\\\"<< insteadof normally >>--arg \"value\"<< and fix them")]
      public bool ExpectEscapedInput { get; set; }
      #endregion Integration
   }
}
