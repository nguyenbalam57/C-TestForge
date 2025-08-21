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
    // Class cho Struct
    public class StructDefinition : CodeElement
    {
        public List<Variable> Members { get; set; }
        public List<Function> Methods { get; set; }
        public bool IsAnonymous { get; set; }
        public bool IsPacked { get; set; }
        public int Alignment { get; set; }
        public int SizeInBytes { get; set; }
        public List<StructDefinition> NestedStructs { get; set; }
        public List<UnionDefinition> NestedUnions { get; set; }
        public List<EnumDefinition> NestedEnums { get; set; }

        // Additional analysis properties
        public bool IsComplete { get; set; }
        public bool IsOpaque { get; set; }
        public int MemberCount => Members?.Count ?? 0;
        public int Padding { get; set; }
        public Dictionary<string, int> MemberOffsets { get; set; }
        public bool HasFlexibleArrayMember { get; set; }
        public bool HasBitFields { get; set; }

        public StructDefinition()
        {
            Members = new List<Variable>();
            Methods = new List<Function>();
            NestedStructs = new List<StructDefinition>();
            NestedUnions = new List<UnionDefinition>();
            NestedEnums = new List<EnumDefinition>();
            MemberOffsets = new Dictionary<string, int>();
            Alignment = 1;
        }

        public StructDefinition(string name) : this()
        {
            Name = name;
            IsAnonymous = string.IsNullOrEmpty(name);
        }

        public void AddMember(Variable member)
        {
            if (member != null && !Members.Contains(member))
            {
                member.IsField = true;
                member.Scope = ScopeType.BlockScope;
                member.Offset = CalculateNextMemberOffset();
                Members.Add(member);

                if (member.IsBitField)
                    HasBitFields = true;

                // Update offsets dictionary
                if (!string.IsNullOrEmpty(member.Name))
                    MemberOffsets[member.Name] = member.Offset;
            }
        }

        public void AddNestedStruct(StructDefinition nestedStruct)
        {
            if (nestedStruct != null && !NestedStructs.Contains(nestedStruct))
            {
                nestedStruct.ParentContext = GetFullyQualifiedName();
                NestedStructs.Add(nestedStruct);
                AddDependency(nestedStruct);
            }
        }

        public void AddNestedUnion(UnionDefinition nestedUnion)
        {
            if (nestedUnion != null && !NestedUnions.Contains(nestedUnion))
            {
                nestedUnion.ParentContext = GetFullyQualifiedName();
                NestedUnions.Add(nestedUnion);
                AddDependency(nestedUnion);
            }
        }

        public void AddNestedEnum(EnumDefinition nestedEnum)
        {
            if (nestedEnum != null && !NestedEnums.Contains(nestedEnum))
            {
                nestedEnum.ParentContext = GetFullyQualifiedName();
                NestedEnums.Add(nestedEnum);
                AddDependency(nestedEnum);
            }
        }

        public Variable GetMember(string memberName)
        {
            return Members.FirstOrDefault(m => m.Name == memberName);
        }

        public Variable GetMemberByOffset(int offset)
        {
            return Members.FirstOrDefault(m => m.Offset == offset);
        }

        public List<Variable> GetPointerMembers()
        {
            return Members.Where(m => m.Type?.IsPointer == true).ToList();
        }

        public List<Variable> GetArrayMembers()
        {
            return Members.Where(m => m.Type?.IsArray == true).ToList();
        }

        public List<Variable> GetBitFieldMembers()
        {
            return Members.Where(m => m.IsBitField).ToList();
        }

        public bool HasMember(string memberName)
        {
            return Members.Any(m => m.Name == memberName);
        }

        public bool HasPointerMembers()
        {
            return Members.Any(m => m.Type?.IsPointer == true);
        }

        public bool IsEmpty()
        {
            return !Members.Any();
        }

        private int CalculateNextMemberOffset()
        {
            if (!Members.Any())
                return 0;

            var lastMember = Members.Last();
            var lastMemberSize = lastMember.GetSize();

            if (lastMemberSize == 0)
                lastMemberSize = 1; // Minimum size

            return lastMember.Offset + lastMemberSize;
        }

        public void CalculateLayout()
        {
            if (!Members.Any())
            {
                SizeInBytes = 0;
                return;
            }

            int currentOffset = 0;
            int maxAlignment = 1;

            foreach (var member in Members)
            {
                var memberSize = member.GetSize();
                var memberAlignment = GetMemberAlignment(member);

                if (memberAlignment > maxAlignment)
                    maxAlignment = memberAlignment;

                // Add padding for alignment
                if (currentOffset % memberAlignment != 0)
                {
                    currentOffset += memberAlignment - (currentOffset % memberAlignment);
                }

                member.Offset = currentOffset;
                MemberOffsets[member.Name] = currentOffset;

                currentOffset += memberSize;
            }

            // Add trailing padding for struct alignment
            if (currentOffset % maxAlignment != 0)
            {
                currentOffset += maxAlignment - (currentOffset % maxAlignment);
            }

            SizeInBytes = currentOffset;
            Alignment = maxAlignment;

            // Calculate total padding
            var totalMemberSize = Members.Sum(m => m.GetSize());
            Padding = SizeInBytes - totalMemberSize;
        }

        private int GetMemberAlignment(Variable member)
        {
            if (member?.Type == null)
                return 1;

            // For basic types, alignment usually equals size (up to pointer size)
            var size = member.GetSize();
            var pointerSize = IntPtr.Size;

            return Math.Min(size > 0 ? size : 1, pointerSize);
        }

        public double GetPaddingRatio()
        {
            return SizeInBytes > 0 ? (double)Padding / SizeInBytes : 0.0;
        }

        public bool IsWellPacked()
        {
            return GetPaddingRatio() < 0.25; // Less than 25% padding
        }

        public override string GetSignature()
        {
            var memberSignatures = Members.Select(m => "    " + m.GetSignature());
            var nestedTypes = new List<string>();

            if (NestedStructs.Any())
                nestedTypes.AddRange(NestedStructs.Select(s => "    " + s.GetSignature()));

            if (NestedUnions.Any())
                nestedTypes.AddRange(NestedUnions.Select(u => "    " + u.GetSignature()));

            if (NestedEnums.Any())
                nestedTypes.AddRange(NestedEnums.Select(e => "    " + e.GetSignature()));

            var allMembers = nestedTypes.Concat(memberSignatures);

            var structName = IsAnonymous ? "" : Name;
            return $"struct {structName} {{\n{string.Join(";\n", allMembers)};\n}}";
        }

        public override void PopulateFromASTNode(ClangASTNode node)
        {
            base.PopulateFromASTNode(node);

            if (node == null) return;

            IsAnonymous = string.IsNullOrEmpty(Name);

            // Extract size and alignment information
            if (node.HasProperty("size"))
                SizeInBytes = node.GetProperty<int>("size");

            if (node.HasProperty("alignment"))
                Alignment = node.GetProperty<int>("alignment");

            // Check if it's a complete definition
            IsComplete = node.Children.Any(c => c.Kind == ClangASTNodeKind.FieldDecl);
            IsOpaque = !IsComplete;

            // Extract field members
            var fieldNodes = node.Children.Where(c => c.Kind == ClangASTNodeKind.FieldDecl);
            foreach (var fieldNode in fieldNodes)
            {
                var field = new Variable();
                field.PopulateFromASTNode(fieldNode);
                AddMember(field);
            }

            // Extract nested types
            var nestedRecords = node.Children.Where(c => c.Kind == ClangASTNodeKind.RecordDecl);
            foreach (var nestedRecord in nestedRecords)
            {
                var nestedStruct = new StructDefinition();
                nestedStruct.PopulateFromASTNode(nestedRecord);
                AddNestedStruct(nestedStruct);
            }

            // Calculate layout if we have complete information
            if (IsComplete)
                CalculateLayout();
        }

        public override string ToString()
        {
            var completeness = IsComplete ? "complete" : "incomplete";
            var anonymity = IsAnonymous ? "anonymous" : "named";
            return $"{anonymity} {completeness} struct: {Name} ({MemberCount} members, {SizeInBytes} bytes)";
        }
    }

    // Class cho Union
    public class UnionDefinition : CodeElement
    {
        public List<Variable> Members { get; set; }
        public bool IsAnonymous { get; set; }
        public int SizeInBytes { get; set; }
        public int Alignment { get; set; }
        public List<StructDefinition> NestedStructs { get; set; }
        public List<UnionDefinition> NestedUnions { get; set; }
        public List<EnumDefinition> NestedEnums { get; set; }

        // Additional analysis properties
        public bool IsComplete { get; set; }
        public bool IsOpaque { get; set; }
        public int MemberCount => Members?.Count ?? 0;
        public Variable LargestMember { get; private set; }

        public UnionDefinition()
        {
            Members = new List<Variable>();
            NestedStructs = new List<StructDefinition>();
            NestedUnions = new List<UnionDefinition>();
            NestedEnums = new List<EnumDefinition>();
            Alignment = 1;
        }

        public UnionDefinition(string name) : this()
        {
            Name = name;
            IsAnonymous = string.IsNullOrEmpty(name);
        }

        public void AddMember(Variable member)
        {
            if (member != null && !Members.Contains(member))
            {
                member.IsField = true;
                member.Scope = ScopeType.BlockScope;
                member.Offset = 0; // All union members start at offset 0
                Members.Add(member);

                // Update largest member
                if (LargestMember == null || member.GetSize() > LargestMember.GetSize())
                    LargestMember = member;
            }
        }

        public void AddNestedStruct(StructDefinition nestedStruct)
        {
            if (nestedStruct != null && !NestedStructs.Contains(nestedStruct))
            {
                nestedStruct.ParentContext = GetFullyQualifiedName();
                NestedStructs.Add(nestedStruct);
                AddDependency(nestedStruct);
            }
        }

        public void AddNestedUnion(UnionDefinition nestedUnion)
        {
            if (nestedUnion != null && !NestedUnions.Contains(nestedUnion))
            {
                nestedUnion.ParentContext = GetFullyQualifiedName();
                NestedUnions.Add(nestedUnion);
                AddDependency(nestedUnion);
            }
        }

        public void AddNestedEnum(EnumDefinition nestedEnum)
        {
            if (nestedEnum != null && !NestedEnums.Contains(nestedEnum))
            {
                nestedEnum.ParentContext = GetFullyQualifiedName();
                NestedEnums.Add(nestedEnum);
                AddDependency(nestedEnum);
            }
        }

        public Variable GetMember(string memberName)
        {
            return Members.FirstOrDefault(m => m.Name == memberName);
        }

        public List<Variable> GetPointerMembers()
        {
            return Members.Where(m => m.Type?.IsPointer == true).ToList();
        }

        public List<Variable> GetArrayMembers()
        {
            return Members.Where(m => m.Type?.IsArray == true).ToList();
        }

        public bool HasMember(string memberName)
        {
            return Members.Any(m => m.Name == memberName);
        }

        public bool HasPointerMembers()
        {
            return Members.Any(m => m.Type?.IsPointer == true);
        }

        public bool IsEmpty()
        {
            return !Members.Any();
        }

        public void CalculateLayout()
        {
            if (!Members.Any())
            {
                SizeInBytes = 0;
                Alignment = 1;
                return;
            }

            // Union size is the size of the largest member
            int maxSize = 0;
            int maxAlignment = 1;

            foreach (var member in Members)
            {
                var memberSize = member.GetSize();
                var memberAlignment = GetMemberAlignment(member);

                if (memberSize > maxSize)
                    maxSize = memberSize;

                if (memberAlignment > maxAlignment)
                    maxAlignment = memberAlignment;

                // All members have offset 0 in a union
                member.Offset = 0;
            }

            // Align the union size
            if (maxSize % maxAlignment != 0)
                maxSize += maxAlignment - (maxSize % maxAlignment);

            SizeInBytes = maxSize;
            Alignment = maxAlignment;
        }

        private int GetMemberAlignment(Variable member)
        {
            if (member?.Type == null)
                return 1;

            var size = member.GetSize();
            var pointerSize = IntPtr.Size;

            return Math.Min(size > 0 ? size : 1, pointerSize);
        }

        public bool IsTaggedUnion()
        {
            // A tagged union typically has an enum member to indicate which variant is active
            return Members.Any(m => m.Type?.BaseType?.Contains("enum") == true);
        }

        public override string GetSignature()
        {
            var memberSignatures = Members.Select(m => "    " + m.GetSignature());
            var nestedTypes = new List<string>();

            if (NestedStructs.Any())
                nestedTypes.AddRange(NestedStructs.Select(s => "    " + s.GetSignature()));

            if (NestedUnions.Any())
                nestedTypes.AddRange(NestedUnions.Select(u => "    " + u.GetSignature()));

            if (NestedEnums.Any())
                nestedTypes.AddRange(NestedEnums.Select(e => "    " + e.GetSignature()));

            var allMembers = nestedTypes.Concat(memberSignatures);

            var unionName = IsAnonymous ? "" : Name;
            return $"union {unionName} {{\n{string.Join(";\n", allMembers)};\n}}";
        }

        public override void PopulateFromASTNode(ClangASTNode node)
        {
            base.PopulateFromASTNode(node);

            if (node == null) return;

            IsAnonymous = string.IsNullOrEmpty(Name);

            // Extract size and alignment information
            if (node.HasProperty("size"))
                SizeInBytes = node.GetProperty<int>("size");

            if (node.HasProperty("alignment"))
                Alignment = node.GetProperty<int>("alignment");

            // Check if it's a complete definition
            IsComplete = node.Children.Any(c => c.Kind == ClangASTNodeKind.FieldDecl);
            IsOpaque = !IsComplete;

            // Extract field members
            var fieldNodes = node.Children.Where(c => c.Kind == ClangASTNodeKind.FieldDecl);
            foreach (var fieldNode in fieldNodes)
            {
                var field = new Variable();
                field.PopulateFromASTNode(fieldNode);
                AddMember(field);
            }

            // Extract nested types
            var nestedRecords = node.Children.Where(c => c.Kind == ClangASTNodeKind.RecordDecl);
            foreach (var nestedRecord in nestedRecords)
            {
                if (nestedRecord.HasProperty("tagUsed") && nestedRecord.GetProperty<string>("tagUsed") == "union")
                {
                    var nestedUnion = new UnionDefinition();
                    nestedUnion.PopulateFromASTNode(nestedRecord);
                    AddNestedUnion(nestedUnion);
                }
                else
                {
                    var nestedStruct = new StructDefinition();
                    nestedStruct.PopulateFromASTNode(nestedRecord);
                    AddNestedStruct(nestedStruct);
                }
            }

            // Calculate layout if we have complete information
            if (IsComplete)
                CalculateLayout();
        }

        public override string ToString()
        {
            var completeness = IsComplete ? "complete" : "incomplete";
            var anonymity = IsAnonymous ? "anonymous" : "named";
            var taggedStr = IsTaggedUnion() ? "tagged " : "";
            return $"{anonymity} {completeness} {taggedStr}union: {Name} ({MemberCount} members, {SizeInBytes} bytes)";
        }

        public override bool Equals(object obj)
        {
            if (obj is UnionDefinition other)
            {
                return Name == other.Name &&
                       Members.Count == other.Members.Count &&
                       Members.SequenceEqual(other.Members);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Members.Count, SizeInBytes);
        }
    }

    // Helper class for struct/union analysis
    public static class CompositeTypeAnalyzer
    {
        public static bool IsLikelyPOD(StructDefinition structDef)
        {
            if (structDef == null || !structDef.IsComplete)
                return false;

            // POD types have only basic data members, no complex constructors, etc.
            return structDef.Members.All(m =>
                m.Type?.IsBuiltinType() == true ||
                m.Type?.IsPointer == true ||
                m.Type?.IsArray == true);
        }

        public static bool HasPaddingIssues(StructDefinition structDef)
        {
            return structDef?.GetPaddingRatio() > 0.5; // More than 50% padding
        }

        public static bool IsLikelyHandle(StructDefinition structDef)
        {
            // Opaque structs with only a pointer or identifier are likely handles
            return structDef?.IsOpaque == true ||
                   (structDef?.Members.Count == 1 && structDef.Members[0].Type?.IsPointer == true);
        }

        public static bool IsLikelyVariant(UnionDefinition unionDef)
        {
            // Tagged unions are variants
            return unionDef?.IsTaggedUnion() == true;
        }

        public static List<Variable> FindCircularReferences(StructDefinition structDef)
        {
            var circularRefs = new List<Variable>();

            if (structDef?.Members == null)
                return circularRefs;

            foreach (var member in structDef.Members)
            {
                if (member.Type?.BaseType == structDef.Name && member.Type.IsPointer)
                {
                    // Self-referencing pointer (common for linked lists, trees)
                    circularRefs.Add(member);
                }
            }

            return circularRefs;
        }
    }
}
