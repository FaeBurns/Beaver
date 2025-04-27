cmake_minimum_required(VERSION 3.5.0)

project(gpu_timer)

add_library(gpu_timer SHARED
    gpu_timer.cpp
)

target_include_directories(gpu_timer PRIVATE "C:\\Program Files\\Unity\\Hub\\Editor\\6000.0.21f1\\Editor\\Data\\PluginAPI")

# Detect platform
if(ANDROID)
    find_library(log-lib log)
    find_library(android-lib android)
    find_library(vulkan-lib vulkan)
    target_link_libraries(gpu_timer ${log-lib} ${android-lib} ${vulkan-lib})
else()
    find_package(Vulkan REQUIRED)
    target_link_libraries(gpu_timer ${Vulkan_LIBRARIES})
    target_include_directories(gpu_timer PRIVATE ${Vulkan_INCLUDE_DIR})
endif()
