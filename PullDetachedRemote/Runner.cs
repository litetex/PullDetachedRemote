using CoreFrameworkBase.IO;
using CoreFrameworkBase.Tasks;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using PullDetachedRemote.Config;
using PullDetachedRemote.Git;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Text;

namespace PullDetachedRemote
{
   public class Runner
   {
      protected Config.Configuration Config { get; set; }

      protected string UpstreamRemoteName { get; set; }

      public Runner(Config.Configuration configuration)
      {
         Config = configuration;

         Init();
      }

      #region Init

      private void Init()
      {
         // TODO
//#if !DEBUG
         if (string.IsNullOrWhiteSpace(Config.GitHubToken))
            throw new ArgumentException($"{nameof(Config.GitHubToken)}[='****'] is invalid");
         //#endif

         if (string.IsNullOrWhiteSpace(Config.DetachedRepo) && Uri.TryCreate(Config.DetachedRepo, UriKind.Absolute, out _))
            throw new ArgumentException($"{nameof(Config.DetachedRepo)}[='{Config.DetachedRepo}'] is invalid");

         if (string.IsNullOrWhiteSpace(Config.DetachedBranch))
            throw new ArgumentException($"{nameof(Config.DetachedBranch)}[='{Config.DetachedBranch}'] is invalid");

         if (string.IsNullOrWhiteSpace(Config.NameOfOriginUpdateBranch))
            Config.NameOfOriginUpdateBranch = $"{Config.DetachedRepo}-{Config.DetachedBranch}";

         Config.NameOfOriginUpdateBranch = GitBranchNormalizer.Clean(Config.NameOfOriginUpdateBranch);
         if (string.IsNullOrWhiteSpace(Config.NameOfOriginUpdateBranch))
            throw new ArgumentException($"{nameof(Config.NameOfOriginUpdateBranch)}[='{Config.NameOfOriginUpdateBranch}'] is invalid");

         Config.PathToWorkingRepo = Repository.Discover(Config.PathToWorkingRepo);
         if (Config.PathToWorkingRepo == null)
            throw new ArgumentException("No local repository found");

         if (string.IsNullOrWhiteSpace(Config.IdentityEmail))
            Config.IdentityEmail = "actions@github.com";

         if (string.IsNullOrWhiteSpace(Config.IdentityEmail))
            Config.IdentityUsername = $"Github Action - {Assembly.GetEntryAssembly().GetName().Name} {Assembly.GetEntryAssembly().GetName().Version}";
      }

      #endregion Init

      public void Run()
      {
         Log.Info("Starting run");

         DoIt();

         Log.Info("All tasks successfully finished!");

         Log.Info("Finished run");
      }

      protected void DoIt()
      {
         var id = new Identity(Config.IdentityUsername, Config.IdentityEmail);
         Log.Info($"Using Identity: Username='{id.Name}', Email='{id.Email}'");

         var githubCredHandler = 
            new CredentialsHandler(
                (url, usernameFromUrl, types) =>
                    new UsernamePasswordCredentials()
                    {
                       Username = Config.GitHubToken,
                       Password = ""
                    });


         Log.Info($"Will use repo at '{Config.PathToWorkingRepo}'");
         var repo = new Repository(Config.PathToWorkingRepo);

         UpstreamRemoteName = GenerateRemoteUpstreamName(repo.Network.Remotes.Select(x => x.Name));

         repo.Network.Remotes.Add(UpstreamRemoteName, Config.DetachedRepo);
         Log.Info($"Using upstream-remote '{UpstreamRemoteName}'<-'{Config.DetachedRepo}'");

         FetchOptions fetchOptions = new FetchOptions();

         if(Config.DetachedCredsUseGitHub)
         {
            fetchOptions.CredentialsProvider = githubCredHandler;
            Log.Info($"Will fetch upstream-remote with GITHUB_TOKEN");
         }
         else if(string.IsNullOrWhiteSpace(Config.DetachedCredsPrinicipal))
         {
            fetchOptions.CredentialsProvider = new CredentialsHandler(
                (url, usernameFromUrl, types) =>
                    new UsernamePasswordCredentials()
                    {
                       Username = Config.DetachedCredsPrinicipal,
                       Password = Config.DetachedCredsPassword ?? ""
                    });
            Log.Info($"Will fetch upstream-remote with custom credentials");
         }

         Commands.Fetch(repo, UpstreamRemoteName, repo.Network.Remotes[UpstreamRemoteName].FetchRefSpecs.Select(x => x.Specification), fetchOptions, "");
         Log.Info($"Fetched upstream-remote successful");

         var localBranch = GenerateLocalBranchname(repo.Branches.Select(x => x.CanonicalName), Config.NameOfOriginUpdateBranch);
      }

      private string GenerateRemoteUpstreamName(IEnumerable<string> exisitingRemoteNames, string preferedName = "upstream", int maxtries = 1000)
      {
         if (!exisitingRemoteNames.Contains(preferedName))
            return preferedName;

         for (int i = 1; i < maxtries; i++)
            if (!exisitingRemoteNames.Contains($"{preferedName}-{i}"))
               return $"{preferedName}-{i}";

         throw new InvalidOperationException($"Could not generate remote upstream name for {preferedName} within {maxtries} times");
      }

      private string GenerateLocalBranchname(IEnumerable<string> existingBranches, string prefixPath, string namePrefix = "lworking-", int maxtries = 1000)
      {
         for (int i = 0; i < maxtries; i++)
            if (!existingBranches.Contains($"{prefixPath}/{namePrefix}-{i}"))
               return $"{prefixPath}/{namePrefix}-{i}";
         throw new InvalidOperationException($"Could not generate a local branch name for {prefixPath}/{namePrefix}-XXX within {maxtries} times");
      }


   }
}
