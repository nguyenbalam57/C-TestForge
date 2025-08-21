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
    // Class cho thành viên của enum
    public class EnumMember : CodeElement
    {
        public int Value { get; set; }
        public bool HasExplicitValue { get; set; }
        public string Expression { get; set; }
        public EnumDefinition OwningEnum { get; set; }

        public EnumMember()
        {
        }

        public EnumMember(string name, int value, bool hasExplicitValue = false) : this()
        {
            Name = name;
            Value = value;
            HasExplicitValue = hasExplicitValue;
        }

        public bool IsDefault()
        {
            return !HasExplicitValue;
        }

        public override string GetSignature()
        {
            if (HasExplicitValue)
                return $"{Name} = {(string.IsNullOrEmpty(Expression) ? Value.ToString() : Expression)}";
            else
                return Name;
        }

        public override void PopulateFromASTNode(ClangASTNode node)
        {
            base.PopulateFromASTNode(node);

            if (node == null) return;

            if (node.HasProperty("value"))
            {
                Value = node.GetProperty<int>("value");
                HasExplicitValue = true;
            }

            if (node.HasProperty("expr"))
            {
                Expression = node.GetProperty<string>("expr");
                HasExplicitValue = true;
            }
        }

        public override string ToString()
        {
            return $"enum constant: {GetSignature()}";
        }
    }

    // Class cho Enum
    public class EnumDefinition : CodeElement
    {
        public List<EnumMember> Members { get; set; }
        public DataType UnderlyingType { get; set; }
        public bool IsAnonymous { get; set; }
        public bool IsScoped { get; set; } // For C++ enum class
        public int MemberCount => Members?.Count ?? 0;
        public int MinValue { get; private set; }
        public int MaxValue { get; private set; }
        public bool IsFlags { get; set; }
        public bool IsSequential { get; private set; }

        public EnumDefinition()
        {
            Members = new List<EnumMember>();
            UnderlyingType = new DataType("int"); // Default underlying type
            IsSequential = true;
        }

        public EnumDefinition(string name) : this()
        {
            Name = name;
            IsAnonymous = string.IsNullOrEmpty(name);
        }

        public void AddMember(EnumMember member)
        {
            if (member != null && !Members.Contains(member))
            {
                member.OwningEnum = this;
                Members.Add(member);
                UpdateValueRange();
                CheckSequential();
                CheckFlags();
            }
        }

        public EnumMember GetMember(string memberName)
        {
            return Members.FirstOrDefault(m => m.Name == memberName);
        }

        public EnumMember GetMemberByValue(int value)
        {
            return Members.FirstOrDefault(m => m.Value == value);
        }

        public bool HasMember(string memberName)
        {
            return Members.Any(m => m.Name == memberName);
        }

        public bool HasValue(int value)
        {
            return Members.Any(m => m.Value == value);
        }

        private void UpdateValueRange()
        {
            if (!Members.Any())
            {
                MinValue = MaxValue = 0;
                return;
            }

            MinValue = Members.Min(m => m.Value);
            MaxValue = Members.Max(m => m.Value);
        }

        private void CheckSequential()
        {
            if (Members.Count < 2)
            {
                IsSequential = true;
                return;
            }

            var sortedValues = Members.Select(m => m.Value).OrderBy(v => v).ToList();
            IsSequential = true;

            for (int i = 1; i < sortedValues.Count; i++)
            {
                if (sortedValues[i] != sortedValues[i - 1] + 1)
                {
                    IsSequential = false;
                    break;
                }
            }
        }

        private void CheckFlags()
        {
            if (Members.Count < 2)
            {
                IsFlags = false;
                return;
            }

            // Check if values are powers of 2 (flag enum pattern)
            IsFlags = Members.All(m => m.Value == 0 || (m.Value > 0 && (m.Value & (m.Value - 1)) == 0));
        }

        public List<EnumMember> GetExplicitMembers()
        {
            return Members.Where(m => m.HasExplicitValue).ToList();
        }

        public List<EnumMember> GetImplicitMembers()
        {
            return Members.Where(m => !m.HasExplicitValue).ToList();
        }

        public bool IsEmpty()
        {
            return !Members.Any();
        }

        public int GetValueRange()
        {
            return MaxValue - MinValue;
        }

        public override string GetSignature()
        {
            var members = string.Join(", ", Members.Select(m => m.GetSignature()));
            var enumName = IsAnonymous ? "" : Name;
            return $"enum {enumName} {{ {members} }}";
        }

        public override void PopulateFromASTNode(ClangASTNode node)
        {
            base.PopulateFromASTNode(node);

            if (node == null) return;

            IsAnonymous = string.IsNullOrEmpty(Name);

            // Extract underlying type if specified
            if (node.HasProperty("fixedUnderlyingType"))
            {
                var underlyingTypeStr = node.GetProperty<string>("fixedUnderlyingType");
                UnderlyingType = DataType.Parse(underlyingTypeStr);
            }

            // Extract enum constants
            var constNodes = node.Children.Where(c => c.Kind == ClangASTNodeKind.EnumConstantDecl);
            int implicitValue = 0;

            foreach (var constNode in constNodes)
            {
                var member = new EnumMember();
                member.PopulateFromASTNode(constNode);

                if (!member.HasExplicitValue)
                {
                    member.Value = implicitValue;
                }

                implicitValue = member.Value + 1;
                AddMember(member);
            }
        }

        public override string ToString()
        {
            var anonymity = IsAnonymous ? "anonymous" : "named";
            var pattern = IsFlags ? "flags" : IsSequential ? "sequential" : "custom";
            return $"{anonymity} enum ({pattern}): {Name} ({MemberCount} members)";
        }

        public override bool Equals(object obj)
        {
            if (obj is EnumDefinition other)
            {
                return Name == other.Name &&
                       Members.Count == other.Members.Count &&
                       Members.SequenceEqual(other.Members);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Members.Count);
        }
    }

    // Class cho Define/Macro
    public class MacroDefinition : CodeElement
    {
        public string Value { get; set; }
        public List<string> Parameters { get; set; }
        public bool IsFunctionLike { get; set; }
        public bool IsConditional { get; set; }
        public string Condition { get; set; }
        public bool IsMultiLine { get; set; }
        public int ParameterCount => Parameters?.Count ?? 0;
        public MacroCategory Category { get; private set; }
        public List<string> UsedMacros { get; set; }

        public enum MacroCategory
        {
            Constant,
            FunctionLike,
            Conditional,
            HeaderGuard,
            Configuration,
            Debug,
            Utility
        }

        public MacroDefinition()
        {
            Parameters = new List<string>();
            UsedMacros = new List<string>();
            DetermineCategory();
        }

        public MacroDefinition(string name, string value) : this()
        {
            Name = name;
            Value = value;
            DetermineCategory();
        }

        public void AddParameter(string parameter)
        {
            if (!string.IsNullOrEmpty(parameter) && !Parameters.Contains(parameter))
            {
                Parameters.Add(parameter);
                IsFunctionLike = Parameters.Any();
                DetermineCategory();
            }
        }

        public bool HasParameter(string parameter)
        {
            return Parameters.Contains(parameter);
        }

        public bool IsConstantMacro()
        {
            return !IsFunctionLike && !string.IsNullOrEmpty(Value) && !IsConditional;
        }

        public bool IsHeaderGuard()
        {
            return Name?.EndsWith("_H") == true || Name?.EndsWith("_HPP") == true ||
                   Name?.EndsWith("_INCLUDED") == true;
        }

        public bool IsDebugMacro()
        {
            var debugNames = new[] { "DEBUG", "NDEBUG", "ASSERT", "TRACE", "LOG" };
            return debugNames.Any(d => Name?.Contains(d) == true);
        }

        public bool IsConfigurationMacro()
        {
            var configNames = new[] { "CONFIG", "ENABLE", "DISABLE", "VERSION", "PLATFORM" };
            return configNames.Any(c => Name?.Contains(c) == true);
        }

        private void DetermineCategory()
        {
            if (IsConditional)
                Category = MacroCategory.Conditional;
            else if (IsHeaderGuard())
                Category = MacroCategory.HeaderGuard;
            else if (IsDebugMacro())
                Category = MacroCategory.Debug;
            else if (IsConfigurationMacro())
                Category = MacroCategory.Configuration;
            else if (IsFunctionLike)
                Category = MacroCategory.FunctionLike;
            else if (IsConstantMacro())
                Category = MacroCategory.Constant;
            else
                Category = MacroCategory.Utility;
        }

        public override string GetSignature()
        {
            if (IsFunctionLike)
                return $"#define {Name}({string.Join(", ", Parameters)}) {Value}";
            else
                return $"#define {Name} {Value}";
        }

        public override void PopulateFromASTNode(ClangASTNode node)
        {
            base.PopulateFromASTNode(node);

            if (node == null) return;

            if (node.HasProperty("isFunctionLike"))
            {
                IsFunctionLike = node.GetProperty<bool>("isFunctionLike");
            }

            if (node.HasProperty("parameters"))
            {
                var paramStr = node.GetProperty<string>("parameters");
                if (!string.IsNullOrEmpty(paramStr))
                {
                    Parameters = paramStr.Split(',').Select(p => p.Trim()).ToList();
                    IsFunctionLike = Parameters.Any();
                }
            }

            if (node.HasProperty("definition"))
            {
                Value = node.GetProperty<string>("definition");
            }

            if (node.HasProperty("isConditional"))
            {
                IsConditional = node.GetProperty<bool>("isConditional");
                if (IsConditional && node.HasProperty("condition"))
                {
                    Condition = node.GetProperty<string>("condition");
                }
            }

            IsMultiLine = Value?.Contains('\n') == true;
            DetermineCategory();
        }

        public override string ToString()
        {
            var type = IsFunctionLike ? "function-like" : "object-like";
            return $"{type} macro ({Category}): {Name}";
        }
    }

    // Class cho Typedef
    public class TypedefDefinition : CodeElement
    {
        public DataType OriginalType { get; set; }
        public DataType NewType { get; set; }
        public bool IsStructTypedef { get; set; }
        public bool IsUnionTypedef { get; set; }
        public bool IsEnumTypedef { get; set; }
        public bool IsFunctionPointerTypedef { get; set; }
        public bool IsArrayTypedef { get; set; }
        public bool IsPointerTypedef { get; set; }
        public TypedefCategory Category { get; private set; }

        public enum TypedefCategory
        {
            Simple,
            Struct,
            Union,
            Enum,
            FunctionPointer,
            Array,
            Pointer,
            Complex
        }

        public TypedefDefinition()
        {
            DetermineCategory();
        }

        public TypedefDefinition(string name, DataType originalType) : this()
        {
            Name = name;
            OriginalType = originalType;
            NewType = new DataType(name);
            DetermineCategory();
        }

        private void DetermineCategory()
        {
            if (OriginalType == null)
            {
                Category = TypedefCategory.Simple;
                return;
            }

            if (IsFunctionPointerTypedef || OriginalType.IsFunction)
                Category = TypedefCategory.FunctionPointer;
            else if (IsStructTypedef)
                Category = TypedefCategory.Struct;
            else if (IsUnionTypedef)
                Category = TypedefCategory.Union;
            else if (IsEnumTypedef)
                Category = TypedefCategory.Enum;
            else if (IsArrayTypedef || OriginalType.IsArray)
                Category = TypedefCategory.Array;
            else if (IsPointerTypedef || OriginalType.IsPointer)
                Category = TypedefCategory.Pointer;
            else if (OriginalType.IsBuiltinType())
                Category = TypedefCategory.Simple;
            else
                Category = TypedefCategory.Complex;
        }

        public bool IsAliasFor(string typeName)
        {
            return OriginalType?.BaseType == typeName;
        }

        public bool CreatesNewType()
        {
            // Some typedefs create genuinely new types, others are just aliases
            return IsStructTypedef || IsUnionTypedef || IsEnumTypedef;
        }

        public bool IsSimpleAlias()
        {
            return !CreatesNewType() && !IsFunctionPointerTypedef &&
                   !IsArrayTypedef && OriginalType?.IsBuiltinType() == true;
        }

        public override string GetSignature()
        {
            var originalSig = OriginalType?.GetSignature() ?? "unknown";
            return $"typedef {originalSig} {Name}";
        }

        public override void PopulateFromASTNode(ClangASTNode node)
        {
            base.PopulateFromASTNode(node);

            if (node == null) return;

            // Parse the underlying type
            if (!string.IsNullOrEmpty(node.QualType))
            {
                OriginalType = DataType.Parse(node.QualType);
                NewType = new DataType(Name);
            }

            // Determine typedef category
            if (node.HasProperty("underlyingType"))
            {
                var underlyingTypeStr = node.GetProperty<string>("underlyingType");

                IsStructTypedef = underlyingTypeStr.Contains("struct");
                IsUnionTypedef = underlyingTypeStr.Contains("union");
                IsEnumTypedef = underlyingTypeStr.Contains("enum");
                IsFunctionPointerTypedef = underlyingTypeStr.Contains("(") && underlyingTypeStr.Contains("*");
                IsArrayTypedef = underlyingTypeStr.Contains("[");
                IsPointerTypedef = underlyingTypeStr.Contains("*") && !IsFunctionPointerTypedef;
            }

            DetermineCategory();
        }

        public override string ToString()
        {
            return $"typedef ({Category}): {Name} -> {OriginalType?.GetSignature()}";
        }

        public override bool Equals(object obj)
        {
            if (obj is TypedefDefinition other)
            {
                return Name == other.Name &&
                       OriginalType?.Equals(other.OriginalType) == true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, OriginalType);
        }
    }

    // Helper classes for analysis
    public static class EnumAnalyzer
    {
        public static bool IsLikelyErrorCode(EnumDefinition enumDef)
        {
            if (enumDef?.Members == null)
                return false;

            var errorNames = new[] { "ERROR", "ERR", "FAIL", "SUCCESS", "OK" };
            return enumDef.Members.Any(m => errorNames.Any(err => m.Name?.Contains(err) == true));
        }

        public static bool IsLikelyState(EnumDefinition enumDef)
        {
            if (enumDef?.Members == null)
                return false;

            var stateNames = new[] { "STATE", "STATUS", "MODE", "PHASE" };
            return stateNames.Any(state => enumDef.Name?.Contains(state) == true) ||
                   enumDef.Members.Any(m => stateNames.Any(state => m.Name?.Contains(state) == true));
        }

        public static bool IsLikelyBitField(EnumDefinition enumDef)
        {
            return enumDef?.IsFlags == true;
        }
    }

    public static class MacroAnalyzer
    {
        public static bool IsLikelySafeToUse(MacroDefinition macro)
        {
            if (macro == null)
                return false;

            // Simple constant macros are generally safe
            if (macro.IsConstantMacro() && !macro.Value?.Contains("(") == true)
                return true;

            // Function-like macros with proper parentheses are safer
            if (macro.IsFunctionLike && macro.Value?.Contains("(") == true && macro.Value?.Contains(")") == true)
                return true;

            return false;
        }

        public static bool HasSideEffects(MacroDefinition macro)
        {
            if (macro?.Value == null)
                return false;

            // Check for assignment, increment, decrement operators
            var sideEffectOperators = new[] { "=", "++", "--", "+=", "-=", "*=", "/=" };
            return sideEffectOperators.Any(op => macro.Value.Contains(op));
        }

        public static bool IsMultipleEvaluation(MacroDefinition macro)
        {
            if (!macro?.IsFunctionLike == true || macro.Parameters == null)
                return false;

            // Check if any parameter appears multiple times in the definition
            return macro.Parameters.Any(param =>
                CountOccurrences(macro.Value, param) > 1);
        }

        private static int CountOccurrences(string text, string substring)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(substring))
                return 0;

            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(substring, index)) != -1)
            {
                count++;
                index += substring.Length;
            }
            return count;
        }

        public static List<string> GetPotentialIssues(MacroDefinition macro)
        {
            var issues = new List<string>();

            if (macro == null)
                return issues;

            if (!IsLikelySafeToUse(macro))
                issues.Add("Potentially unsafe macro usage");

            if (HasSideEffects(macro))
                issues.Add("Macro has side effects");

            if (IsMultipleEvaluation(macro))
                issues.Add("Parameters evaluated multiple times");

            if (macro.IsFunctionLike && !macro.Value?.StartsWith("(") == true)
                issues.Add("Function-like macro not properly parenthesized");

            if (macro.IsMultiLine && !macro.Value?.EndsWith("\\") == true)
                issues.Add("Multi-line macro may have continuation issues");

            return issues;
        }
    }

    public static class TypedefAnalyzer
    {
        public static bool IsOpaque(TypedefDefinition typedef)
        {
            // Opaque typedefs hide the implementation
            return typedef?.OriginalType?.BaseType?.Contains("struct") == true &&
                   typedef.Name != typedef.OriginalType.BaseType;
        }

        public static bool IsForwardDeclaration(TypedefDefinition typedef)
        {
            return typedef?.OriginalType?.BaseType?.StartsWith("struct ") == true ||
                   typedef?.OriginalType?.BaseType?.StartsWith("union ") == true ||
                   typedef?.OriginalType?.BaseType?.StartsWith("enum ") == true;
        }

        public static bool CreatesConfusion(TypedefDefinition typedef)
        {
            if (typedef?.OriginalType == null)
                return false;

            // Typedefs that hide pointer nature can be confusing
            return typedef.OriginalType.IsPointer &&
                   !typedef.Name.EndsWith("Ptr") &&
                   !typedef.Name.EndsWith("Handle");
        }

        public static bool IsStandardPattern(TypedefDefinition typedef)
        {
            if (typedef?.Name == null)
                return false;

            // Common naming patterns
            var standardPatterns = new[]
            {
                "_t$",      // POSIX style: size_t, pthread_t
                "Handle$",  // Windows style: HANDLE
                "Ptr$",     // Pointer types
                "Ref$",     // Reference types
                "Type$"     // Type aliases
            };

            return standardPatterns.Any(pattern =>
                System.Text.RegularExpressions.Regex.IsMatch(typedef.Name, pattern));
        }
    }
}
