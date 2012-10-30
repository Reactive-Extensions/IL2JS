$msbuild = "msbuild"
$msbuildopts = "/nologo", "/verbosity:quiet"
$config = "Debug"

"> build tools: jsv"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\JSV\IL2JS_JSV.csproj
if (!$?) { throw "BUILD FAILED" }

"> build tools: il2jsc"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\Compiler\IL2JS_Compiler.csproj
if (!$?) { throw "BUILD FAILED" }

"> build tools: il2jsr"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\Rewriter\IL2JS_Rewriter.csproj
if (!$?) { throw "BUILD FAILED" }

"> build tools: il2jsp"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\StartPage\IL2JS_StartPage.csproj
if (!$?) { throw "BUILD FAILED" }

"> build tools: msbuild integration"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\MSBuild\IL2JS_MSBuild.csproj
if (!$?) { throw "BUILD FAILED" }

"> build managed interop: JSTypes library for Windows Scripting Host"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\JSTypes\IL2JS_JSTypes_wsh.csproj
if (!$?) { throw "BUILD FAILED" }

"> build managed interop: JSTypes library for Silverlight"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\JSTypes\IL2JS_JSTypes_silverlight.csproj
if (!$?) { throw "BUILD FAILED" }

"> build managed interop: WSHInterop library"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\WSHInterop\IL2JS_WSHInterop.csproj
if (!$?) { throw "BUILD FAILED" }

"> build managed interop: SilverlightInterop library"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\SilverlightInterop\IL2JS_SilverlightInterop.csproj
if (!$?) { throw "BUILD FAILED" }

"> build libraries: mscorlib library for il2js"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\mscorlib\IL2JS_mscorlib.csproj
if (!$?) { throw "BUILD FAILED" }

"> build libraries: System library for il2js"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\System\IL2JS_System.csproj
if (!$?) { throw "BUILD FAILED" }

"> build libraries: System.Core library for il2js"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\System.Core\IL2JS_System.Core.csproj
if (!$?) { throw "BUILD FAILED" }

"> build libraries: System.Net library for il2js"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\System.Net\IL2JS_System.Net.csproj
if (!$?) { throw "BUILD FAILED" }

"> build libraries: System.Windows library for il2js"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\System.Windows\IL2JS_System.Windows.csproj
if (!$?) { throw "BUILD FAILED" }

"> build libraries: System.CoreEx library for il2js"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\System.CoreEx\IL2JS_System.CoreEx.csproj
if (!$?) { throw "BUILD FAILED" }

"> build libraries: Html library"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\Html\IL2JS_Html.csproj
if (!$?) { throw "BUILD FAILED" }

"> build libraries: Xml library"
&$msbuild $msbuildopts "/p:Configuration=$config" $env:IL2JSROOT\Xml\IL2JS_Xml.csproj
if (!$?) { throw "BUILD FAILED" }

"Build succeeded. Try '.\test run' to check build."
