# Example usage:
# - Please `mkdir cmpctircd-docker` and place your App.config, server.pfx, etc. files there.
# - Adjust the path in docker run -v accordingly to the cmpctircd-docker folder
# - Run: docker build -t cmpctircd .
# - Run: docker run -v $HOME/cmpctircd-docker:/cmpctircd/ --name cmpctircd -p 6667:6667 -p 6697:6697 -d cmpctircd

# Set up the base
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /app/

# Set up the project
RUN apt-get -y install git
RUN git clone --recurse-submodules https://bitbucket.org/cmpcti/cmpctircd cmpctircd

# Change directory
WORKDIR /app/cmpctircd/

# Build
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS run
WORKDIR /app/cmpctircd/cmpctircd
COPY --from=build /app/cmpctircd/cmpctircd/out .

# Default listening ports
# You will need to expose them manually with docker run -p 6667:6667 -p 6697:6697 ...
# (https://docs.docker.com/engine/reference/builder/#expose)
EXPOSE 6667
EXPOSE 6697

# Set up the volume
VOLUME /cmpctircd/

# Copy the config over by symlinks
RUN ln -fs /cmpctircd/App.config /app/cmpctircd/cmpctircd/cmpctircd.dll.config
RUN ln -fs /cmpctircd/ircd.motd  /app/cmpctircd/cmpctircd/ircd.motd
RUN ln -fs /cmpctircd/ircd.rules /app/cmpctircd/cmpctircd/ircd.rules
RUN ln -fs /cmpctircd/server.pfx /app/cmpctircd/cmpctircd/server.pfx

# Expose the log files (may need to adjust config)
RUN ln -s /cmpctircd/ircd.log   /app/cmpctircd/cmpctircd/ircd.log

# Run
ENTRYPOINT ["dotnet", "cmpctircd.dll"]
