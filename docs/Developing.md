## Developing
### Tools for developing
* [Visual Studio 2019](https://visualstudio.microsoft.com/de/vs/)
* [SonarLint VS](https://www.sonarlint.org/visualstudio/) (optional)
* [Docker](https://docs.docker.com/engine/install/) (only if you wan't to test the docker image)

### How to develop
* Checkout the repo
* Open it with Visual Studio
* Build it 
* Optional: Build the Dockerfile with [``docker build``](https://docs.docker.com/engine/reference/commandline/build/)

### How to test
Sample files for testing:

<details>
  <summary>launchSettings.json</summary>
  <p>
  
  * located under ``PullDetachedRemote/Properties``
  
  ```JSON
  {
    "profiles": {
      "PDR - GenConf": {
        "commandName": "Project",
        "commandLineArgs": "--genconf config.yml"
      },
      "PDR - Conf": {
        "commandName": "Project",
        "commandLineArgs": "--config config.yml --GITHUB_PAT=xxx"
      },
      "PDR - CMD": {
        "commandName": "Project",
        "commandLineArgs": "--identitymail=test@test.test --identityuser=\"Test Test\" --prlabels \"upstream\" --clonemode=CLONE_ALWAYS --originrepo=https://github.com/<owner>/forked --originbranch=an-update --upstreamrepo=https://github.com/<owner>/fork-base --upstreambranch=master --GITHUB_PAT=xxx"
      }
    }
  }
  ```
  </p>
</details>

<details>
  <summary>config.yml</summary>
  <p>
  
  ```YML
  IdentityEmail: test@test.test
  IdentityUsername: Test Test
  PRMetaInfo:
    Assignees: []
    Reviewers: []
    Labels: ['upstream']
  PathToWorkingRepo: test
  CloneMode: CLONE_ALWAYS
  OriginRepo: https://github.com/<owner>/forked
  OriginBranch: an-update
  UpstreamRepo: https://github.com/<owner>/fork-base
  UpstreamBranch: master
  OriginUpdateBranch: 
  UpstreamCredMode: AUTO
  ```
  </p>
</details>
