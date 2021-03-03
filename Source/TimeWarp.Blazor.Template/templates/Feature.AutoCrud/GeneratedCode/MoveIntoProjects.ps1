$FeatureName = "__FeatureName__"

$ApiFeaturePath = "..\Source\Api\Features\$FeatureName"
$ClientFeaturePath = "..\Source\Client\Features\$FeatureName"
$ServerDataPath = "..\Source\Server\DataAccess"
$ServerFeaturePath = "..\Source\Server\Features"
$ServerMapperPath = "..\Source\Server\Mappers"
$ServerModelPath = "..\Source\Server\Models"
$ServerTestsPath = "..\Tests\Server.Integration.Tests\Features\$FeatureName\$RequestName"

New-Item -ItemType Directory -Force -Path $ApiFeaturePath
New-Item -ItemType Directory -Force -Path $ClientFeaturePath
New-Item -ItemType Directory -Force -Path $ServerTestsPath
New-Item -ItemType Directory -Force -Path $ServerMapperPath
New-Item -ItemType Directory -Force -Path $ServerModelPath
New-Item -ItemType Directory -Force -Path $ServerDataPath

Move-Item "Source\Api\Features\*" -Destination $ApiFeaturePath
Move-Item "Source\Client\*" -Destination $ClientFeaturePath
Move-Item "Source\Server\Data\*" -Destination $ServerDataPath
Move-Item "Source\Server\Features\*" -Destination $ServerFeaturePath
Move-Item "Source\Server\Mappers\*" -Destination $ServerMapperPath
Move-Item "Source\Server\Models\*" -Destination $ServerModelPath