cmake_minimum_required(VERSION 3.10)
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

# Cross-compilation setup
set(CMAKE_SYSTEM_NAME Windows)
set(CMAKE_C_COMPILER x86_64-w64-mingw32-gcc)
set(CMAKE_CXX_COMPILER x86_64-w64-mingw32-g++)
set(CMAKE_FIND_ROOT_PATH /usr/x86_64-w64-mingw32)

project(caprev)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# wxWidgets configuration
set(wxWidgets_ROOT_DIR /usr/x86_64-w64-mingw32)
set(wxWidgets_CONFIGURATION mswu)
set(wxWidgets_USE_STATIC ON)
set(wxWidgets_USE_UNICODE ON)

find_package(wxWidgets COMPONENTS core base REQUIRED)
include(${wxWidgets_USE_FILE})

add_executable(caprev WIN32
    src/main.cpp
    src/MainFrame.cpp
    src/Sensor.cpp
)

target_include_directories(caprev PRIVATE include)
target_link_libraries(caprev ${wxWidgets_LIBRARIES})
