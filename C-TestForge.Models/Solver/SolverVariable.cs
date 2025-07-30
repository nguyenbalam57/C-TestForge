using C_TestForge.Models.Base;

namespace C_TestForge.Models.Solver
{
    /// <summary>
    /// Represents a variable declaration for the Z3 solver
    /// </summary>
    public class SolverVariable : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the variable
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type of the variable in Z3 (Int, Real, Bool, etc.)
        /// </summary>
        public string Z3Type { get; set; } = string.Empty;

        /// <summary>
        /// Original C type of the variable
        /// </summary>
        public string CType { get; set; } = string.Empty;

        /// <summary>
        /// Whether the variable is an array
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Size of the array (if IsArray is true)
        /// </summary>
        public int ArraySize { get; set; }

        /// <summary>
        /// Domain of the variable (for enumeration types)
        /// </summary>
        public List<string> Domain { get; set; } = new List<string>();

        /// <summary>
        /// Creates a clone of the solver variable
        /// </summary>
        public SolverVariable Clone()
        {
            return new SolverVariable
            {
                Id = Id,
                Name = Name,
                Z3Type = Z3Type,
                CType = CType,
                IsArray = IsArray,
                ArraySize = ArraySize,
                Domain = Domain != null ? new List<string>(Domain) : new List<string>()
            };
        }
    }
}