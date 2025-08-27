using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Projects
{
    /// <summary>
    /// Type of source file
    /// </summary>
    public enum SourceFileType
    {
        /// <summary>
        /// C header file (.h)
        /// </summary>
        CHeader,
        /// <summary>
        /// C source file (.c)
        /// </summary>
        CSource,

        Implementation,
        /// <summary>
        /// Unknown file type
        /// </summary>
        Unknown,
        /// <summary>
        /// C++ source file (.cpp, .cc, .cxx)
        /// </summary>
        CPPSource,
        /// <summary>
        /// C++ header file (.hpp)
        /// </summary>
        CPPHeader,
        Assembly,
        Include
    }
}
