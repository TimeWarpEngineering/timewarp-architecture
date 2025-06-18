$FeatureName = "__FeatureName__"
$RootNamespace = "__RootNamespace__"

$ClientFeaturePath = "..\Source\Client\Features\$FeatureName"
$ClientTestPath = "..\Tests\Client.Integration.Tests\Features"

New-Item -ItemType Directory -Force -Path $ClientFeaturePath
New-Item -ItemType Directory -Force -Path $ClientTestPath

Move-Item "Client\*" -Destination $ClientFeaturePath
Move-Item "Client.Tests\*" -Destination $ClientTestPath