using System;

namespace ybwork.Analyzer.Universal
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class EnableStaticValueAttribute : Attribute { }
}
