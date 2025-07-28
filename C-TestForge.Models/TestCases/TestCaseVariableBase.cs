using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestCases
{
    /// <summary>
    /// Base class for test case variables
    /// </summary>
    public abstract class TestCaseVariableBase : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the variable
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the variable
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Whether the variable is an array
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Size of the array (if IsArray is true)
        /// </summary>
        public int ArraySize { get; set; }

        /// <summary>
        /// Whether the variable is a pointer
        /// </summary>
        public bool IsPointer { get; set; }

        /// <summary>
        /// Whether the variable is passed by reference
        /// </summary>
        public bool IsByReference { get; set; }

        /// <summary>
        /// Constraints on the variable
        /// </summary>
        public List<VariableConstraint> Constraints { get; set; } = new List<VariableConstraint>();

        /// <summary>
        /// Get a string representation of the test case variable
        /// </summary>
        public override string ToString()
        {
            string arrayPart = IsArray ? $"[{ArraySize}]" : "";
            string pointerPart = IsPointer ? "*" : "";
            string refPart = IsByReference ? "&" : "";

            return $"{Type}{pointerPart}{refPart} {Name}{arrayPart}";
        }
    }
}
