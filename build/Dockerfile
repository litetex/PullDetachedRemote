# NOTE (2020-05-29 litetex): https://github.com/libgit2/libgit2sharp/issues/1798
# NOTE (2020-05-31 litetex): https://help.github.com/en/actions/reference/virtual-environments-for-github-hosted-runners#docker-container-filesystem
# /github/workspace - Note: GitHub Actions must be run by the default Docker user (root). Ensure your Dockerfile does not set the USER instruction, otherwise you will not be able to access GITHUB_WORKSPACE.

FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine

LABEL repository="https://github.com/litetex/PullDetachedRemote"
LABEL maintainer=litetex

# Work in nested folder, not in the root folder / !
WORKDIR /app

# Copy the app-file
COPY PullDetachedRemote PullDetachedRemote

# Make the app-file executable
RUN chmod +x PullDetachedRemote

ENTRYPOINT ["/app/PullDetachedRemote"]
