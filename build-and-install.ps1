dotnet build --configuration Release AutoLocalizer.sln
dotnet pack --no-build --configuration Release --output ./nupkg  AutoLocalizer.sln
dotnet tool update --global --add-source ./nupkg --no-cache AutoLocalizer
Read-Host -Prompt "Press Enter to exit..."