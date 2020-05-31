# NOTE (2020-05-29 litetex): https://github.com/libgit2/libgit2sharp/issues/1798
FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine

LABEL "repository"="https://github.com/litetex/PullDetachedRemote"

WORKDIR /app

RUN addgroup -S app && adduser -S -G app app 

RUN chown -R app:app /app

USER app

COPY PullDetachedRemote PullDetachedRemote

USER root

RUN chmod +x PullDetachedRemote

# Execute at any cost deevaluated
USER app

ENTRYPOINT ["/app/PullDetachedRemote"]
