param (
    [Parameter(Mandatory=$true)]
    [string]
    $DbContextName,

    [Parameter(Mandatory=$true)]
    [string]
    $MigrationName
)

$project = 'RSSViewer.Core'

dotnet ef migrations add $MigrationName --context $DbContextName --project $project
