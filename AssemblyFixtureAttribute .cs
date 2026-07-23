using System;

namespace Unified.Xunit.Extensions
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class AssemblyFixtureAttribute : Attribute
    {
        public Type FixtureType { get; }

        public AssemblyFixtureAttribute(Type fixtureType)
        {
            FixtureType = fixtureType ?? throw new ArgumentNullException(nameof(fixtureType));
        }
    }
}