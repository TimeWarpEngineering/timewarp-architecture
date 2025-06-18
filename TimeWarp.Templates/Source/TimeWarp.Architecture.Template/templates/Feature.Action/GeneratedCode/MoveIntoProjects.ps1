$FeatureName = "__FeatureName__"


$ClientFeaturePath = "..\Source\Client\Features\$FeatureName\Actions"
$ClientTestPath = "..\Tests\Client.Integration.Tests\Features\$FeatureName"

New-Item -ItemType Directory -Force -Path $ClientFeaturePath
New-Item -ItemType Directory -Force -Path $ClientTestPath

Move-Item "Client\*" -Destination $ClientFeaturePath
Move-Item "Client.Tests\*" -Destination $ClientTestPath