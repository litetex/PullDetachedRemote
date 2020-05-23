# This will run out of support on 2021-11-05
FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine

COPY PullDetachedRemote PullDetachedRemote

RUN chmod +x PullDetachedRemote

ENTRYPOINT ["./PullDetachedRemote"]