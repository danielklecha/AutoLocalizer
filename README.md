# AutoLocalizer

[![NuGet](https://buildstats.info/nuget/AutoLocalizer?includePreReleases=false)](https://www.nuget.org/packages/AutoLocalizer "Download AutoLocalizer from NuGet")

## Introduction

AutoLocalizer is a [.NET Global Tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools ".NET Global Tools overview") for generating translations. Specifically, it reads string values from resource files in the [resx](https://docs.microsoft.com/en-us/dotnet/framework/resources/creating-resource-files-for-desktop-apps#resources-in-resx-files "Resources in .resx Files
") format and generates automatic translations for selected language.

The tool is run from the command line and provides the following options for the translation:

* Translate using Microsoft Translator
* Use exising accounts

## Installation

To install the tool from [NuGet](https://www.nuget.org/packages/AutoLocalizer "AutoLocalizer on NuGet.org") using the .NET SDK run:

```powershell
dotnet tool install --global AutoLocalizer
```

## Usage

To configure key and region for Microsoft Translator
```powershell
autolocalizer set configuration
```

To generate translated version of the specified file

```powershell
autolocalizer X:\Foo\Bar.resx fr
```

## Feedback

Any feedback or issues can be added to the issues for this project in [GitHub](https://github.com/danielklecha/AutoLocalizer/issues "Issues for this project on GitHub.com").

## Repository

The repository is hosted in [GitHub](https://github.com/danielklecha/AutoLocalizer "This project on GitHub.com"): https://github.com/danielklecha/AutoLocalizer

## License

This project is licensed under the [MIT](https://github.com/danielklecha/AutoLocalizer/blob/master/LICENSE.md "The MIT license") license.