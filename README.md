cmpctircd
=========

About
-----
The aim of this project is to provide a stable, fast, and modern ircd.

Status
-----
Under heavy development. It'll be clear when it is production ready. Feel free to report any bugs on [Jira](https://cmpct.atlassian.net).

Dependencies
-----
* .NET Core 2.2

Running
-----
```
git clone --recurse-submodules https://bitbucket.org/cmpcti/cmpctircd cmpctircd

cd cmpctircd/cmpctircd

# Fetch dependencies
dotnet restore
# Adjust App.config to suit your preferences
dotnet run
```

Tests
-----
```
git clone --recurse-submodules https://bitbucket.org/cmpcti/cmpctircd cmpctircd
cd cmpctircd/cmpctircd
dotnet tests
```

Docker
-----
```
wget https://bitbucket.org/cmpcti/cmpctircd/raw/master/Dockerfile

mkdir cmpctircd-docker
# Place your App.config and so on in cmpctircd-docker

# Your cmpctircd-docker folder will be populated with logs once the IRCd runs
docker build -t cmpctircd .
docker run -v $HOME/cmpctircd-docker:/cmpctircd/ --name cmpctircd -p 6667:6667 -p 6697:6697 -d cmpctircd
```

Contact
-------
An IRC server will be made available once the daemon is sufficiently mature (soon, we hope)!
