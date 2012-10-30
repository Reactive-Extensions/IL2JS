function teardown($name) {
  "> teardown $name"
  $dir = "$env:IL2JSROOT\Tests\$name"
  rmdir -ErrorAction:SilentlyContinue -r $dir\bin
  rmdir -ErrorAction:SilentlyContinue -r $dir\obj
  rm -ErrorAction:SilentlyContinue $dir\TestIL2JS.html
  ls -r $dir | where { $_.Name -ilike "*.csproj*" -and $_ -is [System.IO.FileInfo] } | foreach { rm $_.FullName }
  ls -r $dir | where { ( $_.Name -ilike "*.opts" -or $_.Name -ilike "*.out" -or $_.Name -ilike "*.trace" ) -and $_ -is [System.IO.FileInfo] } | foreach { rm $_.FullName }
  ls -r $dir | where { $_.Name -like "*,*" -and $_ -is [System.IO.DirectoryInfo] } | foreach { rm -r $_.FullName }
}

ls $env:IL2JSROOT\Tests\ | where { $_ -is [System.IO.DirectoryInfo] } | foreach { teardown $_.Name }

