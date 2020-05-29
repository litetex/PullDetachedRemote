# NOTE (2020-05-29 litetex): https://github.com/libgit2/libgit2sharp/issues/1798
FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine

COPY PullDetachedRemote PullDetachedRemote

RUN chmod +x PullDetachedRemote

ENTRYPOINT ["./PullDetachedRemote"]
