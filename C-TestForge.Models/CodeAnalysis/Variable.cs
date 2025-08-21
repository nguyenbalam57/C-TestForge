using C_TestForge.Models.CodeAnalysis.BaseClasss;
using C_TestForge.Models.CodeAnalysis.ClangASTNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace C_TestForge.Models.CodeAnalysis
{
    // Class cho biến
    public class Variable : CodeElement
    {
        public DataType Type { get; set; }
        public StorageClass StorageClass { get; set; }
        public string InitialValue { get; set; }
        public bool IsInitialized { get; set; }
        public bool IsStatic { get; set; }
        public bool IsExtern { get; set; }
        public bool IsRegister { get; set; }
        public bool IsConstant { get; set; }
        public bool IsVolatile { get; set; }
        public bool IsGlobal { get; set; }
        public int Offset { get; set; } // Offset trong struct/union
        public AccessLevel AccessLevel { get; set; }
        public bool IsParameter { get; set; }
        public bool IsLocal { get; set; }
        public bool IsField { get; set; }
        public int BitFieldWidth { get; set; }
        public bool IsBitField { get; set; }

        public Variable()
        {
            AccessLevel = AccessLevel.Public;
            StorageClass = StorageClass.Auto;
        }

        public Variable(string name, DataType type) : this()
        {
            Name = name;
            Type = type;
        }

        public bool HasInitializer()
        {
            return IsInitialized && !string.IsNullOrEmpty(InitialValue);
        }

        public bool IsAutomatic()
        {
            return StorageClass == StorageClass.Auto && !IsStatic && !IsExtern;
        }

        public bool HasStorageDuration()
        {
            return IsStatic || IsExtern || IsGlobal;
        }

        public bool IsModifiable()
        {
            return !IsConstant && !Type?.Qualifiers?.Contains(TypeQualifier.Const) == true;
        }

        public int GetSize()
        {
            if (Type?.SizeInBytes > 0)
                return Type.SizeInBytes;

            if (Type?.DetailedInfo?.Size > 0)
                return Type.DetailedInfo.Size;

            return 0;
        }

        public override string GetSignature()
        {
            var sb = new StringBuilder();

            // Storage class
            if (StorageClass != StorageClass.Auto)
                sb.Append(StorageClass.ToString().ToLower() + " ");

            // Type signature
            if (Type != null)
                sb.Append(Type.GetSignature());
            else
                sb.Append("unknown");

            sb.Append(" " + Name);

            // Bit field
            if (IsBitField && BitFieldWidth > 0)
                sb.Append(" : " + BitFieldWidth);

            // Initial value
            if (HasInitializer())
                sb.Append(" = " + InitialValue);

            return sb.ToString();
        }

        public override void PopulateFromASTNode(ClangASTNode node)
        {
            base.PopulateFromASTNode(node);

            if (node == null) return;

            // Extract variable-specific information
            if (node.HasProperty("storageClass"))
            {
                var storageClassStr = node.GetProperty<string>("storageClass");
                if (Enum.TryParse<StorageClass>(storageClassStr, true, out var storageClass))
                    StorageClass = storageClass;

                IsStatic = storageClassStr == "static";
                IsExtern = storageClassStr == "extern";
                IsRegister = storageClassStr == "register";
            }

            if (node.HasProperty("init"))
            {
                IsInitialized = true;
                InitialValue = node.GetProperty<string>("init");
            }

            // Determine variable scope
            var parent = node.Parent;
            if (parent != null)
            {
                switch (parent.Kind)
                {
                    case ClangASTNodeKind.TranslationUnitDecl:
                        IsGlobal = true;
                        Scope = ScopeType.Global;
                        break;
                    case ClangASTNodeKind.FunctionDecl:
                        IsLocal = true;
                        Scope = ScopeType.FunctionScope;
                        break;
                    case ClangASTNodeKind.RecordDecl:
                        IsField = true;
                        Scope = ScopeType.BlockScope;
                        break;
                    case ClangASTNodeKind.CompoundStmt:
                        IsLocal = true;
                        Scope = ScopeType.BlockScope;
                        break;
                }
            }

            // Parse type information
            if (!string.IsNullOrEmpty(node.QualType))
            {
                Type = DataType.Parse(node.QualType);

                // Check for const/volatile qualifiers
                IsConstant = node.QualType.Contains("const");
                IsVolatile = node.QualType.Contains("volatile");
            }
        }

        public override string ToString()
        {
            var scope = IsGlobal ? "global" : IsLocal ? "local" : IsField ? "field" : "unknown";
            return $"{scope} variable: {GetSignature()}";
        }
    }

    // Class cho tham số hàm
    public class Parameter : CodeElement
    {
        public DataType Type { get; set; }
        public string DefaultValue { get; set; }
        public bool HasDefaultValue { get; set; }
        public bool IsConst { get; set; }
        public bool IsVolatile { get; set; }
        public bool IsRestrict { get; set; }
        public int Position { get; set; }
        public bool IsVariadic { get; set; }
        public bool IsThis { get; set; } // For C++ compatibility
        public Function OwningFunction { get; set; }

        public Parameter()
        {
            Position = -1;
        }

        public Parameter(string name, DataType type, int position = -1) : this()
        {
            Name = name;
            Type = type;
            Position = position;
        }

        public bool IsPointerParameter()
        {
            return Type?.IsPointer == true;
        }

        public bool IsArrayParameter()
        {
            return Type?.IsArray == true;
        }

        public bool IsFunctionPointerParameter()
        {
            return Type?.IsFunction == true;
        }

        public bool IsOutputParameter()
        {
            // Heuristic: pointer parameters (except const) are often output parameters
            return IsPointerParameter() && !IsConst;
        }

        public bool IsInputParameter()
        {
            // Heuristic: const parameters or non-pointer parameters are often input parameters
            return IsConst || !IsPointerParameter();
        }

        public bool IsInputOutputParameter()
        {
            // Non-const pointer parameters can be both input and output
            return IsPointerParameter() && !IsConst;
        }

        public override string GetSignature()
        {
            var sb = new StringBuilder();

            if (Type != null)
                sb.Append(Type.GetSignature());
            else
                sb.Append("unknown");

            if (!string.IsNullOrEmpty(Name))
                sb.Append(" " + Name);

            if (HasDefaultValue && !string.IsNullOrEmpty(DefaultValue))
                sb.Append(" = " + DefaultValue);

            return sb.ToString();
        }

        public override void PopulateFromASTNode(ClangASTNode node)
        {
            base.PopulateFromASTNode(node);

            if (node == null) return;

            // Parse type information
            if (!string.IsNullOrEmpty(node.QualType))
            {
                Type = DataType.Parse(node.QualType);

                // Check qualifiers
                IsConst = node.QualType.Contains("const");
                IsVolatile = node.QualType.Contains("volatile");
                IsRestrict = node.QualType.Contains("restrict");
            }

            // Extract default value if present
            if (node.HasProperty("hasDefaultArg") && node.GetProperty<bool>("hasDefaultArg"))
            {
                HasDefaultValue = true;
                DefaultValue = node.GetProperty<string>("defaultArg", "");
            }

            // Determine position from parent function
            var functionNode = node.FindParent(ClangASTNodeKind.FunctionDecl);
            if (functionNode != null)
            {
                var parameters = functionNode.Children.Where(c => c.Kind == ClangASTNodeKind.ParmVarDecl).ToList();
                Position = parameters.IndexOf(node);
            }
        }

        public Variable ToVariable()
        {
            return new Variable(Name, Type)
            {
                IsParameter = true,
                IsConstant = IsConst,
                IsVolatile = IsVolatile,
                SourceRange = SourceRange,
                ClangId = ClangId,
                ASTNode = ASTNode,
                Scope = ScopeType.FunctionScope
            };
        }

        public override string ToString()
        {
            return $"parameter[{Position}]: {GetSignature()}";
        }

        public override bool Equals(object obj)
        {
            if (obj is Parameter other)
            {
                return Name == other.Name &&
                       Position == other.Position &&
                       Type?.Equals(other.Type) == true &&
                       OwningFunction?.Name == other.OwningFunction?.Name;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Position, Type, OwningFunction?.Name);
        }
    }

    // Helper class cho parameter analysis
    public static class ParameterAnalyzer
    {
        public static bool IsLikelyOutputParameter(Parameter param)
        {
            if (param == null || param.Type == null) return false;

            // Pointer to non-const data
            return param.Type.IsPointer &&
                   !param.IsConst &&
                   !param.Type.Qualifiers.Contains(TypeQualifier.Const);
        }

        public static bool IsLikelyInputParameter(Parameter param)
        {
            if (param == null || param.Type == null) return false;

            // Const parameters or value parameters
            return param.IsConst ||
                   param.Type.Qualifiers.Contains(TypeQualifier.Const) ||
                   (!param.Type.IsPointer && !param.Type.IsArray);
        }

        public static bool IsLikelyStringParameter(Parameter param)
        {
            if (param == null || param.Type == null) return false;

            // char* or const char*
            return param.Type.IsPointer &&
                   param.Type.BaseType == "char";
        }

        public static bool IsLikelyArrayParameter(Parameter param)
        {
            if (param == null || param.Type == null) return false;

            // Array or pointer (arrays decay to pointers in function parameters)
            return param.Type.IsArray ||
                   (param.Type.IsPointer && param.Type.PointerLevel == 1);
        }

        public static bool IsLikelyCallbackParameter(Parameter param)
        {
            if (param == null || param.Type == null) return false;

            // Function pointer
            return param.Type.IsFunction ||
                   (param.Type.IsPointer && param.Type.IsFunction);
        }
    }
}
