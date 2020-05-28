FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-focal

COPY * *

RUN chmod +x PullDetachedRemote

ENTRYPOINT ["./PullDetachedRemote"]
