using C_TestForge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Parser
{
    public interface IParser
    {
        CSourceFile ParseFile(string filePath, IEnumerable<string> includePaths = null, IEnumerable<KeyValuePair<string, string>> defines = null);
    }
}
