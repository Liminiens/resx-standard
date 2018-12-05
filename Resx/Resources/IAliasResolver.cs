using System.Reflection;

namespace Resx.Resources {
    /// <summary>
    /// Summary of IAliasResolver.
    /// </summary>
    internal interface IAliasResolver {
        AssemblyName ResolveAlias(string alias);
        void PushAlias(string alias, AssemblyName name);
    }
}

