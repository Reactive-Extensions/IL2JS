rm -ErrorAction:SilentlyContinue -r $env:IL2JSROOT/bin/
$files = @(ls $env:IL2JSROOT -R | where { $_ -is [System.IO.DirectoryInfo] -and ( $_.Name -ieq "obj" -or $_.Name -ieq "bin" ) -and -not ( $_.FullName -ilike "*\utils\*" ) })
$files += @(ls $env:IL2JSROOT -R | where { $_ -is [System.IO.DirectoryInfo] -and $_.Name -ilike "_Resharper*" })
$files += @(ls $env:IL2JSROOT -R | where { $_ -is [System.IO.DirectoryInfo] -and $_.Name -ieq "Properties" -and $_.GetFileSystemInfos().Length -eq 0 })
$files += @(ls $env:IL2JSROOT -R | where { $_ -is [System.IO.FileInfo] -and $_.Name -ilike "*.resharper.user" })

if ($files.length -ne 0) {
  $files | foreach { "  $($_.FullName)" }
  [System.Console]::Write("$($files.Length) files to clean, ok? ")
  $resp = [System.Console]::ReadLine()
  if ($resp -ieq "y") {
    $files | foreach { rm -r $_.FullName }
    "cleaned."
  }
  else {
    "aborted."
  }
}
else {
  "no files to clean."
}
