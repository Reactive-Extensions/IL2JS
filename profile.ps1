$env:IL2JSROOT=pwd

function ct {
  devenv "$env:IL2JSROOT\IL2JS_CompileTime.sln"
}

function rt {
  devenv "$env:IL2JSROOT\IL2JS_RunTime.sln"
}

####
#### astatus
####

function newHashSet {
  $ctor = [Type]"System.Collections.Generic.HashSet``1";
  $str = [Type]"System.String";
  $type = $ctor.MakeGenericType($str);
  return ,[Activator]::CreateInstance($type);
}

function astatus {
  param( [switch]$execute )

  if ($env:SDROOT -eq $()) {
    throw "FAIL: No SDROOT environment variable set"
  }

  $pat = new-object System.Text.RegularExpressions.Regex("^(.*)#[0-9]+ - ([a-z]+) (default change|change [0-9]+) \([a-z]+\)$")
  $ignore = new-object System.Text.RegularExpressions.Regex("^.*\.csproj\.user$|^.*\.ReSharper\.user$|^.*\.ReSharper$|^.*\.sln\.cache$|^.*\\bin\\.*$|^.*\\obj\\.*$")
  $depotPrefix = "//depot/"
  $depot = newHashSet
  $localPrefix = "$($env:SDROOT)\"
  $local = newHashSet
  $added = newHashSet
  $deleted = newHashSet
  $edited = newHashSet

  sd opened ... | foreach {
    $m = $pat.Match($_.Substring($depotPrefix.Length).Replace("/", "\"))
    if ($m.Success) {
      $type = $m.Groups[2].Value
      $name = $m.Groups[1].Value
      if ($type -eq "delete") {
        [void]$deleted.Add($name)
      }
      elseif ($type -eq "add") {
        [void]$added.Add($name)
      }
      elseif ($type -eq "edit") {
        [void]$edited.Add($name)
      }
    }
  }

  sd files -d ... | sort | foreach {
    $m = $pat.Match($_.Substring($depotPrefix.Length).Replace("/", "\"))
    if ($m.Success) {
      [void]$depot.Add($m.Groups[1].Value)
    }
  }

  ls . -r | where { $_ -is [System.IO.FileInfo] } | foreach {
    [void]$local.Add($_.FullName.Substring($localPrefix.Length))
  }

  "> sd revert -a ..."
  if ($execute) {
    sd revert -a ...
  }

  "> sd online ..."
  if ($execute) {
    sd online ...
  }

  $here = pwd
  cd $env:SDROOT

  $depot | foreach {
    if (!$deleted.Contains($_) -and !$local.Contains($_)) {
      "> sd delete $_"
      if ($execute) {
        sd delete $_
      }
    }
  }

  $local | foreach {
    if (!$added.Contains($_) -and !$depot.Contains($_)) {
      $m = $ignore.Match($_)
      if (!$m.Success) {
        "> sd add $_"
        if ($execute) {
          sd add $_
        }
      }
    }
  }

  cd $here
}

####
#### Environment
####

function setSDROOT {
    $path = $env:IL2JSROOT
    while ($path -ne $() -and $path.length -gt 0)
    {
        if (test-path (join-path $path "sd.ini")) {
            $env:SDROOT=$path
            return
        }
        $path = split-path -parent $path
    }
}

setSDROOT

$host.ui.RawUI.WindowTitle = "IL2JS $env:IL2JSROOT"
$host.ui.RawUI.ForegroundColor = "White"
$host.ui.RawUI.BackgroundColor = "DarkGray"
cls

if ($env:SDROOT -ne $()) {
  "SDROOT=$($env:SDROOT)"
}
else {
  "Note: No 'sd.ini' file found, not setting SDROOT"
}

$windowsroot="$env:systemroot"
$winsdk = $(get-itemproperty "HKLM:\SOFTWARE\Microsoft\Microsoft SDKs\Windows").CurrentInstallFolder
$vsroot = $(get-itemproperty -ErrorAction:SilentlyContinue "HKLM:\SOFTWARE\Microsoft\VisualStudio\10.0").InstallDir
if ($vsroot -eq $()) {
    $vsroot = $(get-itemproperty -ErrorAction:SilentlyContinue "HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\10.0").InstallDir
}
if ($vsroot -eq $()) {
  "Note: No VS2010 could be found."
}
else {
  "VS2010 is in $vsroot"
}

$utils = "$env:IL2JSROOT\utils"

$origpath = $env:PATH

$env:PATH ="$winsdk\bin\"
$env:PATH+=";$windowsroot\Microsoft.NET\Framework\v4.0.30319\"
$env:PATH+=";$vsroot"
$env:PATH+=";$utils"
$env:PATH+=";$utils\SD\X86\"
$env:PATH+=";$utils\SDB\";
$env:PATH+=";$utils\SDPack\";
$env:PATH+=";$utils\Reflector\";
$env:PATH+=";$utils\Wix\bin\";
$env:PATH+=";$env:IL2JSROOT\bin\"
$env:PATH+=";$origpath"

