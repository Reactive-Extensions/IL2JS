param (
  $action,
  [switch]$debug,
  [switch]$debugger
)

# Global files

$windowsroot="$env:systemroot"
$cscript="$windowsroot\system32\cscript.exe"
if (!(test-path $cscript)) {
  throw "FAIL: No cscript at '$cscript'"
}
$cscriptopts = "/H:CScript", "/Nologo"

$slroot="C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\Silverlight\v4.0"

$ilasm="$windowsroot\Microsoft.NET\Framework\v2.0.50727\ilasm.exe"
if (!(test-path $ilasm)) {
  throw "FAIL: No ilasm at '$ilasm'"
}
$ilasmopts = "/err", "/noautoinherit"

$winsdk = $(get-itemproperty "HKLM:\SOFTWARE\Microsoft\Microsoft SDKs\Windows").CurrentInstallFolder
$ildasm="$winsdk\bin\ildasm.exe"
if (!(test-path $ildasm)) {
  throw "FAIL: No ildasm at '$ildasm'"
}
$ildasmopts = "/nobar"

$windiff="$winsdk\bin\windiff.exe"
if (!(test-path $windiff)) {
  throw "FAIL: No windiff at '$windiff'"
}

$msbuild = "msbuild"
$msbuildopts = "/nologo", "/verbosity:quiet"

# Local files

$tests = "$env:IL2JSROOT\Tests"
if (!(test-path $tests)) {
  throw "FAIL: Cannot find tests at '$tests'"
}

function usage() {
  "usage:"
  "  test clean"
  "  test clean <testname>"
  "  test run"
  "  test run <testname>"
  "  test baseline <testname>"
  "  test cscript <testname>"
  "  test recscript <testname>"
  "  test rerunjs <testname>"
  "  test diff <testname>"
  "  test list"
  "with options:"
  "  -debug"
  "  -debugger"
}

function clean($test) {
  "> clean $test"
  $test_dir = "$tests\$test"
  if (!(test-path $test_dir)) {
    throw "FAIL: No such test '$test'"
  }
  rmdir -ErrorAction:SilentlyContinue -r $test_dir\bin
  rmdir -ErrorAction:SilentlyContinue -r $test_dir\obj
}

function build($test, $target) {
  "> build $test for $target"
  $test_dir = "$tests\$test"
  if (!(test-path $test_dir)) {
    throw "FAIL: No such test '$test'"
  }
  switch -regex ($target) {
    "il2js" {
      "  > build il2jsc"
      &$msbuild $msbuildopts "/p:Configuration=Debug" "$env:IL2JSROOT\Compiler\IL2JS_Compiler.csproj"
      if (!$?) {
        throw "FAIL: Cannot build il2jsc"
      }
      $il2jsc="$env:IL2JSROOT\bin\il2jsc.exe"
      if (!(test-path $il2jsc)) {
        throw "FAIL: No il2jsc at '$il2jsc'"
      }
      "  > build mscorlib"
      &$msbuild $msbuildopts "/p:Configuration=Debug" "$env:IL2JSROOT\mscorlib\IL2JS_mscorlib.csproj"
      if (!$?) {
        throw "FAIL: Cannot build mscorlib"
      }
      "  > build test"
      if ($debug) {
        "    > in debug mode"
        $config = "Debug"
      }
      else {
        "    > in release mode"
        $config = "Release"
      }
      $platform = "Plain"
      $test_bin = "$test_dir\bin\$($config)$($platform)"
      $test_exe = "$test_bin\IL2JS_Tests_$($test)_$($target).exe"
      $il2jscopts = "-mode $platform +prettyPrint +clrArraySemantics +clrNullVirtcallSemantics +clrInteropExceptions +safeInterop -target cscript -loadPath `"$test_bin`""
      if ($debugger) {
        $il2jscopts += " -debugLevel 2"
        $il2jscopts += " -debugTrace `"$test_bin\il2jsc.trace`""
        $il2jscopts += " -allWarns"
      }
      $dummy = $(mkdir -ErrorAction:SilentlyContinue $test_bin)
      &$msbuild $msbuildopts "/p:Configuration=$config" "/p:Platform=$platform" "/p:IL2JSCOptions=$il2jscopts" "$test_dir\IL2JS_Tests_$($test)_$($target).csproj" > $test_bin\il2jsc.out
      if (!$?) {
        throw "FAIL: Cannot build '$test'"
      }
      if (!(test-path $test_bin)) {
        throw "FAIL: No test binaries at '$test_bin'"
      }
      if (!(test-path $test_exe)) {
        throw "FAIL: No test executable at '$test_exe'"
      }
      cat "$test_bin\manifest.txt" | foreach { cat "$test_bin\$_" >> $test_bin\test.js }
    }
    "wsh" {
      "  > build test"
      &$msbuild $msbuildopts "/p:Configuration=Debug" "/p:IL2JSGenerateJavaScript=false" "$test_dir\IL2JS_Tests_$($test)_$($target).csproj"
      if (!$?) {
        throw "FAIL: Cannot build '$test'"
      }
      $test_bin = "$test_dir\bin\Debug"
      if (!(test-path $test_bin)) {
        throw "FAIL: No test binaries at '$test_bin'"
      }
      $test_exe = "$test_bin\IL2JS_Tests_$($test)_$($target).exe"
      if (!(test-path $test_exe)) {
        throw "FAIL: No test executable at '$test_exe'"
      }
    }
    default {
      throw "FAIL: Unrecognised build target '$target'"
    }
  }
}

function run($test, $target) {
  $test_dir = "$tests\$test"
  if (!(test-path $test_dir)) {
    throw "FAIL: No such test '$test'"
  }
  switch ($target) {
    "cscript" {
      if ($debug) {
        $config = "Debug"
      }
      else {
        $config = "Release"
      }
      $platform = "Plain"
      if ($debugger) {
        $flag = "/X"
      } else {
        $flag = "/D"
      }
      $test_main = "$test_dir\bin\$($config)$($platform)\test.js"
      if (!(test-path $test_main)) {
        throw "FAIL: No main at '$test_main'"
      }
      &$cscript $cscriptopts $flag $test_main
      if (!$?) {
        throw "FAIL: Test '$test_main' failed"
      }
    }
    "wsh" {
      $test_exe = "$test_dir\bin\Debug\IL2JS_Tests_$($test)_wsh.exe"
      if (!(test-path $test_exe)) {
        throw "FAIL: No executable at '$test_exe'"
      }
      &"$test_exe"
      if (!$?) {
        throw "FAIL: Test '$test_exe' failed"
      }
    }
    "browser" {
      $test_page = "$test_dir\bin\Release\TestIL2JS.html"
      if (!(test-path $test_page)) {
        throw "FAIL: No test page at '$test_page'"
      }
      &start $test_page
    }
    "handwritten" {
      $test_page = "$test_dir\TestHandWritten.html"
      if (!(test-path $test_page)) {
        throw "FAIL: No test page at '$test_page'"
      }
      &start $test_page
    }
    default {
      throw "FAIL: Unrecognised run target '$target'"
    }
  }
}

function runAndCompare($test) {
  "> runAndCompare $test"
  clean $test
  build $test "il2js" "cscript"
  build $test "wsh"
  $left = "$tests\$test\bin\Debug\baseline.out"
  "> run '$test' for 'wsh'"
  run $test "wsh" > $left
  if ($debug) {
    $config = "Debug"
  } else {
    $config = "Release"
  }
  $platform = "Plain"
  $right = "$tests\$test\bin\$($config)$($platform)\il2js.out"
  "> run '$test' for 'cscipt'"
  run $test "cscript" > $right
  if ("$(cat $left)" -ne "$(cat $right)") {
    throw "FAIL: Outputs for test '$test' differ (try 'test diff $test' to see differences)"
  }
  cat $left
  "SUCCESS: Test '$test' passes"
}

function showDiff($test) {
  $left = "$tests\$test\bin\Debug\baseline.out"
  $right = "$tests\$test\bin\ReleasePlain\il2js.out"
  &$windiff $left $right
}

$alltests = ls $tests |
            where { $_ -is [System.IO.DirectoryInfo] } |
            foreach { $_.Name }

$allabtests = ls $tests |
              where { $_ -is [System.IO.DirectoryInfo] -and
                      (test-path "$($_.FullName)\IL2JS_Tests_$($_.Name)_il2js.csproj") -and
                      !(test-path "$($_.FullName)\LIBRARY") } |
              foreach { $_.Name }

switch ($action) {
  "clean" {
    if ($args.length -eq 0) {
      foreach ($test in $alltests) {
        clean $test
      }
    }
    elseif ($args.length -eq 1) {
      clean $args[0]
    }
    else {
      usage
      throw "FAIL: Incorrect usage"
    }
  }
  "run" {
    if ($args.length -eq 0) {
      foreach ($test in $allabtests) {
        "BEGIN $test"
        runAndCompare $test
        "END $test"
      }
      "---------------------------"
      "| SUCCESS: All tests pass |"
      "---------------------------"
    }
    elseif ($args.length -eq 1) {
      runAndCompare $args[0]
    }
    else {
      usage
      throw "FAIL: Incorrect usage"
    }
  }
  "baseline" {
    if ($args.length -eq 1) {
      clean $args[0]
      build $args[0] "wsh"
      run $args[0] "wsh"
    }
    else {
      usage
      throw "FAIL: Incorrect usage"
    }
  }
  "cscript" {
    if ($args.length -eq 1) {
      clean $args[0]
      build $args[0] "il2js"
      run $args[0] "cscript"
    }
    else {
      usage
      throw "FAIL: Incorrect usage"
    }
  }
  "recscript" {
    if ($args.length -eq 1) {
      run $args[0] "cscript"
    }
    else {
      usage
      throw "FAIL: Incorrect usage"
    }
  }
  "diff" {
    if ($args.length -eq 1) {
      showDiff $args[0]
    }
    else {
      usage
      throw "FAIL: Incorrect usage"
    }
  }
  "list" {
    $allabtests
  }
  "help" {
    usage
  }
  default {
    usage
    throw "FAIL: Incorrect usage"
  }
}
