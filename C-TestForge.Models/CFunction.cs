using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using C_TestForge.Models.TestCases;

namespace C_TestForge.Models
{
    /// <summary>
    /// Represents a function in C source code
    /// </summary>
    public class CFunction
    {
        /// <summary>
        /// Gets or sets the unique identifier for the function
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the name of the function
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the return type of the function
        /// </summary>
        public string ReturnType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the storage class of the function (extern, static, inline)
        /// </summary>
        public FunctionStorageClass StorageClass { get; set; }

        /// <summary>
        /// Gets or sets the list of parameters for the function
        /// </summary>
        public List<CVariable> Parameters { get; set; } = new List<CVariable>();

        /// <summary>
        /// Gets or sets the list of local variables declared in the function
        /// </summary>
        public List<CVariable> LocalVariables { get; set; } = new List<CVariable>();

        /// <summary>
        /// Gets or sets the list of functions called by this function
        /// </summary>
        public List<string> CalledFunctions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of functions that call this function
        /// </summary>
        public List<string> CalledByFunctions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the line number where the function begins
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the line number where the function ends
        /// </summary>
        public int EndLineNumber { get; set; }

        /// <summary>
        /// Gets or sets the body of the function
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the function is enabled (not disabled by preprocessing)
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the parent preprocessor directive (if defined in a conditional block)
        /// </summary>
        public CPreprocessorDirective ParentDirective { get; set; } = new CPreprocessorDirective();

        /// <summary>
        /// Gets or sets the source file where the function is defined
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the function is defined (has a body) or just declared
        /// </summary>
        public bool IsDefined { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the function is inline
        /// </summary>
        public bool IsInline { get; set; }

        /// <summary>
        /// Gets or sets whether the function is static
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Gets or sets the complexity metrics of the function
        /// </summary>
        public FunctionComplexity Complexity { get; set; } = new FunctionComplexity();

        /// <summary>
        /// Gets or sets whether the function is a test case
        /// </summary>
        public bool IsTestCase { get; set; }

        /// <summary>
        /// Gets or sets the tags associated with the function
        /// </summary>
        public string Tags { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the function
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Creates a deep copy of this function
        /// </summary>
        /// <returns>A new CFunction instance with the same properties</returns>
        public CFunction Clone()
        {
            var clone = new CFunction
            {
                Id = Guid.NewGuid(),
                Name = this.Name,
                ReturnType = this.ReturnType,
                StorageClass = this.StorageClass,
                LineNumber = this.LineNumber,
                EndLineNumber = this.EndLineNumber,
                Body = this.Body,
                IsEnabled = this.IsEnabled,
                SourceFile = this.SourceFile,
                IsDefined = this.IsDefined,
                IsInline = this.IsInline,
                IsStatic = this.IsStatic,
                IsTestCase = this.IsTestCase,
                Tags = this.Tags,
                Description = this.Description
            };

            // Deep copy the lists
            foreach (var param in this.Parameters)
            {
                clone.Parameters.Add(param.Clone());
            }

            foreach (var localVar in this.LocalVariables)
            {
                clone.LocalVariables.Add(localVar.Clone());
            }

            clone.CalledFunctions = new List<string>(this.CalledFunctions);
            clone.CalledByFunctions = new List<string>(this.CalledByFunctions);

            // Clone complexity
            if (this.Complexity != null)
            {
                clone.Complexity = this.Complexity.Clone();
            }

            // Clone parent directive
            if (this.ParentDirective != null)
            {
                clone.ParentDirective = new CPreprocessorDirective
                {
                    Id = this.ParentDirective.Id,
                    RawText = this.ParentDirective.RawText,
                    Type = this.ParentDirective.Type
                };
            }

            return clone;
        }

        /// <summary>
        /// Converts the function to its C declaration
        /// </summary>
        /// <returns>A string representation of the function as a C declaration</returns>
        public override string ToString()
        {
            var builder = new StringBuilder();

            // Add storage class if applicable
            if (StorageClass != FunctionStorageClass.None)
            {
                builder.Append(StorageClass.ToString().ToLower()).Append(' ');
            }

            // Add inline if applicable
            if (IsInline)
            {
                builder.Append("inline ");
            }

            // Add return type
            builder.Append(ReturnType).Append(' ');

            // Add name and parameters
            builder.Append(Name).Append('(');

            if (Parameters.Count > 0)
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(Parameters[i].ToCString().TrimEnd(';'));
                }
            }
            else
            {
                builder.Append("void");
            }

            builder.Append(')');

            // Add body if defined
            if (IsDefined)
            {
                builder.AppendLine();
                builder.AppendLine("{");

                string[] lines = Body.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    builder.AppendLine($"    {line}");
                }

                builder.Append("}");
            }
            else
            {
                builder.Append(";");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets the signature of the function
        /// </summary>
        /// <returns>A string representation of the function signature</returns>
        public string GetSignature()
        {
            var builder = new StringBuilder();

            // Add return type
            builder.Append(ReturnType).Append(' ');

            // Add name and parameters
            builder.Append(Name).Append('(');

            if (Parameters.Count > 0)
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(Parameters[i].ToCString().TrimEnd(';'));
                }
            }
            else
            {
                builder.Append("void");
            }

            builder.Append(')');

            return builder.ToString();
        }

        /// <summary>
        /// Analyzes the function body to find function calls
        /// </summary>
        /// <returns>A list of function names that are called</returns>
        public List<string> AnalyzeFunctionCalls()
        {
            var result = new List<string>();

            if (string.IsNullOrEmpty(Body))
                return result;

            // This is a simplified approach. A more robust solution would parse the AST.
            // Regular expression to match function calls
            var functionCallPattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\(";
            var matches = Regex.Matches(Body, functionCallPattern);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    string functionName = match.Groups[1].Value;

                    // Skip if it's a common C keyword that might be followed by parentheses
                    if (functionName == "if" || functionName == "for" || functionName == "while" ||
                        functionName == "switch" || functionName == "return" || functionName == "sizeof")
                        continue;

                    // Skip if it's the current function (recursive call)
                    if (functionName == Name)
                        continue;

                    // Add to the list if not already present
                    if (!result.Contains(functionName))
                    {
                        result.Add(functionName);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Analyzes the function body to find conditional branches
        /// </summary>
        /// <returns>A list of conditional expressions</returns>
        public List<string> AnalyzeBranches()
        {
            var result = new List<string>();

            if (string.IsNullOrEmpty(Body))
                return result;

            // This is a simplified approach. A more robust solution would parse the AST.

            // Extract if conditions
            var ifPattern = @"if\s*\((.*?)\)";
            var ifMatches = Regex.Matches(Body, ifPattern, RegexOptions.Singleline);

            foreach (Match match in ifMatches)
            {
                if (match.Groups.Count > 1)
                {
                    string condition = match.Groups[1].Value.Trim();
                    result.Add(condition);
                }
            }

            // Extract while conditions
            var whilePattern = @"while\s*\((.*?)\)";
            var whileMatches = Regex.Matches(Body, whilePattern, RegexOptions.Singleline);

            foreach (Match match in whileMatches)
            {
                if (match.Groups.Count > 1)
                {
                    string condition = match.Groups[1].Value.Trim();
                    result.Add(condition);
                }
            }

            // Extract for conditions (second part)
            var forPattern = @"for\s*\((.*?);(.*?);(.*?)\)";
            var forMatches = Regex.Matches(Body, forPattern, RegexOptions.Singleline);

            foreach (Match match in forMatches)
            {
                if (match.Groups.Count > 2)
                {
                    string condition = match.Groups[2].Value.Trim();
                    if (!string.IsNullOrEmpty(condition))
                    {
                        result.Add(condition);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Calculates the cyclomatic complexity of the function
        /// </summary>
        /// <returns>The cyclomatic complexity value</returns>
        public int CalculateCyclomaticComplexity()
        {
            // Start with 1 (for the function itself)
            int complexity = 1;

            if (string.IsNullOrEmpty(Body))
                return complexity;

            // Count decision points
            complexity += Regex.Matches(Body, @"\bif\b").Count;
            complexity += Regex.Matches(Body, @"\belse\s+if\b").Count;
            complexity += Regex.Matches(Body, @"\bwhile\b").Count;
            complexity += Regex.Matches(Body, @"\bfor\b").Count;
            complexity += Regex.Matches(Body, @"\bcase\b").Count;
            complexity += Regex.Matches(Body, @"\bcatch\b").Count;
            complexity += Regex.Matches(Body, @"\b\|\|\b").Count;
            complexity += Regex.Matches(Body, @"\b&&\b").Count;
            complexity += Regex.Matches(Body, @"\?").Count; // Conditional operator

            return complexity;
        }

        /// <summary>
        /// Converts this function to a TestCase
        /// </summary>
        /// <returns>A TestCase for this function</returns>
        public TestCaseUser ToTestCase()
        {
            var testCase = new TestCaseUser
            {
                Name = $"Test_{Name}",
                Description = $"Test case for {Name} function",
                FunctionUnderTest = Name,
                Type = TestCaseType.UnitTest,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            // Add input parameters
            foreach (var param in Parameters)
            {
                if (!param.IsOutput)
                {
                    testCase.InputParameters.Add(param);
                }
            }

            // Add expected outputs
            if (ReturnType != "void")
            {
                testCase.ExpectedOutputs.Add(new CVariable
                {
                    Name = "return_value",
                    Type = ReturnType,
                    Value = GetDefaultValueForType(ReturnType)
                });
            }

            // Add output parameters
            foreach (var param in Parameters)
            {
                if (param.IsOutput)
                {
                    testCase.ExpectedOutputs.Add(param);
                }
            }

            return testCase;
        }

        /// <summary>
        /// Gets the default value for a given C type
        /// </summary>
        private object GetDefaultValueForType(string type)
        {
            string baseType = type.ToLower();

            if (baseType.Contains("char"))
                return '\0';
            if (baseType.Contains("int") || baseType.Contains("long") || baseType.Contains("short"))
                return 0;
            if (baseType.Contains("float") || baseType.Contains("double"))
                return 0.0;
            if (baseType.Contains("bool"))
                return false;

            return null;
        }
    }


}