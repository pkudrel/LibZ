using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;

namespace LibZ.Injected
{
    /// <summary>
    /// </summary>
    /// <summary>
    /// AsmZResolver. Mini resolver getting assemblies straight from resources.
    /// </summary>
    internal class AsmZResolver
    {
        /// <summary>Trace key path.</summary>
        public const string REGISTRY_KEY_PATH = @"Software\Softpark\LibZ";

        /// <summary>Trace key name.</summary>
        public const string REGISTRY_KEY_NAME = @"Trace";


        /// <summary>The resource name regular expression.</summary>
        private static readonly Regex ResourceNameRx = new Regex(
            @"asmz://(?<guid>[0-9a-fA-F]{32})/(?<size>[0-9]+)(/(?<flags>[a-zA-Z0-9]*))?",
            RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        /// <summary>The 'this' assembly (please note, this type is going to be embedded into other assemblies)</summary>
        private static readonly Assembly ThisAssembly = typeof(AsmZResolver).Assembly;

        /// <summary>This assembly short name (for debugging).</summary>
        private static readonly string ThisAssemblyName = ThisAssembly.GetName().Name;

        /// <summary>Hash of 'this' assembly name.</summary>
        private static readonly Guid ThisAssemblyGuid = Hash(ThisAssembly.FullName);


        /// <summary>The initialized flag.</summary>
        private static int _initialized;

        /// <summary>The resource names found in 'this' assembly.</summary>
        private static readonly Dictionary<Guid, Match> ResourceNames
            = new Dictionary<Guid, Match>();

        /// <summary>The loaded assemblies cache.</summary>
        private static readonly Dictionary<Guid, Assembly> LoadedAssemblies =
            new Dictionary<Guid, Assembly>();

        /// <summary>The loaded assemblies cache.</summary>
        /// <summary>Flag indicating if Trace should be used.</summary>
        private static readonly bool UseTrace;


        // private static readonly List<string> Data = new List<string>() ;
        private static readonly
            List<(string Name, string PublicKeyToken, string Culture, string oldVersion, string newVersion)>
            DependentAssemblies
                = new List<(string, string, string, string, string)>();


        /// <summary>Initializes the <see cref="AsmZResolver" /> class.</summary>
        static AsmZResolver()
        {
            var value =
                SafeGetRegistryDWORD(false, REGISTRY_KEY_PATH, REGISTRY_KEY_NAME) ??
                SafeGetRegistryDWORD(true, REGISTRY_KEY_PATH, REGISTRY_KEY_NAME) ??
                0;
            //UseTrace = value != 0;
            UseTrace = true;
        }

        /// <summary>Initializes resolver.</summary>
        public static void Initialize()
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
                return;


            foreach (var rn in ThisAssembly.GetManifestResourceNames())
            {
               
                if (rn.StartsWith("assemblyBinding://")) ProcessAssemblyBinding(rn);
                var m = ResourceNameRx.Match(rn);
                if (!m.Success) continue;
                var guid = new Guid(m.Groups["guid"].Value);
                if (ResourceNames.ContainsKey(guid))
                    Warn($"Duplicated assembly id '{guid:N}', ignoring.");
                else
                    ResourceNames[guid] = m;
            }

            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver;
        }

        private static void ProcessAssemblyBinding(string resourceName)
        {
            Info($"ProcessAssemblyBinding; Name: {resourceName}");
            using (var stream = ThisAssembly.GetManifestResourceStream(resourceName))
            {
                try
                {
                    if (stream == null) return;
                    var buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    var result = Encoding.UTF8.GetString(buffer);
                    ParseDependentAssembly(result);
                }
                catch (Exception e)
                {
                    Error($"result: {e.Message}");
                }
            }
        }

        public static void ParseDependentAssembly(string t)
        {
            string GetValue(string txt, string name, int start)
            {
                var posToken = txt.IndexOf(name, start, StringComparison.OrdinalIgnoreCase);
                if (posToken < 0) return string.Empty;
                var posStart = txt.IndexOf("\"", posToken, StringComparison.OrdinalIgnoreCase);
                if (posStart < 0) return string.Empty;
                posStart++;
                var posEnd = txt.IndexOf("\"", posStart, StringComparison.OrdinalIgnoreCase);
                var ret = txt.Substring(posStart, posEnd - posStart);
                return ret;
            }

            var pos = 0;
            while (true)
            {
                var start = t.IndexOf("<dependentAssembly>", pos, StringComparison.Ordinal);
                if (start < 0) break;
                var name = GetValue(t, "name", start);
                var publicKeyToken = GetValue(t, "publicKeyToken", start);
                var culture = GetValue(t, "culture", start);
                var oldVersion = GetValue(t, "oldVersion", start);
                var newVersion = GetValue(t, "newVersion", start);
                DependentAssemblies.Add((name, publicKeyToken, culture, oldVersion, newVersion));
                var end = t.IndexOf("</dependentAssembly>", start, StringComparison.Ordinal);
                if (end < 0) break;
                pos = end;
            }
        }


        /// <summary>
        /// Gets bool value from registry. Note this is a wropper to
        /// isolate access to Registry class which might be a problem on Mono.
        /// </summary>
        /// <param name="machine">
        /// if set to <c>true</c> "local machine" registry root is used;
        /// "current user" otherwise.
        /// </param>
        /// <param name="path">The path to key.</param>
        /// <param name="name">The name of value.</param>
        /// <returns>Value of given... value.</returns>
        private static uint? SafeGetRegistryDWORD(bool machine, string path, string name)
        {
            try
            {
                return GetRegistryDWORD(machine, path, name);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Gets bool value from registry.</summary>
        /// <param name="machine">
        /// if set to <c>true</c> "local machine" registry root is used;
        /// "current user" otherwise.
        /// </param>
        /// <param name="path">The path to key.</param>
        /// <param name="name">The name of value.</param>
        /// <returns>Value of given... value.</returns>
        private static uint? GetRegistryDWORD(bool machine, string path, string name)
        {
            var root = machine ? Registry.LocalMachine : Registry.CurrentUser;

            var key = root.OpenSubKey(path, false);

            var value = key?.GetValue(name);
            if (value == null)
                return null;

            try
            {
                return Convert.ToUInt32(value);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Assembly resolver.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="ResolveEventArgs" /> instance containing the event data.</param>
        /// <returns>Loaded assembly or <c>null</c>.</returns>
        private static Assembly AssemblyResolver(object sender, ResolveEventArgs args)
        {
            Debug($"Resolving: '{args.Name}'");

            var name = args.Name;
            var result =
                TryLoadAssembly((IntPtr.Size == 4 ? "x86:" : "x64:") + name) ??
                TryLoadAssembly(name) ??
                TryLoadAssembly((IntPtr.Size == 4 ? "x64:" : "x86:") + name);

            if (result != null)
                Debug($"Found: '{args.Name}'");
            else
            {
                Debug($"Not found: '{args.Name}'");
                
            }
               

            return result;
        }

        /// <summary>Tries the load assembly.</summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <returns>Loaded assembly or <c>null</c>.</returns>
        private static Assembly TryLoadAssembly(string resourceName)
        {
            try
            {
                var guid = Hash(resourceName);
                Match match;
                if (!ResourceNames.TryGetValue(guid, out match))
                {
                    return null;
                }

                lock (LoadedAssemblies)
                {
                    Assembly cached;
                    if (LoadedAssemblies.TryGetValue(guid, out cached)) return cached;
                }

                Debug($"Trying to load '{resourceName}'");
                resourceName = match.Groups[0].Value;
                var flags = match.Groups["flags"].Value;
                var size = int.Parse(match.Groups["size"].Value);
                var compressed = flags.Contains("z");
                var unmanaged = flags.Contains("u");
                var portable = flags.Contains("p");

                var buffer = new byte[size];

                using (var rstream = ThisAssembly.GetManifestResourceStream(resourceName))
                {
                    if (rstream == null) return null;
                    using (var zstream = compressed ? new DeflateStream(rstream, CompressionMode.Decompress) : rstream)
                    {
                        zstream.Read(buffer, 0, size);
                    }
                }

                var loaded = unmanaged || portable
                    ? LoadUnmanagedAssembly(resourceName, guid, buffer)
                    : Assembly.Load(buffer);

                lock (LoadedAssemblies)
                {
                    Assembly cached;
                    if (LoadedAssemblies.TryGetValue(guid, out cached)) return cached;
                    if (loaded != null) LoadedAssemblies[guid] = loaded;
                }

                return loaded;
            }
            catch (Exception e)
            {
                Error($"{e.GetType().Name}: {e.Message}");
                return null;
            }
        }

        /// <summary>Loads the unmanaged assembly.</summary>
        /// <param name="resourceName">Name of the assembly.</param>
        /// <param name="guid">The GUID.</param>
        /// <param name="assemblyImage">The assembly binary image.</param>
        /// <returns>Loaded assembly or <c>null</c>.</returns>
        private static Assembly LoadUnmanagedAssembly(string resourceName, Guid guid, byte[] assemblyImage)
        {
            Debug($"Trying to load as unmanaged/portable assembly '{resourceName}'");

            var folderPath = Path.Combine(
                Path.GetTempPath(),
                ThisAssemblyGuid.ToString("N"));
            Directory.CreateDirectory(folderPath);
            var filePath = Path.Combine(folderPath, string.Format("{0:N}.dll", guid));
            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists || fileInfo.Length != assemblyImage.Length)
                File.WriteAllBytes(filePath, assemblyImage);

            return Assembly.LoadFrom(filePath);
        }

        /// <summary>Calculates hash of given text (usually assembly name).</summary>
        /// <param name="text">The text.</param>
        /// <returns>A hash.</returns>
        private static Guid Hash(string text)
        {
            return new Guid(
                MD5.Create().ComputeHash(
                    Encoding.UTF8.GetBytes(
                        text.ToLowerInvariant())));
        }

        private static void Debug(string message)
        {
            if (message != null && UseTrace)
                Trace.TraceInformation("INFO (AsmZ/{0}) {1}", ThisAssemblyName, message);
        }

        private static void Warn(string message)
        {
            if (message != null && UseTrace)
                Trace.TraceWarning("WARN (AsmZ/{0}) {1}", ThisAssemblyName, message);
        }


        private static void Info(string message)
        {
            if (message != null)
                Trace.TraceInformation("INFO (AsmZ/{0}) {1}", ThisAssemblyName, message);
        }

        private static void Error(string message)
        {
            if (message != null && UseTrace)
                Trace.TraceError("ERROR (AsmZ/{0}) {1}", ThisAssemblyName, message);
        }
    }
}