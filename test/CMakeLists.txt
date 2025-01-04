cmake_minimum_required(VERSION 3.10)
project(test_server)

find_package(wxWidgets COMPONENTS core base net REQUIRED)
include(${wxWidgets_USE_FILE})

add_executable(test_server TestServer.cpp ../src/Protocol.cpp)

target_include_directories(test_server PRIVATE 
    ${CMAKE_SOURCE_DIR}/include
    ${wxWidgets_INCLUDE_DIRS}
)

target_link_libraries(test_server PRIVATE 
    ${wxWidgets_LIBRARIES}
)
