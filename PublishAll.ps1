dotnet publish -p:PublishProfile=win-x64-self-contained
dotnet publish -p:PublishProfile=osx-x64-self-contained
dotnet publish -p:PublishProfile=linux-x64-self-contained

#dotnet publish -p:PublishProfile=win-x64-framework-dependent
#dotnet publish -p:PublishProfile=osx-x64-framework-dependent
#dotnet publish -p:PublishProfile=linux-x64-framework-dependent
