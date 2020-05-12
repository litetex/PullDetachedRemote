using PullDetachedRemote.Git;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace PullDetachedRemote.Tests.Git
{
   public class GitBranchNormalizerTest
   {
      [Theory]
      [InlineData("a")]
      [InlineData("ab")]
      [InlineData("feature/abc")]
      [InlineData("feature/abc/def")]
      [InlineData("feature/abc/def/hjashd")]
      [InlineData("this-is-a-test")]

      [InlineData("@a")]
      public void ShouldBeUnmodified(string value)
      {
         Assert.True(GitBranchNormalizer.IsValid(value));

         var result = GitBranchNormalizer.Fix(value);

         Assert.Equal(value, result);
      }

      // https://mirrors.edge.kernel.org/pub/software/scm/git/docs/git-check-ref-format.html
      [Theory]

      [InlineData(null,null)]
      [InlineData("",null)]
      [InlineData(" ", null)]
      [InlineData("     ", null)]
      /*
       * 1. They can include slash / for hierarchical (directory) grouping, 
       * but no slash-separated component can begin with a dot . or end with the sequence .lock. 
       */
      [InlineData("./bad", "bad")]
      [InlineData("a.lock/.lock", "a.lock")]
      [InlineData(".lock/.lock", "lock")]
      [InlineData(".lock/a.lock", "lock/a")]
      [InlineData("bad/.lock", "bad")]
      [InlineData("...lock", ".lock")] // 3
      [InlineData("..lock", "lock")] // 3
      [InlineData("./.lock", null)]
      [InlineData("./.lock.", ".lock")] 
      /*
       * 2. They must contain at least one /. This enforces the presence of a category like heads/, tags/ etc. but the actual names are not restricted. 
       * If the --allow-onelevel option is used, this rule is waived. 
       */
      // Skip
      /*
       * 3. They cannot have two consecutive dots .. anywhere. 
       */
      [InlineData("..", null)]
      [InlineData("a..", "a")]
      [InlineData("..b", "b")]
      [InlineData("...b", ".b")]
      [InlineData("../../.b", ".b")]


      /*
       * 6. They cannot begin or end with a slash / or contain multiple consecutive slashes 
       * (see the --normalize option below for an exception to this rule) 
       */
      [InlineData("/", null)]
      [InlineData("//", null)]
      [InlineData("//////", null)]
      [InlineData("/b", "b")]
      [InlineData("//b", "b")]
      [InlineData("////b", "b")]
      [InlineData("a/", "a")]
      [InlineData("b//", "b")]
      [InlineData("b////", "b")]
      [InlineData("/b/", "b")]
      [InlineData("//b//", "b")]
      [InlineData("///b///", "b")]
      [InlineData("///abcdef-ghjk/abc///", "abcdef-ghjk/abc")]

      /*
       * 7. They cannot end with a dot . 
       */
      [InlineData(".", null)]
      [InlineData("a.", "a")]
      [InlineData("ab.", "ab")]
      [InlineData("ab./", "ab")]
      [InlineData("a./", "a")]
      [InlineData("....", null)]
      [InlineData(".....", null)]

#pragma warning disable S125 // Sections of code should not be commented out
      /*
       * 8. They cannot contain a sequence @{ 
       */
#pragma warning restore S125 // Sections of code should not be commented out
      [InlineData("@{", null)]
      [InlineData("/@{.", null)]
      [InlineData("abc@{", "abc")]

      /*
       * 9. They cannot be the single character @
       */
      [InlineData("@", null)]
      [InlineData("..@...", null)]
      [InlineData("..@.", null)]
      [InlineData("..@a", "@a")]
      public void ShouldBeFixed(string malformed, string expected)
      {
         Assert.False(GitBranchNormalizer.IsValid(malformed));

         try
         {
            var result = GitBranchNormalizer.Fix(malformed);

            Assert.Equal(expected, result);
         }
         catch (ArgumentException ex)
         {
            if (expected != null || ex.Message != "Value is invalid")
               throw ex;
         }
      }

      /*
       * 4. They cannot have ASCII control characters (i.e. bytes whose values are lower than \040 [-> 8x4 = 32], or \177 [-> 64x1 + 8x7 + 7 = 127] DEL),
       * space, tilde ~, caret ^, or colon : anywhere. 
       * 
       * 5. They cannot have question-mark ?, asterisk *, or open bracket [ anywhere. 
       * See the --refspec-pattern option below for an exception to this rule. 
       * 
       * 10. They cannot contain a \
       */
      [Theory]
      [InlineData("x",null)]
      [InlineData("xx", null)]
      [InlineData("x/x", "/")]
      [InlineData("xa", "a")]
      [InlineData("ax", "a")]
      [InlineData("axb", "ab")]
      [InlineData("axax", "aa")]
      [InlineData("xax", "a")]
      public void InvalidCharShouldBe(string format, string expected)
      {
         var invalidChars = new List<int>(new int[] {
            127, // ASCII CTRL 127 = DEL
            ' ',
            '~',
            '^',
            ':',
            '?',
            '*',
            '[',
            '\\' // 10
         });
         invalidChars.AddRange(Enumerable.Range(0, 32)); // ASCII CTRL < 32

         foreach (char invalidChar in invalidChars.OfType<char>())
         {
            var malformed = format.Replace('x', invalidChar);

            ShouldBeFixed(malformed, expected);
         }
      }
   }
}
