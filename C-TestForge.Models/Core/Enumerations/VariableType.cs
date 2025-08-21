using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.Enumerations
{
    /// <summary>
    /// Type of a variable in C code
    /// </summary>
    public enum VariableType
    {
        /// <summary>
        /// Void type
        /// </summary>
        Void,

        /// <summary>
        /// Boolean type
        /// </summary>
        Bool,

        /// <summary>
        /// Character type (signed or unsigned depending on platform)
        /// </summary>
        Char,

        /// <summary>
        /// Signed character type
        /// </summary>
        SignedChar,

        /// <summary>
        /// Unsigned character type
        /// </summary>
        UnsignedChar,

        /// <summary>
        /// Short integer type
        /// </summary>
        Short,

        /// <summary>
        /// Unsigned short integer type
        /// </summary>
        UnsignedShort,

        /// <summary>
        /// Integer type
        /// </summary>
        Int,

        /// <summary>
        /// Unsigned integer type
        /// </summary>
        UnsignedInt,

        /// <summary>
        /// Long integer type
        /// </summary>
        Long,

        /// <summary>
        /// Unsigned long integer type
        /// </summary>
        UnsignedLong,

        /// <summary>
        /// Long long integer type
        /// </summary>
        LongLong,

        /// <summary>
        /// Unsigned long long integer type
        /// </summary>
        UnsignedLongLong,

        /// <summary>
        /// Single precision floating point type
        /// </summary>
        Float,

        /// <summary>
        /// Double precision floating point type
        /// </summary>
        Double,

        /// <summary>
        /// Extended precision floating point type
        /// </summary>
        LongDouble,

        /// <summary>
        /// Pointer type
        /// </summary>
        Pointer,

        /// <summary>
        /// Array type
        /// </summary>
        Array,

        /// <summary>
        /// Function type
        /// </summary>
        Function,

        /// <summary>
        /// Structure type
        /// </summary>
        Struct,

        /// <summary>
        /// Union type
        /// </summary>
        Union,

        /// <summary>
        /// Enumeration type
        /// </summary>
        Enum,

        /// <summary>
        /// Typedef type
        /// </summary>
        Typedef,

        /// <summary>
        /// Generic primitive type (deprecated, use specific types)
        /// </summary>
        [Obsolete("Use specific primitive types instead")]
        Primitive,

        /// <summary>
        /// Generic integer type (deprecated, use specific integer types)
        /// </summary>
        [Obsolete("Use specific integer types instead")]
        Integer,

        /// <summary>
        /// Legacy unsigned char alias (deprecated, use UnsignedChar)
        /// </summary>
        [Obsolete("Use UnsignedChar instead")]
        UChar,

        /// <summary>
        /// Unknown or unresolved type
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Extension methods for VariableType enum
    /// </summary>
    public static class VariableTypeExtensions
    {
        /// <summary>
        /// Checks if the variable type is a signed integer type
        /// </summary>
        public static bool IsSignedInteger(this VariableType type)
        {
            return type switch
            {
                VariableType.SignedChar or
                VariableType.Short or
                VariableType.Int or
                VariableType.Long or
                VariableType.LongLong => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if the variable type is an unsigned integer type
        /// </summary>
        public static bool IsUnsignedInteger(this VariableType type)
        {
            return type switch
            {
                VariableType.UnsignedChar or
                VariableType.UnsignedShort or
                VariableType.UnsignedInt or
                VariableType.UnsignedLong or
                VariableType.UnsignedLongLong => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if the variable type is any integer type (signed or unsigned)
        /// </summary>
        public static bool IsInteger(this VariableType type)
        {
            return IsSignedInteger(type) || IsUnsignedInteger(type);
        }

        /// <summary>
        /// Checks if the variable type is a floating point type
        /// </summary>
        public static bool IsFloatingPoint(this VariableType type)
        {
            return type switch
            {
                VariableType.Float or
                VariableType.Double or
                VariableType.LongDouble => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if the variable type is a character type
        /// </summary>
        public static bool IsCharacter(this VariableType type)
        {
            return type switch
            {
                VariableType.Char or
                VariableType.SignedChar or
                VariableType.UnsignedChar => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if the variable type is a composite type (struct, union, array)
        /// </summary>
        public static bool IsComposite(this VariableType type)
        {
            return type switch
            {
                VariableType.Struct or
                VariableType.Union or
                VariableType.Array => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if the variable type is a user-defined type
        /// </summary>
        public static bool IsUserDefined(this VariableType type)
        {
            return type switch
            {
                VariableType.Struct or
                VariableType.Union or
                VariableType.Enum or
                VariableType.Typedef => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets the typical size in bytes for the variable type (platform dependent)
        /// </summary>
        public static int GetTypicalSize(this VariableType type)
        {
            return type switch
            {
                VariableType.Bool => 1,
                VariableType.Char or VariableType.SignedChar or VariableType.UnsignedChar => 1,
                VariableType.Short or VariableType.UnsignedShort => 2,
                VariableType.Int or VariableType.UnsignedInt => 4,
                VariableType.Long or VariableType.UnsignedLong => IntPtr.Size, // 4 on 32-bit, 8 on 64-bit
                VariableType.LongLong or VariableType.UnsignedLongLong => 8,
                VariableType.Float => 4,
                VariableType.Double => 8,
                VariableType.LongDouble => 16, // Platform dependent
                VariableType.Pointer => IntPtr.Size, // Platform dependent
                _ => 0 // Unknown size
            };
        }

        /// <summary>
        /// Gets the C language keyword for the variable type
        /// </summary>
        public static string GetCKeyword(this VariableType type)
        {
            return type switch
            {
                VariableType.Void => "void",
                VariableType.Bool => "_Bool",
                VariableType.Char => "char",
                VariableType.SignedChar => "signed char",
                VariableType.UnsignedChar => "unsigned char",
                VariableType.Short => "short",
                VariableType.UnsignedShort => "unsigned short",
                VariableType.Int => "int",
                VariableType.UnsignedInt => "unsigned int",
                VariableType.Long => "long",
                VariableType.UnsignedLong => "unsigned long",
                VariableType.LongLong => "long long",
                VariableType.UnsignedLongLong => "unsigned long long",
                VariableType.Float => "float",
                VariableType.Double => "double",
                VariableType.LongDouble => "long double",
                VariableType.Struct => "struct",
                VariableType.Union => "union",
                VariableType.Enum => "enum",
                _ => type.ToString().ToLower()
            };
        }
    }
}