# NOTE (2020-05-29 litetex): https://github.com/libgit2/libgit2sharp/issues/1798
FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-alpine

LABEL "repository"="https://github.com/litetex/PullDetachedRemote"

# Work in nested folder, not in the root folder / !
WORKDIR /app

# https://github.com/mhart/alpine-node/issues/48
# Create non-root user
RUN addgroup -S app && adduser -S -G app app 

# Assign app-folder to non-root user
RUN chown -R app:app /app

# Copy the app-file (as non-root)
USER app
COPY PullDetachedRemote PullDetachedRemote

# Make the app-file executable
USER root
RUN chmod +x PullDetachedRemote

# When executing: At any cost as non-root
USER app
ENTRYPOINT ["/app/PullDetachedRemote"]
