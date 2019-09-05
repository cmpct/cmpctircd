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

# Add config file
COPY App.config /app/cmpctircd/cmpctircd/cmpctircd.dll.config
COPY server.pfx /app/cmpctircd/cmpctircd/server.pfx

# Run
ENTRYPOINT [ "dotnet", "cmpctircd.dll" ]
