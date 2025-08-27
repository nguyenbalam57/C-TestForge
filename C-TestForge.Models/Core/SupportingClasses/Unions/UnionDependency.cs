using C_TestForge.Models.Base;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace C_TestForge.Models.Core.SupportingClasses.Unions
{
    /// <summary>
    /// Represents a dependency between unions
    /// </summary>
    public class UnionDependency : SourceCodeEntity
    {
        /// <summary>
        /// Name of the union that has the dependency
        /// </summary>
        public string UnionName { get; set; } = string.Empty;

        /// <summary>
        /// Name of the union that is depended upon
        /// </summary>
        public string DependsOn { get; set; } = string.Empty;

        /// <summary>
        /// Type of dependency
        /// </summary>
        public UnionDependencyType DependencyType { get; set; }

        /// <summary>
        /// Context or reason for the dependency
        /// </summary>
        public string Context { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{UnionName} -> {DependsOn} ({DependencyType})";
        }

        public UnionDependency Clone()
        {
            return new UnionDependency
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                UnionName = UnionName,
                DependsOn = DependsOn,
                DependencyType = DependencyType,
                Context = Context
            };
        }
    }
}
