using System;

namespace ybwork.Analyzer.Universal
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class PickFieldsAttribute : Attribute
    {
        public readonly Type PickTpe;
        public PickFieldsAttribute(Type type)
        {
            PickTpe = type;
        }
    }
}
