FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-focal

RUN apt-get update \
  && apt-get upgrade \
  && apt-get install git  

COPY PullDetachedRemote PullDetachedRemote

RUN chmod +x PullDetachedRemote

ENTRYPOINT ["./PullDetachedRemote"]
