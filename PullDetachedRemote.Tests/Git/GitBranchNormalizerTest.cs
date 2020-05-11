using PullDetachedRemote.Git;
using System;
using System.Collections.Generic;
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
      
      public void ShouldBeUnmodified(string value)
      {
         Assert.True(GitBranchNormalizer.IsValid(value));

         var result = GitBranchNormalizer.Fix(value);

         Assert.Equal(value, result);
      }

      // https://mirrors.edge.kernel.org/pub/software/scm/git/docs/git-check-ref-format.html
      [Theory]
      /*
       * 1. They can include slash / for hierarchical (directory) grouping, 
       * but no slash-separated component can begin with a dot . or end with the sequence .lock. 
       */
      [InlineData("./bad","bad")]
      [InlineData("a.lock/.lock", "a")]
      [InlineData(".lock/.lock", "lock")]
      [InlineData(".lock/a.lock", "lock/a")]
      [InlineData("bad.lock", "bad")]
      [InlineData("...lock", null)]
      [InlineData("...lock.", "lock")]
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
      [InlineData("...b", "b")]
      [InlineData("../../.b", "b")]

      [InlineData("a~", "a")]
      [InlineData("a~b", "ab")]
      [InlineData("~b", "b")]
      [InlineData("a:", "a")]
      [InlineData("a:b", "ab")]
      [InlineData(":b", "b")]

      // TODO: More!
      public void ShouldBeFixed(string malformed, string expected)
      {
         Assert.False(GitBranchNormalizer.IsValid(malformed));

         try
         {
            var result = GitBranchNormalizer.Fix(malformed);

            Assert.Equal(expected, result);
         }
         catch(ArgumentException ex)
         {
            if (expected != null || ex.Message != "Value is invalid")
               throw ex;
         }
      }
   }
}
