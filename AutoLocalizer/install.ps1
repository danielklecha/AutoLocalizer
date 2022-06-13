dotnet tool uninstall --global AutoLocalizer
dotnet pack --configuration Release
dotnet tool install --global --add-source ./nupkg AutoLocalizer