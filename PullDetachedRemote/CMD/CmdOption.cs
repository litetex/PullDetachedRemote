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
      [Option('g', "GITHUB_TOKEN", HelpText = "GITHUB_TOKEN")]
      public string GITHUB_TOKEN { get; set; } = null;

      /// <summary>
      /// <see cref="Config.Configuration.DetachedCredsPrinicipal"/>
      /// </summary>
      [Option("DETACHED_CREDS_PRINCIPAL", HelpText = "DETACHED_CREDS_PRINCIPAL")]
      public string DETACHED_CREDS_PRINCIPAL { get; set; } = null;

      /// <summary>
      /// <see cref="Config.Configuration.DetachedCredsPassword"/>
      /// </summary>
      [Option("DETACHED_CREDS_PW", HelpText = "DETACHED_CREDS_PW")]
      public string DETACHED_CREDS_PW { get; set; } = null;
      #endregion SetableBuildProperties
   }
}
