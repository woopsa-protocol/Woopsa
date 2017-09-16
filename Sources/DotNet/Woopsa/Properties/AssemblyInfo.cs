using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


#if NETCORE2
    [assembly: AssemblyTitle("Woopsa for .NET Core 2.0")]
#elif NET4_0
    [assembly: AssemblyTitle("NLog for .NET Framework 4")]
#elif NET4_5
    [assembly: AssemblyTitle("NLog for .NET Framework 4.5")]
#else
#error Unrecognized build target - please update AssemblyInfo.cs
#endif

[assembly: AssemblyDescription("Woopsa is a protocol that's simple, lightweight, free, open-source, web and object-oriented, publish-subscribe, real-time capable and Industry 4.0 ready. It contributes to the revolution of the Internet of Things")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Objectis SA")]
[assembly: AssemblyCopyright("Copyright © Objectis SA 2017")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.2.0.0")]