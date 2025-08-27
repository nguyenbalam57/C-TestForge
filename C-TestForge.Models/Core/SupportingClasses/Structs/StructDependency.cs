using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.SupportingClasses.Structs
{
    /// <summary>
    /// Represents struct dependency information
    /// </summary>
    public class StructDependency
    {
        public string StructName { get; set; } = string.Empty;
        public string DependsOn { get; set; } = string.Empty;
        public StructDependencyType DependencyType { get; set; }
        public int LineNumber { get; set; }
    }

    /// <summary>
    /// Types of struct dependencies
    /// </summary>
    public enum StructDependencyType
    {
        Direct,
        Indirect,
        Circular,
        ForwardDeclaration
    }
}
