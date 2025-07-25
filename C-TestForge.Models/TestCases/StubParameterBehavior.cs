using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestCases
{
    public class StubParameterBehavior
    {
        public string ParameterName { get; set; }
        public StubParameterAction Action { get; set; }
        public object Value { get; set; }
        public int BufferSize { get; set; }
    }
}
