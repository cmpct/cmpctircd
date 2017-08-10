## config
TEST_FILES="ChannelInOutTest.cs"
# path to dependencies
IRCD_PATH="../bin/Debug/cmpctircd.exe"
NUNIR_PATH="bin/net-4.5/nunitlite-runner.exe"
NUNIT_PATH="bin/net-4.5/nunit.framework.dll"
SMART_PATH="bin/Meebey.SmartIrc4net.dll"
# where to create test assembly
TEST_PATH="Tests.dll"
## end config

# build
csc /out:${TEST_PATH} -t:library -r:${NUNIT_PATH} -r:${SMART_PATH} ${TEST_FILES}
# run
mono ${IRCD_PATH} & export IRCD_PID=$!
mono ${NUNIR_PATH} ${TEST_PATH}
# finish up
kill ${IRCD_PID}
