using PullDetachedRemote.Config;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;

namespace PullDetachedRemote
{
   public class StatusReport
   {
      public DateTime LastUpdateStartTimeUtc { get; set; } = DateTime.UtcNow;

      public List<string> Messages { get; set; } = new List<string>();

      public bool Error { get; set; } = false;

      public bool PRBaseNotBeforeTarget { get; set; } = false;

      public bool CreatedBranch { get; set; } = false;

      public bool HasUpstreamUpdates { get; set; } = false;

      public bool Pushed { get; set; } = false;

      public bool CreatedPR { get; set; } = false;

      public bool UpdatedPRSuccessfully { get; set; } = false;

      public Configuration ResolvedConfig { get; set; }

      public override string ToString()
      {
         return new SerializerBuilder().Build().Serialize(this);
      }
   }
}
