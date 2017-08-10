## config
TEST_FILES="ChannelInOutTest.cs"
# path to dependencies
NUNIR_PATH="bin/net-4.5/nunitlite-runner.exe"
NUNIT_PATH="bin/net-4.5/nunit.framework.dll"
SMART_PATH="bin/Meebey.SmartIrc4net.dll"
# where to create test assembly
TEST_PATH="Tests.dll"
## end config

# build
csc /out:${TEST_PATH} -t:library -r:${NUNIT_PATH} -r:${SMART_PATH} ${TEST_FILES}
# run
mono bin/net-4.5/nunitlite-runner.exe Tests.dll
