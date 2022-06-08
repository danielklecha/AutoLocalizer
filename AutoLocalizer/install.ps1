dotnet tool uninstall --global autolocalizer
dotnet pack
dotnet tool install --global --add-source ./nupkg autolocalizer