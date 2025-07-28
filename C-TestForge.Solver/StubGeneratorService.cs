using C_TestForge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;

namespace C_TestForge.Solver
{
    /// <summary>
    /// Service for generating stubs for unit tests
    /// </summary>
    public class StubGeneratorService : IStubGeneratorService
    {
        private readonly IParserService _parserService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parserService">The parser service</param>
        public StubGeneratorService(IParserService parserService)
        {
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
        }

        /// <summary>
        /// Generates stubs for the given function
        /// </summary>
        public async Task<Dictionary<string, string>> GenerateStubsAsync(string functionName, string filePath)
        {
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentNullException(nameof(functionName));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            // Get function analysis
            var functionAnalysis = await _parserService.AnalyzeFunctionAsync(functionName, filePath);
            if (functionAnalysis == null)
                throw new InvalidOperationException($"Function not found: {functionName}");

            // Get all functions
            var functions = await _parserService.ExtractFunctionsAsync(filePath);
            var function = functions.FirstOrDefault(f => f.Name == functionName);
            if (function == null)
                throw new InvalidOperationException($"Function not found: {functionName}");

            // Find called functions
            var calledFunctionNames = functionAnalysis.FunctionCalls.Select(fc => fc.CalledFunctionName).Distinct().ToList();

            // Find called functions that are in the same file
            var calledFunctions = functions.Where(f => calledFunctionNames.Contains(f.Name)).ToList();

            // Generate stubs
            var stubs = new Dictionary<string, string>();
            foreach (var calledFunction in calledFunctions)
            {
                var stub = await GenerateStubAsync(calledFunction);
                stubs[calledFunction.Name] = stub;
            }

            return stubs;
        }

        /// <summary>
        /// Generates a single stub for a function
        /// </summary>
        public async Task<string> GenerateStubAsync(CFunction function)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));

            var sb = new StringBuilder();

            // Generate function signature
            sb.Append(function.ReturnType).Append(" ");
            sb.Append(function.Name).Append("(");

            // Add parameters
            for (int i = 0; i < function.Parameters.Count; i++)
            {
                var param = function.Parameters[i];

                // Add parameter type
                sb.Append(param.Type).Append(" ");

                // Add parameter name
                sb.Append(param.Name);

                // Add comma if not the last parameter
                if (i < function.Parameters.Count - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.AppendLine(")");
            sb.AppendLine("{");

            // Add stub implementation
            sb.AppendLine("    /* Stub implementation */");

            // Add return statement if needed
            if (function.ReturnType != "void")
            {
                sb.Append("    return ");

                // Generate default return value based on type
                if (function.ReturnType.Contains("int") ||
                    function.ReturnType.Contains("long") ||
                    function.ReturnType.Contains("short") ||
                    function.ReturnType.Contains("byte"))
                {
                    sb.Append("0");
                }
                else if (function.ReturnType.Contains("float") ||
                         function.ReturnType.Contains("double"))
                {
                    sb.Append("0.0");
                }
                else if (function.ReturnType.Contains("bool") ||
                         function.ReturnType.Contains("boolean"))
                {
                    sb.Append("false");
                }
                else if (function.ReturnType.Contains("char"))
                {
                    sb.Append("'\\0'");
                }
                else if (function.ReturnType.Contains("*") ||
                         function.ReturnType.Contains("pointer"))
                {
                    sb.Append("NULL");
                }
                else
                {
                    sb.Append("0");
                }

                sb.AppendLine(";");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Generates stub headers for the given functions
        /// </summary>
        public async Task<string> GenerateStubHeadersAsync(IEnumerable<CFunction> functions)
        {
            if (functions == null)
                throw new ArgumentNullException(nameof(functions));

            var sb = new StringBuilder();

            // Add header guard
            sb.AppendLine("#ifndef STUBS_H");
            sb.AppendLine("#define STUBS_H");
            sb.AppendLine();

            // Add includes
            sb.AppendLine("#include <stddef.h>");
            sb.AppendLine("#include <stdbool.h>");
            sb.AppendLine();

            // Add function prototypes
            foreach (var function in functions)
            {
                // Generate function prototype
                sb.Append(function.ReturnType).Append(" ");
                sb.Append(function.Name).Append("(");

                // Add parameters
                for (int i = 0; i < function.Parameters.Count; i++)
                {
                    var param = function.Parameters[i];

                    // Add parameter type
                    sb.Append(param.Type).Append(" ");

                    // Add parameter name
                    sb.Append(param.Name);

                    // Add comma if not the last parameter
                    if (i < function.Parameters.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }

                sb.AppendLine(");");
            }

            sb.AppendLine();
            sb.AppendLine("#endif /* STUBS_H */");

            return sb.ToString();
        }
    }
}
