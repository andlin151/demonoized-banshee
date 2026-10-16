//
// NativeLibraryMap.cs
//
// Replacement for Mono's <dllmap> configuration, which .NET does not support.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Hyena
{
    public static class NativeLibraryMap
    {
        // Library names used in [DllImport] across Hyena, Banshee and their managed
        // dependencies, mapped to the sonames found on Linux. Only consulted after
        // the runtime's own probing fails, e.g. for "libc" (.NET would load libc.so,
        // which is a linker script on glibc) or "intl" (gettext lives in glibc).
        private static readonly Dictionary<string, string> linux_map = new Dictionary<string, string> {
            { "libc",                       "libc.so.6" },
            { "intl",                       "libc.so.6" },
            { "libintl",                    "libc.so.6" },
            { "sqlite3",                    "libsqlite3.so.0" },
            { "libglib-2.0-0.dll",          "libglib-2.0.so.0" },
            { "libgobject-2.0-0.dll",       "libgobject-2.0.so.0" },
            { "libgio-2.0-0.dll",           "libgio-2.0.so.0" },
            { "libgtk-win32-3.0-0.dll",     "libgtk-3.so.0" },
            { "libgdk-win32-3.0-0.dll",     "libgdk-3.so.0" },
            { "libgdk-win32-2.0-0.dll",     "libgdk-3.so.0" },
            { "libgdk_pixbuf-2.0-0.dll",    "libgdk_pixbuf-2.0.so.0" },
            { "libcairo-2.dll",             "libcairo.so.2" },
            { "libpango-1.0-0.dll",         "libpango-1.0.so.0" },
            { "libpangocairo-1.0-0.dll",    "libpangocairo-1.0.so.0" },
            { "libdbus-glib-1-2.dll",       "libdbus-glib-1.so.2" },
            { "libbanshee.dll",             "libbanshee.so" },
        };

        private static bool initialized;

        public static void Add (string dll, string linux_target)
        {
            lock (linux_map) {
                linux_map[dll] = linux_target;
            }
        }

        [ModuleInitializer]
        public static void Initialize ()
        {
            lock (linux_map) {
                if (initialized) {
                    return;
                }
                initialized = true;
            }
            AssemblyLoadContext.Default.ResolvingUnmanagedDll += Resolve;
        }

        private static IntPtr Resolve (Assembly assembly, string name)
        {
            if (!RuntimeInformation.IsOSPlatform (OSPlatform.Linux)) {
                return IntPtr.Zero;
            }

            string target;
            lock (linux_map) {
                if (!linux_map.TryGetValue (name, out target)) {
                    return IntPtr.Zero;
                }
            }

            // Default search paths include the requesting assembly's directory,
            // where private libraries such as libbanshee.so are installed.
            IntPtr handle;
            return NativeLibrary.TryLoad (target, assembly, null, out handle) ? handle : IntPtr.Zero;
        }
    }
}
