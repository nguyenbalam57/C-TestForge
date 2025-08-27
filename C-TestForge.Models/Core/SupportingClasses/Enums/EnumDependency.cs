using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.SupportingClasses.Enums
{
    /// <summary>
    /// Enum dependency relationship
    /// </summary>
    public class EnumDependency
    {
        public string EnumName { get; set; }
        public string DependsOn { get; set; }
        public EnumDependencyType DependencyType { get; set; }
        public int LineNumber { get; set; }
    }

    /// <summary>
    /// Types of enum dependencies
    /// </summary>
    public enum EnumDependencyType
    {
        ValueDependency,
        TypeDependency,
        UsageDependency
    }
}
