[![Build](https://img.shields.io/github/workflow/status/litetex/PullDetachedRemote/Master%20CI)](https://github.com/litetex/PullDetachedRemote/actions?query=workflow%3A%22Master+CI%22)
[![Build Sonar](https://dev.azure.com/litetex/PullDetachedRemote/_apis/build/status/master?label=build%20sonar)](https://dev.azure.com/litetex/PullDetachedRemote/_build/latest?definitionId=8)
[![Latest stable docker version](https://img.shields.io/badge/docker-latest-%232684ff)](https://hub.docker.com/r/litetex/pulldetachedremote/tags?name=latest)

[![Build develop](https://img.shields.io/github/workflow/status/litetex/PullDetachedRemote/Develop%20CI?label=build%20develop)](https://github.com/litetex/PullDetachedRemote/actions?query=workflow%3A%22Develop+CI%22)
[![Build develop Sonar](https://dev.azure.com/litetex/PullDetachedRemote/_apis/build/status/develop?label=build%20develop%20sonar)](https://dev.azure.com/litetex/PullDetachedRemote/_build/latest?definitionId=7)
[![Develop docker version](https://img.shields.io/badge/docker-develop-%232684ff)](https://hub.docker.com/r/litetex/pulldetachedremote/tags?name=develop&page=1)

# PullDetachedRemote
Creates a detached upstream branch and a corresponding PR from another repo and updates it automatically

Docker-Image for [pull-detached-remote](https://github.com/litetex/pull-detached-remote)

## Usage
### Inputs
→ https://github.com/litetex/pull-detached-remote#inputs
#### Arguments
:point_right: see also [action.yml](https://github.com/litetex/pull-detached-remote/blob/develop/action.yml) in the GitHub Action

:point_right: ``--help`` 

Arguments can be looked up in  [CmdOption](PullDetachedRemote/CMD/CmdOption.cs)

#### Enviroment Variables
→ https://github.com/litetex/pull-detached-remote#environment-variables

### 1. Run it over commandline (only)
The program is run with commandline parameters only.

No disk or similar required.

Sample run:
```BASH
PullDetachedRemote.exe --identitymail=test@test.test --identityuser=\"Test Test\" --prlabels \"upstream\" --clonemode=CLONE_ALWAYS --originrepo=https://github.com/<owner>/forked --originbranch=an-update --upstreamrepo=https://github.com/<owner>/fork-base --upstreambranch=master --GITHUB_PAT=xxx
```


#### Run it as standalone with docker (example)
```BASH
docker run litetex/pulldetachedremote:develop --identitymail=test@test.test --identityuser=\"Test Test\" --prlabels \"upstream\" --clonemode=CLONE_ALWAYS --originrepo=https://github.com/<owner>/forked --originbranch=an-update --upstreamrepo=https://github.com/<owner>/fork-base --upstreambranch=master --GITHUB_PAT=xxx
```

### 2. Run it over YML-config file
The program can also be mainly run with a config file.

Note: 

Sample run:
```BASH
PullDetachedRemote.exe --config config.yml --GITHUB_PAT=xxx
```
#### Generate a sample config file
You can also generate a sample configuration file with:
```BASH
PullDetachedRemote.exe --genconf config.yml
```
### 3. Run it as hybrid :twisted_rightwards_arrows:
You can also mix
- config file
- parameters
- environment variables

The order how they are set can be found in [StartUp.cs](PullDetachedRemote/StartUp.cs)

## Build
Wan't to build the project by yourself?

Checkout, how it is build in the [automated build plans](.github/workflows/)

## Develop
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
My sample files for testing:

launchSettings.json
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

config.yml
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
