namespace System.Runtime.CompilerServices { internal static class IsExternalInit {} }

namespace System
{
    internal readonly struct Index
    {
        private readonly int _value; 
        public Index(int value, bool fromEnd = false) => _value = fromEnd ? ~value : value; 
        public int Value => _value < 0 ? ~_value : _value; 
        public bool IsFromEnd => _value < 0; 
        public int GetOffset(int length) => _value < 0 ? length + ~_value : _value;
    }
}