$FeatureName = "__FeatureName__"

$ApiFeaturePath = "..\Source\Api\Features\$FeatureName"
$ClientFeaturePath = "..\Source\Client\Features\$FeatureName"
$ServerPath = "..\Source\Server"
$ServerTestsPath = "..\Tests\Server.Integration.Tests\Features\$FeatureName\$RequestName"

New-Item -ItemType Directory -Force -Path $ClientFeaturePath
New-Item -ItemType Directory -Force -Path $ServerTestsPath

Move-Item "Source\Api\Features\*" -Destination $ApiRequestPath
Move-Item "Source\Server\*" -Destination $ServerPath
Move-Item "Source\Client\**" -Destination $ClientFeaturePath