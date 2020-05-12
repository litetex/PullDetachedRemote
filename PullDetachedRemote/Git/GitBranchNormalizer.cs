using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;

namespace PullDetachedRemote.Git
{
   /// <summary>
   /// Normalizes a value to a valid git Branch
   /// </summary>
   /// <seealso cref="https://github.com/TheSavior/clean-git-ref/blob/bf3c8afc8032bcdc6a53634c18b601dc6df478fb/src/index.js"/>
   /// <seealso cref="https://mirrors.edge.kernel.org/pub/software/scm/git/docs/git-check-ref-format.html"/>
   public static class GitBranchNormalizer
   {
      public static bool IsValid(string value)
      {
         if (string.IsNullOrWhiteSpace(value))
            return false;

         return !InvalidaitionReason.ALL.Any(x => x.Validator(value));
      }

      public static string Fix(string value)
      {
         if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is invalid");

         bool wasInvalid;
         do
         {
            wasInvalid = false;

            foreach (var invReason in InvalidaitionReason.ALL)
            {
               if (invReason.Validator(value))
               {
                  wasInvalid = true;

                  var newValue = invReason.Fixer(value);
                  if (value.Equals(newValue))
                     throw new ArgumentException($"Fixing the value '{value}' failed: new value is '{newValue}' - same!");

                  value = newValue;

                  if (string.IsNullOrEmpty(value))
                     throw new ArgumentException("Value is invalid");
               }
            }
         } while (wasInvalid);

         return value;
      }

      public class InvalidaitionReason
      {
         public static InvalidaitionReason[] ALL
         {
            get => new InvalidaitionReason[]
{
               DOT_AT_START,
               LOCK_AT_END,
               DOUBLE_CONSECUTIVE_DOT,
               INVALID_CHAR,
               BEGIN_SLASH,
               END_SLASH,
               END_DOT,
               AT_LEFT_CURLY_BRACKET,
               AT
            };
         }

         /*
          * 1. They can include slash / for hierarchical (directory) grouping, 
          * but no slash-separated component can begin with a dot . 
          * or end with the sequence .lock. 
          */
         public static readonly InvalidaitionReason DOT_AT_START = new InvalidaitionReason(
            str => str.Contains('/') && str.StartsWith('.'),
            str => str.Remove(0, 1)
         );
         public static readonly InvalidaitionReason LOCK_AT_END = new InvalidaitionReason(
            str => str.Contains('/') && str.EndsWith(".lock"),
            str => str.Remove(str.Length - 5)
         );

         /*
          * 2. They must contain at least one /. This enforces the presence of a category like heads/, tags/ etc. but the actual names are not restricted. 
          * If the --allow-onelevel option is used, this rule is waived. 
          */
         //Skipped

         /*
          * 3. They cannot have two consecutive dots .. anywhere. 
          */
         public static readonly InvalidaitionReason DOUBLE_CONSECUTIVE_DOT = new InvalidaitionReason(
            str => str.Contains(".."),
            str => str.Replace("..", null)
         );


         /*
          * 4. They cannot have ASCII control characters (i.e. bytes whose values are lower than \040 [-> 8x4 = 32], or \177 [-> 64x1 + 8x7 + 7 = 127] DEL),
          * space, tilde ~, caret ^, or colon : anywhere. 
          * 
          * 5. They cannot have question-mark ?, asterisk *, or open bracket [ anywhere. 
          * See the --refspec-pattern option below for an exception to this rule. 
          * 
          * 10. They cannot contain a \
          */

         private static readonly int[] INVALID_CHARS = new int[] { 127, ' ', '~', '^', ':', '?', '*', '[', '\\' };

         public static readonly InvalidaitionReason INVALID_CHAR = new InvalidaitionReason(
            str => str.FirstOrDefault(c => c < 32 || INVALID_CHARS.Contains(c)) != default,
            str => string.Concat(str.Where(c => c >= 32 && !INVALID_CHARS.Contains(c)))
         );

         /*
          * 6. They cannot begin or end with a slash / or contain multiple consecutive slashes 
          * (see the --normalize option below for an exception to this rule) 
          */
         public static readonly InvalidaitionReason BEGIN_SLASH = new InvalidaitionReason(
            str => str.StartsWith('/'),
            str => str.Remove(0, 1)
         );
         public static readonly InvalidaitionReason END_SLASH = new InvalidaitionReason(
            str => str.EndsWith('/'),
            str => str.Remove(str.Length - 1)
         );

         /*
          * 7. They cannot end with a dot . 
          */
         public static readonly InvalidaitionReason END_DOT = new InvalidaitionReason(
            str => str.EndsWith('.'),
            str => str.Remove(str.Length - 1)
         );


#pragma warning disable S125 // Sections of code should not be commented out
         /*
          * 8. They cannot contain a sequence @{ 
          */
#pragma warning restore S125 // Sections of code should not be commented out
         public static readonly InvalidaitionReason AT_LEFT_CURLY_BRACKET = new InvalidaitionReason(
           str => str.Contains("@{"),
           str => str.Replace("@{", null)
         );

         /*
          * 9. They cannot be the single character @
          */
         public static readonly InvalidaitionReason AT = new InvalidaitionReason(
           str => "@".Equals(str),
           str => ""
         );

         public Func<string, bool> Validator { get; protected set; }

         public Func<string, string> Fixer { get; protected set; }

         protected InvalidaitionReason(Func<string, bool> validator, Func<string, string> fixer)
         {
            Validator = validator;
            Fixer = fixer;
         }
      };
   }
}
