dotnet tool uninstall --global AutoLocalizer
dotnet pack
dotnet tool install --global --add-source ./nupkg AutoLocalizer