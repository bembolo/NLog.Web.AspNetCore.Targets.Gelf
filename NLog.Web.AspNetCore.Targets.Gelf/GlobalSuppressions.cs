// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1168:Empty arrays and collections should be returned instead of null", Justification = "Returning null instead of an empty JObject makes sense here", Scope = "member", Target = "~M:NLog.Web.AspNetCore.Targets.Gelf.GelfConverter.GetGelfObject(NLog.LogEventInfo)~Newtonsoft.Json.Linq.JObject")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S103:Lines should not be too long", Justification = "This was raised ecause of the SupressMessage attributes in this file...")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "Using inline magic numbers is more readable here.", Scope = "member", Target = "~M:NLog.Web.AspNetCore.Targets.Gelf.UdpTransport.ConstructChunkHeader(System.Byte[],System.Int32,System.Int32)~System.Byte[]")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly", Justification = "It is correctly implemented.", Scope = "type", Target = "~T:NLog.Web.AspNetCore.Targets.Gelf.GelfTarget")]