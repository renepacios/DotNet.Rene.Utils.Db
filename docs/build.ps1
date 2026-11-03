$ErrorActionPreference = 'Stop'

Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # These integrations also build the core library and abstractions through project references.
    $projects = @(
        (Join-Path 'src' 'Rene.Utils.Db.AutoMapper' 'Rene.Utils.Db.AutoMapper.csproj')
        (Join-Path 'src' 'Rene.Utils.Db.Mapster' 'Rene.Utils.Db.Mapster.csproj')
    )
    foreach ($project in $projects) {
        dotnet build $project --configuration Release `
            --property:GenerateDocumentationFile=true `
            --property:GeneratePackageOnBuild=false `
            --property:CopyLocalLockFileAssemblies=true `
            --property:NoWarn=1591
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    dotnet docfx (Join-Path $PSScriptRoot 'docfx.json') --warningsAsErrors
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
