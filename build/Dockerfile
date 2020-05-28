FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-focal

RUN apt-get update \
  && apt-get upgrade -y \
  && apt-get install git -y

COPY PullDetachedRemote PullDetachedRemote

RUN chmod +x PullDetachedRemote

ENTRYPOINT ["./PullDetachedRemote"]
