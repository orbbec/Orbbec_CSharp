project(3.advanced.hdr LANGUAGES CSharp)

include(CSharpUtilities)

add_executable(${PROJECT_NAME}
  Program.cs
  Window.xaml
  Window.xaml.cs
)

target_link_libraries(${PROJECT_NAME} PRIVATE ob_csharp common)

add_dependencies(${PROJECT_NAME} ob_csharp common)

set_property(TARGET ${PROJECT_NAME} PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION v4.7.2)

set_property(TARGET ${PROJECT_NAME} PROPERTY VS_DOTNET_REFERENCES
    "System"
    "System.Xaml"
    "PresentationCore"
    "PresentationFramework"
    "WindowsBase"
)

set_target_properties(${PROJECT_NAME} PROPERTIES
    FOLDER samples
)

install(TARGETS 3.advanced.hdr RUNTIME DESTINATION bin)