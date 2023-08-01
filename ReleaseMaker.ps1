Try{

msbuild.exe /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
if (! $?) { throw "msbuild failed" }


$scriptPath = (Resolve-Path .\).Path + "\"
$tcpListenerVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($scriptPath + "TcpListener\bin\Release\net452\TcpListener.exe").FileVersion
$dataParserVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($scriptPath + "Teltonika.DataParser.Client\bin\Release\Teltonika.DataParser.Client.exe").FileVersion
$udpListenerVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($scriptPath + "UdpListener\bin\Release\net452\UdpListener.exe").FileVersion

$releasePackVersion = $dataParserVersion.Substring(0, $dataParserVersion.lastIndexOf('.'))
$releasePackFolderName = "Teltonika Parser v" + $releasePackVersion

$tcpListenerFolderName = $releasePackFolderName + "\TcpListener v" + $tcpListenerVersion
$dataParserFolderName =  $releasePackFolderName + "\Teltonika.Data.Parser v" + $dataParserVersion
$sourceCodeFolderName = $releasePackFolderName + "\Teltonika.Parser (source code)"
$udpListenerFolderName = $releasePackFolderName + "\UdpListener v" + $udpListenerVersion

# MAKE FOLDERS

	Remove-Item $releasePackFolderName -Recurse -ErrorAction Ignore
    new-item -Name $releasePackFolderName -ItemType directory | Out-Null
	
    new-item -Name $tcpListenerFolderName -ItemType directory | Out-Null
    new-item -Name $dataParserFolderName -ItemType directory | Out-Null 
    new-item -Name $sourceCodeFolderName -ItemType directory | Out-Null	
    new-item -Name $udpListenerFolderName -ItemType directory | Out-Null 	

# COPY IMPORTANT DATA
	copy "TcpListener\bin\Release\net452\*.*" "$tcpListenerFolderName" 
	copy "Teltonika.DataParser.Client\bin\Release\*.*" "$dataParserFolderName" 
	copy "UdpListener\bin\Release\net452\*.*" "$udpListenerFolderName"
	
	msbuild.exe /t:Clean,Build
	if (! $?) { throw "msbuild clean failed" }	
	
	xcopy "TcpListener" ("$sourceCodeFolderName" + "\TcpListener") /E/I/Q	
	xcopy "Teltonika.Codec" ("$sourceCodeFolderName" + "\Teltonika.Codec") /E/I/Q
	xcopy "Teltonika.DataParser.Client" ("$sourceCodeFolderName" + "\Teltonika.DataParser.Client") /E/I/Q
	xcopy "UdpListener" ("$sourceCodeFolderName" + "\UdpListener") /E/I/Q
	
	copy "Teltonika Data Parser HOW-TO.docx" "$sourceCodeFolderName"
	copy "Teltonika.Parser.sln" "$sourceCodeFolderName"	  

	Get-ChildItem -Path $sourceCodeFolderName -Include bin,obj -Recurse | Remove-Item -Recurse -Force
	
# MAKE ARCHIVE		

    Write-Host "Compressing '$releasePackFolderName'..." -ForegroundColor Green
    $ProcessInfo = New-Object System.Diagnostics.ProcessStartInfo 
    $ProcessInfo.FileName = "$($scriptPath)$("7zr.exe")"
    $ProcessInfo.RedirectStandardError = $true 
    $ProcessInfo.RedirectStandardOutput = $true 
    $ProcessInfo.UseShellExecute = $false 
    $ProcessInfo.Arguments = "a `"$($scriptPath)$($releasePackFolderName).7z`" `"$($scriptPath)$($releasePackFolderName)`""
    $Process = New-Object System.Diagnostics.Process 
    $Process.StartInfo = $ProcessInfo 
    $Process.Start() | Out-Null 
    $Process.WaitForExit() 
    $output = $Process.StandardOutput.ReadToEnd() 
    $output 
    Write-Host "Folder was sucessfully compressed." -ForegroundColor Green
    
# DELETE MASTER DIRECTORY
    Remove-Item -Recurse -Force "$releasePackFolderName"
	
} Catch {
   $errorMessage = $_.Exception.Message
   $failedItem = $_.Exception.ItemName
   Write-Host "Error: $errorMessage. Item that failed - $failedItem. Press 'Enter' to exit" -ForegroundColor Red
   pause
}

