// The game exports IL2CPP versions of these attributes; the C# compiler needs managed byte[].
namespace System.Runtime.CompilerServices;
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
internal sealed class NullableAttribute : Attribute
{
    public readonly byte[] NullableFlags;
    public NullableAttribute(byte flag) { NullableFlags = new[] { flag }; }
    public NullableAttribute(byte[] flags) { NullableFlags = flags; }
}
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
internal sealed class NullableContextAttribute : Attribute
{
    public readonly byte Flag;
    public NullableContextAttribute(byte flag) { Flag = flag; }
}
