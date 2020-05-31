# NOTE (2020-05-29 litetex): https://github.com/libgit2/libgit2sharp/issues/1798
FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine

LABEL "repository"="https://github.com/litetex/PullDetachedRemote"

WORKDIR /app

RUN chown -R 1001:1001 /app

USER 1001

COPY PullDetachedRemote PullDetachedRemote

RUN chmod +x PullDetachedRemote

ENTRYPOINT ["/app/PullDetachedRemote"]
