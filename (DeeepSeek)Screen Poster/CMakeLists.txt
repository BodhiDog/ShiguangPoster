cmake_minimum_required(VERSION 3.16)
project(ScreenPoster LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

set(CMAKE_AUTOMOC ON)

find_package(Qt6 QUIET COMPONENTS Widgets)
if(Qt6_FOUND)
    set(QT_PKG Qt6)
else()
    find_package(Qt5 REQUIRED COMPONENTS Widgets)
    set(QT_PKG Qt5)
endif()

add_executable(ScreenPoster WIN32
    src/main.cpp
    src/util.cpp
    src/config.cpp
    src/csv.cpp
    src/gallery.cpp
    src/matcher.cpp
    src/sentences.cpp
    src/cloud.cpp
    src/idle.cpp
    src/screensaver.cpp
    src/settings.cpp
    src/app.cpp
)

target_link_libraries(ScreenPoster PRIVATE ${QT_PKG}::Widgets)
target_include_directories(ScreenPoster PRIVATE src)

# Windows GUI entry point (Qt provides WinMain)
if(WIN32)
    if(Qt6_FOUND AND TARGET Qt6::EntryPoint)
        target_link_libraries(ScreenPoster PRIVATE Qt6::EntryPoint)
    elseif(TARGET Qt5::WinMain)
        target_link_libraries(ScreenPoster PRIVATE Qt5::WinMain)
    endif()
endif()

if(MSVC)
    add_compile_options(/utf-8)
endif()

add_custom_command(TARGET ScreenPoster POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_directory "${CMAKE_SOURCE_DIR}/pictures" "$<TARGET_FILE_DIR:ScreenPoster>/pictures"
    COMMAND ${CMAKE_COMMAND} -E copy_directory "${CMAKE_SOURCE_DIR}/resources" "$<TARGET_FILE_DIR:ScreenPoster>/resources"
    COMMAND ${CMAKE_COMMAND} -E make_directory "$<TARGET_FILE_DIR:ScreenPoster>/config"
    COMMAND ${CMAKE_COMMAND} -E copy_if_different "${CMAKE_SOURCE_DIR}/config/config.ini" "$<TARGET_FILE_DIR:ScreenPoster>/config/config.ini"
    COMMAND ${CMAKE_COMMAND} -E copy_if_different "${CMAKE_SOURCE_DIR}/sentences.csv" "$<TARGET_FILE_DIR:ScreenPoster>/sentences.csv"
)
