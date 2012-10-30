$root = $env:IL2JSROOT

$guid_regex        = new-object System.Text.RegularExpressions.Regex("%GUID%")
$baseguid_regex    = new-object System.Text.RegularExpressions.Regex("%BASEGUID%")
$name_regex        = new-object System.Text.RegularExpressions.Regex("%NAME%")
$target_regex      = new-object System.Text.RegularExpressions.Regex("%TARGET%")
$output_regex      = new-object System.Text.RegularExpressions.Regex("%OUTPUT%")

$standard_regex    = new-object System.Text.RegularExpressions.Regex("^#STANDARD#[ ]*(.*)$")
$isbase_regex      = new-object System.Text.RegularExpressions.Regex("^#ISBASE#[ ]*(.*)$")
$refbase_regex     = new-object System.Text.RegularExpressions.Regex("^#REFBASE#[ ]*(.*)$")

$il2js_regex       = new-object System.Text.RegularExpressions.Regex("^#IL2JS#[ ]*(.*)$")
$wsh_regex         = new-object System.Text.RegularExpressions.Regex("^#WSH#[ ]*(.*)$")

$blank_regex       = new-object System.Text.RegularExpressions.Regex("^[ ]*(.*)$")

function setup($flavor, $target, $name, $guid, $baseguid) {
  "> setup $name ($flavor) for $target"
  if ($flavor -eq "isbase") {
    $output = "Library"
  }
  else {
    $output = "Exe"
  }
  cat $root\Tests\template | foreach {
    $line = $_
    $line = $target_regex.Replace($line, $target.ToUpper())
    $line = $guid_regex.Replace($line, $guid.ToUpper())
    $line = $name_regex.Replace($line, $name)
    $line = $output_regex.Replace($line, $output)
    if ($baseguid.length -ne 0) {
      $line = $baseguid_regex.Replace($line, $baseguid.ToUpper())
    }
    $match = $il2js_regex.Match($line)
    if ($match.Success) {
      if ($target -eq "il2js") {
        $match.Groups[1].Value
      }
    }
    else {
      $match = $wsh_regex.Match($line)
      if ($match.Success) {
        if ($target -eq "wsh") {
          $match.Groups[1].Value
        }
      }
      else {
        $match = $standard_regex.Match($line)
        if ($match.Success) {
          if ($flavor -eq "standard") {
           $match.Groups[1].Value
           }
        }
        else {
          $match = $isbase_regex.Match($line)
          if ($match.Success) {
            if ($flavor -eq "isbase") {
              $match.Groups[1].Value
            }
          }
          else {
            $match = $refbase_regex.Match($line)
            if ($match.Success) {
              if ($flavor -eq "refbase") {
                $match.Groups[1].Value
              }
            }
            else {
              $match = $blank_regex.Match($line)
              if ($match.Success) {
                $match.Groups[1].Value
              }
            }
          }
        }
      }
    }
  } > "$root\Tests\$name\IL2JS_Tests_$($name)_$($target).csproj"
}

setup "standard" "il2js"       "Arithmetic"     "C17859A2-F6C1-429A-9559-5B0E76F63182" ""
setup "standard" "wsh"         "Arithmetic"     "D35A4C75-28AE-423B-8624-FA710B0BBF4B" ""

setup "standard" "il2js"       "Array"          "2E37CCD5-670B-4175-A872-1BFDB05B4625" ""
setup "standard" "wsh"         "Array"          "06A1066A-A003-42A0-910F-D9ABBD5EB9F6" ""

setup "standard" "il2js"       "BCL"            "9B7A9AA0-E1C5-4143-8012-4502E6A9151D" ""
setup "standard" "wsh"         "BCL"            "A04D8993-26DC-4D53-8268-E2BD6830FDE9" ""

setup "standard" "il2js"       "ControlFlow"    "8B68AAD3-09FA-4D5A-8AFD-191402B2239A" ""
setup "standard" "wsh"         "ControlFlow"    "E94A0795-9172-4AE1-97B7-C5E6E1587B28" ""

setup "standard" "il2js"       "Delegates"      "FFC31A1D-48AA-4559-84DD-8DEB97734F91" ""
setup "standard" "wsh"         "Delegates"      "215212D0-58BE-4DE4-9CB9-9D9EEA011418" ""

setup "standard" "il2js"       "Enums"          "C4EEE9C1-E60E-4EAF-BF85-39832828C790" ""
setup "standard" "wsh"         "Enums"          "3C56E090-3DF0-4E34-BD04-5761761C27D2" ""

setup "standard" "il2js"       "Exception"      "3E21140E-1E2B-4EA4-B496-2999E1A6193C" ""
setup "standard" "wsh"         "Exception"      "390C15EC-AE8C-490B-ACB5-7D78C2EEF7D2" ""

setup "standard" "il2js"       "ForEach"        "F747717F-5AEA-4ED8-A0FA-EA39194ADD53" ""
setup "standard" "wsh"         "ForEach"        "CA49E615-4763-404F-9E4C-47131E5B6FC2" ""

setup "standard" "il2js"       "HelloWorld"     "980EA15A-9E26-43E8-8CDC-DE8C5681738B" ""
setup "standard" "wsh"         "HelloWorld"     "0570DBAF-23BC-479C-AD32-62C0FB466FDE" ""

setup "standard" "il2js"       "Initializer"    "FC44277C-8E26-43DC-BE86-D3FEC3596F82" ""
setup "standard" "wsh"         "Initializer"    "6302D1BC-B770-436B-A6CD-323C5B62AAF4" ""

setup "standard" "il2js"       "Instructions"   "13FDD0D2-74D2-46A9-960E-7F3BCA8C4D67" ""
setup "standard" "wsh"         "Instructions"   "DDA36474-3694-4168-9662-C8CFCA909915" ""

setup "standard" "il2js"       "Interop"        "91C40149-1EF0-40A0-9AA2-D158C5445A7E" ""
setup "standard" "wsh"         "Interop"        "58C89D53-A8CC-4d51-8FAE-F65CA9005086" ""

setup "standard" "il2js"       "Lists"          "D08CAAE9-F6FC-4859-ACA8-A42D7268A911" ""
setup "standard" "wsh"         "Lists"          "67DB7528-D662-4EDE-9E97-F09729EB5984" ""

setup "standard" "il2js"       "Methods"        "4491DF7D-0328-4BA1-997B-9B3843E709C3" ""
setup "standard" "wsh"         "Methods"        "8136F979-3DFA-4435-B2CE-2C258405DDCF" ""

setup "standard" "il2js"       "Nullable"       "81007EDC-0291-4533-A8A0-2B8F78E62DF6" ""
setup "standard" "wsh"         "Nullable"       "38570DB5-D349-4757-AF5A-CC6295946A1C" ""

setup "standard" "il2js"       "Recursion"      "16E24927-45C8-4C8D-9A13-386EECCE9C32" ""
setup "standard" "wsh"         "Recursion"      "AFB9BB89-1370-446D-B229-F60D4BEAA107" ""

setup "standard" "il2js"       "ValueTypes"     "4B4485BE-F40D-493B-93B6-BAC241F07B3E" ""
setup "standard" "wsh"         "ValueTypes"     "EFC0FB41-A0A9-4545-96A3-FE2D3983479F" ""

setup "standard" "il2js"       "Virtual"        "DEE1C9A1-6F3F-4706-822B-4D495CF9EB1C" ""
setup "standard" "wsh"         "Virtual"        "9B07F2C5-C15A-4600-99CA-CE52DD770518" ""

setup "isbase"   "il2js"       "ReflectionBase" "31001E1F-58E1-4EAA-BC8E-54329B141F1C" ""
setup "isbase"   "wsh"         "ReflectionBase" "94880C6D-28A3-4838-8A76-740A182BFF8A" ""

setup "refbase"  "il2js"       "Reflection"     "873CB1A0-FE9E-4A9C-9E6E-97B14EC17B13" "31001E1F-58E1-4EAA-BC8E-54329B141F1C"
setup "refbase"  "wsh"         "Reflection"     "A2E121DB-E4CB-4449-959D-C5C3B541A9DC" "94880C6D-28A3-4838-8A76-740A182BFF8A"

setup "isbase"   "il2js"       "GenericsBase"   "4FB96D9C-3CAE-4E3D-B7FD-2F8B58A005BC" ""
setup "isbase"   "wsh"         "GenericsBase"   "149765D6-4327-4B11-A9B3-04927A7C4478" ""

setup "refbase"  "il2js"       "Generics"       "7DE4A962-EA37-44A1-A55B-676BC8B5A450" "4FB96D9C-3CAE-4E3D-B7FD-2F8B58A005BC"
setup "refbase"  "wsh"         "Generics"       "7755155D-EC7F-4B4D-BF5E-2A58C558980C" "149765D6-4327-4B11-A9B3-04927A7C4478"

setup "standard" "il2js"       "Enumerable"     "BC6EEFB6-A893-4c67-825B-62DEDCC003E0" ""
setup "standard" "wsh"         "Enumerable"     "9F5C3FD6-9D1E-49d9-ADB8-46CCE793B391" ""
