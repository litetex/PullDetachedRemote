## Usage
### Inputs
→ https://github.com/litetex/pull-detached-remote#inputs

#### Arguments
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
