$key = $env:private_nuget_apikey
if ($key) {
	Write-Output "API KEY found"
	$packages = Get-ChildItem -Recurse packages\*.nupkg
	foreach ($pkg in $packages) {
		..\.nuget\nuget push $pkg.FullName -source https://nuget.boxfusion.co.za/api/v2/package -apikey $key
	}
} else {
	Write-Output "API KEY not found!"
}