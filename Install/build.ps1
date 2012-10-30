$zip = "$env:IL2JSROOT\utils\zip.exe"
$wix = "$env:IL2JSROOT\utils\Wix\bin"
$candle = "$wix\candle.exe"
$light = "$wix\light.exe"
$uiext = "$wix\WixUIExtension.dll"
$vsext = "$wix\WixVSExtension.dll"


"> IL2JSApplication template"
rm -ErrorAction:SilentlyContinue "$env:IL2JSROOT\Install\IL2JSApplication.zip"
cd "$env:IL2JSROOT\Templates\IL2JSApplication"
&$zip -r "$env:IL2JSROOT\Install\IL2JSApplication.zip" *
cd "$env:IL2JSROOT\Templates\IL2JSLibrary"
&$zip -r "$env:IL2JSROOT\Install\IL2JSLibrary.zip" *

cd "$env:IL2JSROOT\Install"
"> candle"
&$candle $IL2JSROOT il2js.wxs
"> light"
&$light -ext $uiext -ext $vsext il2js.wixobj
