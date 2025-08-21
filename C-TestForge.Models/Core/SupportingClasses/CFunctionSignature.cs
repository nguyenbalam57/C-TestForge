using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.SupportingClasses
{
    /// <summary>
    /// Represents a function signature for function pointers
    /// </summary>
    public class CFunctionSignature
    {
        /// <summary>
        /// Return type of the function
        /// </summary>
        public string ReturnType { get; set; } = string.Empty;

        /// <summary>
        /// List of parameter types
        /// </summary>
        public List<string> ParameterTypes { get; set; } = new List<string>();

        /// <summary>
        /// Whether the function is variadic
        /// </summary>
        public bool IsVariadic { get; set; }

        /// <summary>
        /// Calling convention (if specified)
        /// </summary>
        public string CallingConvention { get; set; } = string.Empty;

        public override string ToString()
        {
            string paramList = string.Join(", ", ParameterTypes);
            if (IsVariadic && ParameterTypes.Count > 0)
                paramList += ", ...";
            else if (IsVariadic)
                paramList = "...";

            string convention = !string.IsNullOrEmpty(CallingConvention) ? $" {CallingConvention}" : "";
            return $"{ReturnType}{convention}(*) ({paramList})";
        }

        public CFunctionSignature Clone()
        {
            return new CFunctionSignature
            {
                ReturnType = ReturnType,
                ParameterTypes = new List<string>(ParameterTypes ?? new List<string>()),
                IsVariadic = IsVariadic,
                CallingConvention = CallingConvention
            };
        }
    }
}
