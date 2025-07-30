using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.CodeAnalysis.Functions
{
    /// <summary>
    /// Enumerates the types of branches
    /// </summary>
    public enum BranchType
    {
        /// <summary>
        /// If statement
        /// </summary>
        If,

        /// <summary>
        /// Switch statement
        /// </summary>
        Switch,

        /// <summary>
        /// While loop
        /// </summary>
        While,

        /// <summary>
        /// Do-while loop
        /// </summary>
        DoWhile,

        /// <summary>
        /// For loop
        /// </summary>
        For,

        /// <summary>
        /// Ternary operator
        /// </summary>
        Ternary
    }
}
