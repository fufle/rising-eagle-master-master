/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;

#if !PLATFORM_COMPACTFRAMEWORK
using System.Security;
using System.Runtime.ConstrainedExecution;
#endif

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("System.Data.SQLite Core")]
[assembly: AssemblyDescription("ADO.NET Data Provider for SQLite")]
[assembly: AssemblyCompany("https://system.data.sqlite.org/")]
[assembly: AssemblyProduct("System.Data.SQLite")]
[assembly: AssemblyCopyright("Public Domain")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

#if PLATFORM_COMPACTFRAMEWORK && RETARGETABLE
[assembly: AssemblyFlags(AssemblyNameFlags.Retargetable)]
#endif

//  Setting ComVisible to false makes the types in this assembly not visible
//  to COM componenets.  If you need to access a type in this assembly from
//  COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: InternalsVisibleTo("System.Data.SQLite.Linq, PublicKey=" + System.Data.SQLite.SQLite3.PublicKey)]

#if NET_40 || NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
[assembly: InternalsVisibleTo("System.Data.SQLite.EF6, PublicKey=" + System.Data.SQLite.SQLite3.PublicKey)]
#endif

[assembly: NeutralResourcesLanguage("en")]

#if !PLATFORM_COMPACTFRAMEWORK
#if !NET_40 && !NET_45 && !NET_451 && !NET_452 && !NET_46 && !NET_461 && !NET_462 && !NET_47 && !NET_471
[assembly: AllowPartiallyTrustedCallers]
#endif

[assembly: ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]

#if NET_40 || NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
//
// NOTE: This attribute is only available in .NET Framework 4.0 or higher.
//
[assembly: SecurityRules(SecurityRuleSet.Level1)]
#endif
#endif

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers
// by using the '*' as shown below:
[assembly: AssemblyVersion("1.0.108.0")]
#if !PLATFORM_COMPACTFRAMEWORK
[assembly: AssemblyFileVersion("1.0.108.0")]
#endif
