using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using Resx.Utility;

namespace Resx.Resources
{
    internal class AssemblyNamesTypeResolutionService : ITypeResolutionService
    {
        private AssemblyName[] names;

        private readonly ConcurrentDictionary<AssemblyName, Assembly> cachedAssemblies =
            new ConcurrentDictionary<AssemblyName, Assembly>();

        private readonly ConcurrentDictionary<string, Type> cachedTypes =
            new ConcurrentDictionary<string, Type>();

        internal AssemblyNamesTypeResolutionService(AssemblyName[] names)
        {
            this.names = names;
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public Assembly GetAssembly(AssemblyName name)
        {
            return GetAssembly(name, true);
        }

        //
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public Assembly GetAssembly(AssemblyName name, bool throwOnError)
        {
            if (cachedAssemblies.TryGetValue(name, out var asm))
            {
                return asm;
            }

            Assembly result = null;

            // try to load it first from the gac
#pragma warning disable 0618
            //Although LoadWithPartialName is obsolete, we still have to call it: changing 
            //this would be breaking in cases where people edited their resource files by
            //hand.
            result = Assembly.LoadWithPartialName(name.FullName);
#pragma warning restore 0618
            if (result != null)
            {
                cachedAssemblies[name] = result;
            }
            else if (names != null)
            {
                foreach (var asmName in names)
                {
                    if (name.Equals(asmName))
                    {
                        try
                        {
                            result = Assembly.LoadFrom(GetPathOfAssembly(name));
                            cachedAssemblies[name] = result;
                        }
                        catch
                        {
                            if (throwOnError)
                            {
                                throw;
                            }
                        }
                    }
                }
            }

            return result;
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public string GetPathOfAssembly(AssemblyName name)
        {
            return name.CodeBase;
        }

        public Type GetType(string name)
        {
            return GetType(name, true);
        }

        public Type GetType(string name, bool throwOnError)
        {
            return GetType(name, throwOnError, false);
        }

        public Type GetType(string name, bool throwOnError, bool ignoreCase)
        {
            if (cachedTypes.TryGetValue(name, out var type))
            {
                return type;
            }

            Type result = null;

            // Missed in cache, try to resolve the type. First try to resolve in the GAC
            if (name.IndexOf(',') != -1)
            {
                result = Type.GetType(name, false, ignoreCase);
            }

            //
            // Did not find it in the GAC, check the reference assemblies
            if (result == null && names != null)
            {
                //
                // If the type is assembly qualified name, we sort the assembly names
                // to put assemblies with same name in the front so that they can
                // be searched first.
                int pos = name.IndexOf(',');
                if (pos > 0 && pos < name.Length - 1)
                {
                    string fullName = name.Substring(pos + 1).Trim();
                    AssemblyName assemblyName = null;
                    try
                    {
                        assemblyName = new AssemblyName(fullName);
                    }
                    catch
                    {
                    }

                    if (assemblyName != null)
                    {
                        List<AssemblyName> assemblyList = new List<AssemblyName>(names.Length);
                        for (int i = 0; i < names.Length; i++)
                        {
                            if (string.Compare(assemblyName.Name, names[i].Name, StringComparison.OrdinalIgnoreCase) ==
                                0)
                            {
                                assemblyList.Insert(0, names[i]);
                            }
                            else
                            {
                                assemblyList.Add(names[i]);
                            }
                        }

                        names = assemblyList.ToArray();
                    }
                }

                // Search each reference assembly
                for (int i = 0; i < names.Length; i++)
                {
                    Assembly a = GetAssembly(names[i], false);
                    if (a != null)
                    {
                        result = a.GetType(name, false, ignoreCase);
                        if (result == null)
                        {
                            int indexOfComma = name.IndexOf(",");
                            if (indexOfComma != -1)
                            {
                                string shortName = name.Substring(0, indexOfComma);
                                result = a.GetType(shortName, false, ignoreCase);
                            }
                        }
                    }

                    if (result != null)
                        break;
                }
            }

            if (result == null && throwOnError)
            {
                throw new ArgumentException(string.Format(SR.InvalidResXNoType, name));
            }

            if (result != null)
            {
                cachedTypes.TryAdd(name, result);
            }

            return result;
        }

        public void ReferenceAssembly(AssemblyName name)
        {
            throw new NotSupportedException();
        }
    }
}