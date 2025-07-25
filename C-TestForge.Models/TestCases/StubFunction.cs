using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestCases
{
    public class StubFunction
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public List<CVariable> Parameters { get; set; } = new List<CVariable>();
        public object ReturnValue { get; set; }
        public List<StubParameterBehavior> ParameterBehaviors { get; set; } = new List<StubParameterBehavior>();

        /// <summary>
        /// Creates a deep copy of a StubFunction
        /// </summary>
        /// <param name="stub">The StubFunction to clone</param>
        /// <returns>A new StubFunction instance with the same properties</returns>
        public StubFunction Clone()
        {
            var clone = new StubFunction
            {
                Name = this.Name,
                ReturnType = this.ReturnType,
                ReturnValue = this.ReturnValue,
            };

            // Clone parameters
            foreach (var param in this.Parameters)
            {
                clone.Parameters.Add(param.Clone());
            }

            // Clone parameter behaviors
            foreach (var behavior in this.ParameterBehaviors)
            {
                clone.ParameterBehaviors.Add(new StubParameterBehavior
                {
                    ParameterName = behavior.ParameterName,
                    Action = behavior.Action,
                    Value = behavior.Value,
                    BufferSize = behavior.BufferSize
                });
            }

            return clone;
        }
    }
}
