using CoreFrameworkBase.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;
using static PullDetachedRemote.Config.YmlConfig;

namespace PullDetachedRemote.Config
{
   public class YmlConfig : YmlConfig<YmlConfigConfigurator>
   {
      public YmlConfig()
      {
         Config = new YmlConfigConfigurator();
      }
   }

   public class YmlConfig<C> : JsonConfig<C> where C : YmlConfigConfigurator
   {
      public override void PopulateFrom(string filecontent)
      {
         base.PopulateFrom(
            new SerializerBuilder()
               .JsonCompatible()
               .Build()
               .Serialize(new DeserializerBuilder()
                  .Build()
                  .Deserialize(new StringReader(filecontent))));
      }

      public override string SerializeToFileContent()
      {
         return 
            new SerializerBuilder()
               .JsonCompatible()
               .Build()
               .Serialize(
                  JsonConvert.
                  DeserializeObject(
                     base.SerializeToFileContent()));
      }

   }
   public class YmlConfigConfigurator : JsonConfigConfigurator
   {
      public new const string DEFAULT_SAVEPATH = "config.yml";

      /// <summary>
      /// The Path where the file is saved; by default <see cref="DEFAULT_SAVEPATH"/> 
      /// </summary>
      /// <remarks>You shouldn't change it at runtime!</remarks>
      public override string SavePath { get; set; } = DEFAULT_SAVEPATH;
   }
}
