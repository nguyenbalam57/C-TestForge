using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Base
{
    /// <summary>
    /// Base interface for all model objects
    /// </summary>
    public interface IModelObject
    {
        /// <summary>
        /// Unique identifier for the object
        /// </summary>
        string Id { get; set; }
    }
}
