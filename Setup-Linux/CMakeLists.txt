cmake_minimum_required(VERSION 3.8)

project(ArteranosPackage)

set(CPACK_GENERATOR "DEB")

set(CMAKE_PROJECT_HOMEPAGE_URL "https://github.com/willneedit/Arteranos")

set(CPACK_PACKAGE_NAME "arteranos-server")
set(CPACK_PACKAGE_VENDOR "willneedit")
set(CPACK_PACKAGE_CONTACT "willneedit")
set(CPACK_PACKAGE_DESCRIPTION "This is the Arteranos Dedicated Server, suited for running on a headless Linux box.")
set(CPACK_PACKAGE_DESCRIPTION_SUMMARY "Arteranos Dedicated Server")

set(CPACK_DEBIAN_PACKAGE_SECTION "games")
set(CPACK_DEBIAN_PACKAGE_CONTROL_EXTRA ${CMAKE_BINARY_DIR}/postinst;${CMAKE_BINARY_DIR}/prerm)

set(CPACK_PACKAGING_INSTALL_PREFIX "/")

include(CPack)
include(GNUInstallDirs)

message(STATUS "Source dir   : ${CMAKE_SOURCE_DIR}")
message(STATUS "Binary dir   : ${CMAKE_BINARY_DIR}")
message(STATUS "OS           : ${CMAKE_SYSTEM_NAME}")
message(STATUS "Architecture : ${CMAKE_SYSTEM_PROCESSOR}")
message(STATUS "Version      : ${CPACK_PACKAGE_VERSION}")

if(NOT ${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
	message(SEND_ERROR "Architecture other than Linux")
endif()


set(ArtifactsDir ${CMAKE_SOURCE_DIR}/../build/Linux64-Server)
set(TargetDirName arteranos-${CPACK_PACKAGE_VERSION})

set(TargetLibexecDir	usr/${CMAKE_INSTALL_LIBEXECDIR}/${TargetDirName})
set(TargetBinDir		usr/${CMAKE_INSTALL_BINDIR})
set(TargetInitdDir		${CMAKE_INSTALL_SYSCONFDIR}/init.d)

set(MainExe Arteranos-Server)
set(MainExePath /${TargetLibexecDir}/${MainExe})

file(MAKE_DIRECTORY ${CMAKE_BINARY_DIR}/tgt/${TargetBinDir})
configure_file(
	Arteranos-Server-Start.in
	${CMAKE_BINARY_DIR}/tgt/${TargetBinDir}/${MainExe}-Start
	FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
	@ONLY
	NEWLINE_STYLE LF
)

file(MAKE_DIRECTORY ${CMAKE_BINARY_DIR}/tgt/${TargetInitdDir})
configure_file(
	init_d_arteranos.in
	${CMAKE_BINARY_DIR}/tgt/${TargetInitdDir}/arteranos
	FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
	@ONLY
	NEWLINE_STYLE LF
)

configure_file(
	postinst.in
	${CMAKE_BINARY_DIR}/postinst
	FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
	@ONLY
	NEWLINE_STYLE LF
)

configure_file(
	prerm.in
	${CMAKE_BINARY_DIR}/prerm
	FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
	@ONLY
	NEWLINE_STYLE LF
)

install(PROGRAMS 
	${ArtifactsDir}/${MainExe}
	${ArtifactsDir}/ipfs
	${ArtifactsDir}/UnityPlayer.so
	DESTINATION ${TargetLibexecDir}
)

install(DIRECTORY
	${ArtifactsDir}/${MainExe}_Data
	DESTINATION ${TargetLibexecDir}
)

install(DIRECTORY
	${CMAKE_BINARY_DIR}/tgt/${TargetBinDir}
	FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
	DESTINATION usr
)

install(DIRECTORY
	${CMAKE_BINARY_DIR}/tgt/${CMAKE_INSTALL_SYSCONFDIR}
	FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE GROUP_READ GROUP_EXECUTE WORLD_READ WORLD_EXECUTE
	DESTINATION .
)
