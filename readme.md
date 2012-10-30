# IL2JS - An Intermediate Language to JavaScript Compiler #

----------

Compile MSIL to JavaScript without changing program behavior and by extension compile any .Net language into JavaScript without changing program behaviour.

Compare with:

- Script#: Type check and translate a C#-like language as JavaScript

- Silverlight: Run MSIL in a CLR embedded within the browser 

IL2JS supports all .NET 3.5 features except:

- P/Invoke, native methods, unsafe code
- Unsigned and 64-bit integers (always interpreted as doubles)
- Variance on type parameters other than in IEnumerable

The key highlights are:

- **No change** required to Visual Studio, source compilers, existing toolchain, existing managed debugger

- **No change** to target browser or script host

- **No plugins**

# Getting Started #

Check the HOW\_TO\_BUILD.txt file for instructions on how to build IL2JS.

# LICENSE #

----------


**Copyright 2011 Microsoft Corporation**

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at [http://www.apache.org/licenses/LICENSE-2.0](http://www.apache.org/licenses/LICENSE-2.0)

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.