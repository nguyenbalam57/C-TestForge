using C_TestForge.Models.CodeAnalysis.ClangASTNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.CodeAnalysis.BaseClasss
{
    // Base class cho tất cả các thành phần code (Enhanced for Clang AST)
    public abstract class CodeElement
    {
        // Basic properties
        public string Name { get; set; }
        public string SourceFile { get; set; }
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }
        public ScopeType Scope { get; set; }
        public string Documentation { get; set; }
        public Dictionary<string, object> Attributes { get; set; }

        // Dependencies and relationships
        public List<CodeElement> Dependencies { get; set; }
        public List<CodeElement> UsedBy { get; set; }

        // Clang AST specific properties
        public string ClangId { get; set; }
        public ClangASTNodeKind ASTKind { get; set; }
        public SourceRange SourceRange { get; set; }
        public ClangASTNode ASTNode { get; set; }
        public string MangleName { get; set; }
        public LinkageKind Linkage { get; set; }
        public string QualifiedType { get; set; }
        public bool IsImplicit { get; set; }
        public bool IsReferenced { get; set; }
        public bool IsUsed { get; set; }
        public string ParentContext { get; set; }
        public Dictionary<string, string> ClangAttributes { get; set; }

        protected CodeElement()
        {
            Dependencies = new List<CodeElement>();
            UsedBy = new List<CodeElement>();
            Attributes = new Dictionary<string, object>();
            SourceRange = new SourceRange();
            ClangAttributes = new Dictionary<string, string>();
        }

        public virtual void AddDependency(CodeElement dependency)
        {
            if (dependency != null && !Dependencies.Contains(dependency))
            {
                Dependencies.Add(dependency);
                if (!dependency.UsedBy.Contains(this))
                    dependency.UsedBy.Add(this);
            }
        }

        public virtual void RemoveDependency(CodeElement dependency)
        {
            if (dependency != null)
            {
                Dependencies.Remove(dependency);
                dependency.UsedBy.Remove(this);
            }
        }

        public virtual void ClearDependencies()
        {
            foreach (var dep in Dependencies.ToList())
            {
                RemoveDependency(dep);
            }
        }

        // Tạo từ Clang AST Node
        public virtual void PopulateFromASTNode(ClangASTNode node)
        {
            if (node == null) return;

            ClangId = node.Id;
            ASTKind = node.Kind;
            SourceRange = node.Location;
            ASTNode = node;
            IsImplicit = node.IsImplicit;
            IsReferenced = node.IsReferenced;
            IsUsed = node.IsUsed;
            QualifiedType = node.QualType;

            if (!string.IsNullOrEmpty(node.Name))
                Name = node.Name;

            if (SourceRange?.Begin != null)
            {
                SourceFile = SourceRange.Begin.FileName;
                LineNumber = SourceRange.Begin.Line;
                ColumnNumber = SourceRange.Begin.Column;
            }

            // Copy properties từ AST node
            foreach (var prop in node.Properties)
            {
                if (prop.Value != null)
                    Attributes[prop.Key] = prop.Value;
            }
        }

        // Helper methods
        public bool IsInSystemHeader()
        {
            return SourceRange?.Begin?.FileName?.Contains("usr/include") == true ||
                   SourceRange?.Begin?.FileName?.Contains("Program Files") == true;
        }

        public bool IsInMacroExpansion()
        {
            return SourceRange?.Begin?.IsMacroExpansion == true;
        }

        public string GetFullyQualifiedName()
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(ParentContext))
                parts.Add(ParentContext);

            if (!string.IsNullOrEmpty(Name))
                parts.Add(Name);

            return string.Join("::", parts);
        }

        public SourceLocation GetDefinitionLocation()
        {
            return SourceRange?.Begin ?? SourceLocation.Invalid();
        }

        public virtual bool HasAttribute(string attributeName)
        {
            return Attributes.ContainsKey(attributeName) || ClangAttributes.ContainsKey(attributeName);
        }

        public virtual T GetAttribute<T>(string attributeName, T defaultValue = default)
        {
            if (Attributes.ContainsKey(attributeName))
            {
                try
                {
                    return (T)Convert.ChangeType(Attributes[attributeName], typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public virtual void SetAttribute(string attributeName, object value)
        {
            Attributes[attributeName] = value;
        }

        // Abstract method that must be implemented by derived classes
        public abstract string GetSignature();

        public override string ToString()
        {
            return $"{GetType().Name}: {Name} at {SourceRange?.Begin}";
        }

        public override bool Equals(object obj)
        {
            if (obj is CodeElement other)
            {
                return Name == other.Name &&
                       GetType() == other.GetType() &&
                       SourceRange?.Equals(other.SourceRange) == true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, GetType(), SourceRange);
        }
    }
}
