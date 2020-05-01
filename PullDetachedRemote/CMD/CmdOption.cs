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
   }
}
