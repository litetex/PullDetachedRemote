using CoreFrameworkBase.Config;
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

   public class YmlConfig<C> : FileBasedConfig<C> where C : YmlConfigConfigurator
   {
      [YamlIgnore]
      public override C Config { get; set; }

      public override void PopulateFrom(string filecontent)
      {
         Type t = this.GetType();

         var obj = new DeserializerBuilder()
                  .Build()
                  .Deserialize(new StringReader(filecontent), t);

         PropertyCopier.Copy(obj, this);
      }

      public override string SerializeToFileContent()
      {
         return 
            new SerializerBuilder()
               .Build()
               .Serialize(
                 this);
      }
   }

   public class YmlConfigConfigurator : FileBasedConfigConfigurator
   {
      public const string DEFAULT_SAVEPATH = "config.yml";

      /// <summary>
      /// The Path where the file is saved; by default <see cref="DEFAULT_SAVEPATH"/> 
      /// </summary>
      /// <remarks>You shouldn't change it at runtime!</remarks>
      public override string SavePath { get; set; } = DEFAULT_SAVEPATH;
   }

   class PropertyCopier
   {
      public static void Copy(object parent, object child)
      {
         var parentProperties = parent.GetType().GetProperties();
         var childProperties = child.GetType().GetProperties();

         foreach (var parentProperty in parentProperties)
         {
            foreach (var childProperty in childProperties)
            {
               if (parentProperty.Name == childProperty.Name && parentProperty.PropertyType == childProperty.PropertyType)
               {
                  childProperty.SetValue(child, parentProperty.GetValue(parent));
                  break;
               }
            }
         }
      }
   }
}
