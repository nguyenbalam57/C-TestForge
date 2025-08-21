using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.CodeAnalysis.ClangASTNodes
{
    // Class cho AST Node information từ Clang
    public class ClangASTNode
    {
        public string Id { get; set; }
        public ClangASTNodeKind Kind { get; set; }
        public SourceRange Location { get; set; }
        public string Text { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public List<ClangASTNode> Children { get; set; }
        public ClangASTNode Parent { get; set; }
        public int Depth { get; set; }
        public string Type { get; set; }
        public string QualType { get; set; }
        public bool IsImplicit { get; set; }
        public bool IsReferenced { get; set; }
        public bool IsUsed { get; set; }
        public string Name { get; set; }

        public ClangASTNode()
        {
            Properties = new Dictionary<string, object>();
            Children = new List<ClangASTNode>();
            Location = new SourceRange();
        }

        public void AddChild(ClangASTNode child)
        {
            if (child != null)
            {
                child.Parent = this;
                child.Depth = this.Depth + 1;
                Children.Add(child);
            }
        }

        public List<ClangASTNode> FindByKind(ClangASTNodeKind kind)
        {
            var result = new List<ClangASTNode>();
            FindByKindRecursive(kind, result);
            return result;
        }

        private void FindByKindRecursive(ClangASTNodeKind kind, List<ClangASTNode> result)
        {
            if (Kind == kind)
                result.Add(this);

            foreach (var child in Children)
                child.FindByKindRecursive(kind, result);
        }

        public List<ClangASTNode> FindByName(string name)
        {
            var result = new List<ClangASTNode>();
            FindByNameRecursive(name, result);
            return result;
        }

        private void FindByNameRecursive(string name, List<ClangASTNode> result)
        {
            if (Name == name || (Properties.ContainsKey("name") && Properties["name"]?.ToString() == name))
                result.Add(this);

            foreach (var child in Children)
                child.FindByNameRecursive(name, result);
        }

        public ClangASTNode FindParent(ClangASTNodeKind kind)
        {
            var current = Parent;
            while (current != null)
            {
                if (current.Kind == kind)
                    return current;
                current = current.Parent;
            }
            return null;
        }

        public List<ClangASTNode> GetAncestors()
        {
            var ancestors = new List<ClangASTNode>();
            var current = Parent;
            while (current != null)
            {
                ancestors.Add(current);
                current = current.Parent;
            }
            return ancestors;
        }

        public List<ClangASTNode> GetDescendants()
        {
            var descendants = new List<ClangASTNode>();
            GetDescendantsRecursive(descendants);
            return descendants;
        }

        private void GetDescendantsRecursive(List<ClangASTNode> descendants)
        {
            foreach (var child in Children)
            {
                descendants.Add(child);
                child.GetDescendantsRecursive(descendants);
            }
        }

        public bool HasProperty(string propertyName)
        {
            return Properties.ContainsKey(propertyName);
        }

        public T GetProperty<T>(string propertyName, T defaultValue = default)
        {
            if (Properties.ContainsKey(propertyName))
            {
                try
                {
                    return (T)Convert.ChangeType(Properties[propertyName], typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public void SetProperty(string propertyName, object value)
        {
            Properties[propertyName] = value;
        }

        public override string ToString()
        {
            var nameStr = !string.IsNullOrEmpty(Name) ? $" '{Name}'" : "";
            return $"{Kind}{nameStr} at {Location}";
        }

        public string ToTreeString(int indentLevel = 0)
        {
            var indent = new string(' ', indentLevel * 2);
            var result = $"{indent}{this}\n";

            foreach (var child in Children)
            {
                result += child.ToTreeString(indentLevel + 1);
            }

            return result;
        }
    }

}
