$Env:ASPNETCORE_ENVIRONMENT = "Development"
Start-CosmosDbEmulator

Push-Location DevOps/Tye
# Start-Process -FilePath "wt" -ArgumentList "tye run --dashboard --watch --logs console"
Start-Process pwsh -argument '-nologo -noprofile -executionpolicy bypass -command tye run --dashboard --logs console'
# tye run --dashboard --watch --logs console
Pop-Location
 