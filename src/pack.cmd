dotnet clean ./Purview.Logging.SourceGenerator.sln
dotnet pack -c Release -o ./.artifacts/ ./Purview.Logging.SourceGenerator/Purview.Logging.SourceGenerator.csproj

dotnet clean ./Purview.Logging.SourceGenerator.sln
dotnet pack -c Release -o ./.artifacts/ ./Purview.Logging.SourceGenerator.CSharp9/Purview.Logging.SourceGenerator.CSharp9.csproj
