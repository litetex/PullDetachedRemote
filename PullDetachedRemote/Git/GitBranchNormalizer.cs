using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PullDetachedRemote.Git
{
   //TODO DOES NOT WORK!!!
   /// <summary>
   /// Normalizes a value to a valid git Branch
   /// </summary>
   /// <seealso cref="https://github.com/TheSavior/clean-git-ref/blob/bf3c8afc8032bcdc6a53634c18b601dc6df478fb/src/index.js"/>
   public static class GitBranchNormalizer
   {
      private static string ReplaceAll(string str, string search, string replacement)
      {
         return str.Replace(search, replacement);
      }

      private static string ReplaceAll(string str, Regex search, string replacement)
      {
         return search.Replace(str, replacement);
      }

      public static string Clean(string value)
      {
         value = ReplaceAll(value, "./", "/");
         value = ReplaceAll(value, "..", ".");
         value = ReplaceAll(value, " ", "-");
         value = ReplaceAll(value, new Regex(@"^[~^:?*\\\-]"), "");
         value = ReplaceAll(value, new Regex(@"[~^:?*\\]"), "-");
         value = ReplaceAll(value, new Regex(@"[~^:?*\\\-]$"), "");
         value = ReplaceAll(value, "@{", "-");
         value = ReplaceAll(value, new Regex(@"\.$"), "");
         value = ReplaceAll(value, new Regex(@"\/$"), "");
         value = ReplaceAll(value, new Regex(@"[\/]{2,}"), "/");
         value = ReplaceAll(value, new Regex(@"\.lock$"), "");
         return value;
      }
   }
}
