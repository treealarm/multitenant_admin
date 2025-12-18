$Image = "mtadminserver"
$Tag = "latest"
$Remote = "pruginkad/${Image}:${Tag}"

Write-Host "Tagging image..."
docker tag "${Image}:${Tag}" $Remote
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "Pushing image..."
docker push $Remote
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "Done"
