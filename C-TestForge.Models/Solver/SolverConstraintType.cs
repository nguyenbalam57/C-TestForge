using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Solver
{
    /// <summary>
    /// Type of constraint for the solver
    /// </summary>
    public enum SolverConstraintType
    {
        /// <summary>
        /// Equality constraint (a == b)
        /// </summary>
        Equality,

        /// <summary>
        /// Inequality constraint (a != b)
        /// </summary>
        Inequality,

        /// <summary>
        /// Less than constraint (a < b)
        /// </summary>
        LessThan,

        /// <summary>
        /// Less than or equal constraint (a <= b)
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// Greater than constraint (a > b)
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Greater than or equal constraint (a >= b)
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// Range constraint (a <= x <= b)
        /// </summary>
        Range,

        /// <summary>
        /// Membership constraint (x in [a, b, c])
        /// </summary>
        Membership,

        /// <summary>
        /// Custom constraint (e.g. complex expression)
        /// </summary>
        Custom
    }
}
