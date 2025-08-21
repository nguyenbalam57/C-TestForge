using C_TestForge.Models.CodeAnalysis.BaseClasss;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.CodeAnalysis
{
    // Class cho type information chi tiết
    public class DetailedTypeInfo
    {
        public string CanonicalType { get; set; }
        public string DisplayName { get; set; }
        public bool IsIncomplete { get; set; }
        public bool IsConstQualified { get; set; }
        public bool IsVolatileQualified { get; set; }
        public bool IsRestrictQualified { get; set; }
        public bool IsTriviallyCopyable { get; set; }
        public bool IsTriviallyDestructible { get; set; }
        public bool IsPOD { get; set; }
        public int Alignment { get; set; }
        public int Size { get; set; }
        public Dictionary<string, object> ClangTypeInfo { get; set; }

        public DetailedTypeInfo()
        {
            ClangTypeInfo = new Dictionary<string, object>();
        }

        public override string ToString()
        {
            return DisplayName ?? CanonicalType ?? "unknown";
        }
    }

    // Class đại diện cho kiểu dữ liệu
    public class DataType : CodeElement
    {
        public string BaseType { get; set; }
        public bool IsPointer { get; set; }
        public int PointerLevel { get; set; }
        public bool IsArray { get; set; }
        public List<int> ArrayDimensions { get; set; }
        public bool IsFunction { get; set; }
        public List<TypeQualifier> Qualifiers { get; set; }
        public bool IsUnsigned { get; set; }
        public bool IsLong { get; set; }
        public bool IsShort { get; set; }
        public int SizeInBytes { get; set; }

        // Enhanced with Clang integration
        public DetailedTypeInfo DetailedInfo { get; set; }
        public string ClangTypeId { get; set; }
        public bool IsFromSystemHeader { get; set; }

        public DataType()
        {
            ArrayDimensions = new List<int>();
            Qualifiers = new List<TypeQualifier>();
            DetailedInfo = new DetailedTypeInfo();
        }

        public DataType(string baseType) : this()
        {
            BaseType = baseType;
            Name = baseType;
        }

        public void PopulateFromClangType(Dictionary<string, object> clangTypeInfo)
        {
            if (clangTypeInfo == null) return;

            DetailedInfo = new DetailedTypeInfo();

            if (clangTypeInfo.ContainsKey("qualType"))
            {
                DetailedInfo.CanonicalType = clangTypeInfo["qualType"].ToString();
                QualifiedType = DetailedInfo.CanonicalType;
            }

            if (clangTypeInfo.ContainsKey("isConst"))
                DetailedInfo.IsConstQualified = Convert.ToBoolean(clangTypeInfo["isConst"]);

            if (clangTypeInfo.ContainsKey("isVolatile"))
                DetailedInfo.IsVolatileQualified = Convert.ToBoolean(clangTypeInfo["isVolatile"]);

            if (clangTypeInfo.ContainsKey("size"))
            {
                DetailedInfo.Size = Convert.ToInt32(clangTypeInfo["size"]);
                SizeInBytes = DetailedInfo.Size;
            }

            if (clangTypeInfo.ContainsKey("alignment"))
                DetailedInfo.Alignment = Convert.ToInt32(clangTypeInfo["alignment"]);

            if (clangTypeInfo.ContainsKey("displayName"))
                DetailedInfo.DisplayName = clangTypeInfo["displayName"].ToString();

            DetailedInfo.ClangTypeInfo = clangTypeInfo;

            // Parse qualifiers
            if (DetailedInfo.IsConstQualified && !Qualifiers.Contains(TypeQualifier.Const))
                Qualifiers.Add(TypeQualifier.Const);

            if (DetailedInfo.IsVolatileQualified && !Qualifiers.Contains(TypeQualifier.Volatile))
                Qualifiers.Add(TypeQualifier.Volatile);
        }

        public bool IsBuiltinType()
        {
            var builtinTypes = new[]
            {
                "void", "char", "short", "int", "long", "float", "double",
                "signed", "unsigned", "_Bool", "_Complex", "_Imaginary"
            };
            return builtinTypes.Contains(BaseType);
        }

        public bool IsIntegerType()
        {
            var integerTypes = new[]
            {
                "char", "short", "int", "long", "signed", "unsigned", "_Bool"
            };
            return integerTypes.Any(t => BaseType?.Contains(t) == true);
        }

        public bool IsFloatingType()
        {
            var floatingTypes = new[] { "float", "double", "_Complex", "_Imaginary" };
            return floatingTypes.Any(t => BaseType?.Contains(t) == true);
        }

        public bool IsArithmeticType()
        {
            return IsIntegerType() || IsFloatingType();
        }

        public bool IsScalarType()
        {
            return IsArithmeticType() || IsPointer;
        }

        public bool IsQualified()
        {
            return Qualifiers.Any(q => q != TypeQualifier.None);
        }

        public string GetUnqualifiedType()
        {
            var sb = new StringBuilder();

            if (IsUnsigned) sb.Append("unsigned ");
            if (IsLong) sb.Append("long ");
            if (IsShort) sb.Append("short ");

            sb.Append(BaseType);

            if (IsPointer)
                sb.Append(new string('*', PointerLevel));

            if (IsArray && ArrayDimensions.Any())
            {
                foreach (int dim in ArrayDimensions)
                    sb.Append($"[{(dim > 0 ? dim.ToString() : "")}]");
            }

            return sb.ToString();
        }

        public DataType GetPointedToType()
        {
            if (!IsPointer || PointerLevel <= 0)
                return null;

            var pointedType = new DataType(BaseType)
            {
                IsPointer = PointerLevel > 1,
                PointerLevel = Math.Max(0, PointerLevel - 1),
                IsArray = IsArray,
                ArrayDimensions = new List<int>(ArrayDimensions),
                Qualifiers = new List<TypeQualifier>(Qualifiers),
                IsUnsigned = IsUnsigned,
                IsLong = IsLong,
                IsShort = IsShort,
                SizeInBytes = IntPtr.Size // Pointer size
            };

            return pointedType;
        }

        public DataType GetArrayElementType()
        {
            if (!IsArray || !ArrayDimensions.Any())
                return null;

            var elementType = new DataType(BaseType)
            {
                IsPointer = IsPointer,
                PointerLevel = PointerLevel,
                IsArray = ArrayDimensions.Count > 1,
                ArrayDimensions = ArrayDimensions.Skip(1).ToList(),
                Qualifiers = new List<TypeQualifier>(Qualifiers),
                IsUnsigned = IsUnsigned,
                IsLong = IsLong,
                IsShort = IsShort
            };

            return elementType;
        }

        public override string GetSignature()
        {
            var sb = new StringBuilder();

            if (Qualifiers.Any(q => q != TypeQualifier.None))
                sb.Append(string.Join(" ", Qualifiers.Where(q => q != TypeQualifier.None)
                    .Select(q => q.ToString().ToLower())) + " ");

            if (IsUnsigned) sb.Append("unsigned ");
            if (IsLong) sb.Append("long ");
            if (IsShort) sb.Append("short ");

            sb.Append(BaseType ?? "unknown");

            if (IsPointer)
                sb.Append(new string('*', PointerLevel));

            if (IsArray && ArrayDimensions.Any())
            {
                foreach (int dim in ArrayDimensions)
                    sb.Append($"[{(dim > 0 ? dim.ToString() : "")}]");
            }

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is DataType other)
            {
                return BaseType == other.BaseType &&
                       IsPointer == other.IsPointer &&
                       PointerLevel == other.PointerLevel &&
                       IsArray == other.IsArray &&
                       ArrayDimensions.SequenceEqual(other.ArrayDimensions) &&
                       Qualifiers.SequenceEqual(other.Qualifiers) &&
                       IsUnsigned == other.IsUnsigned &&
                       IsLong == other.IsLong &&
                       IsShort == other.IsShort;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BaseType, IsPointer, PointerLevel, IsArray,
                IsUnsigned, IsLong, IsShort);
        }

        public static DataType Parse(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
                return new DataType("void");

            var dataType = new DataType();
            var cleanType = typeString.Trim();

            // Parse qualifiers
            if (cleanType.Contains("const"))
            {
                dataType.Qualifiers.Add(TypeQualifier.Const);
                cleanType = cleanType.Replace("const", "").Trim();
            }

            if (cleanType.Contains("volatile"))
            {
                dataType.Qualifiers.Add(TypeQualifier.Volatile);
                cleanType = cleanType.Replace("volatile", "").Trim();
            }

            if (cleanType.Contains("restrict"))
            {
                dataType.Qualifiers.Add(TypeQualifier.Restrict);
                cleanType = cleanType.Replace("restrict", "").Trim();
            }

            // Parse modifiers
            if (cleanType.Contains("unsigned"))
            {
                dataType.IsUnsigned = true;
                cleanType = cleanType.Replace("unsigned", "").Trim();
            }

            if (cleanType.Contains("long"))
            {
                dataType.IsLong = true;
                cleanType = cleanType.Replace("long", "").Trim();
            }

            if (cleanType.Contains("short"))
            {
                dataType.IsShort = true;
                cleanType = cleanType.Replace("short", "").Trim();
            }

            // Parse pointers
            if (cleanType.Contains("*"))
            {
                dataType.IsPointer = true;
                dataType.PointerLevel = cleanType.Count(c => c == '*');
                cleanType = cleanType.Replace("*", "").Trim();
            }

            // Parse arrays
            if (cleanType.Contains("["))
            {
                dataType.IsArray = true;
                var arrayParts = cleanType.Split('[');
                cleanType = arrayParts[0].Trim();

                for (int i = 1; i < arrayParts.Length; i++)
                {
                    var dimStr = arrayParts[i].Replace("]", "").Trim();
                    if (int.TryParse(dimStr, out int dimension))
                        dataType.ArrayDimensions.Add(dimension);
                    else
                        dataType.ArrayDimensions.Add(0); // Variable length array
                }
            }

            // Set base type
            dataType.BaseType = cleanType;
            dataType.Name = dataType.GetSignature();

            return dataType;
        }
    }
}
