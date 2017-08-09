# build
csc -t:library -r:bin/net-4.5/nunit.framework.dll -r:bin/Meebey.SmartIrc4net.dll ChannelInOutTest.cs
# run
mono bin/net-4.5/nunitlite-runner.exe Tests.dll
