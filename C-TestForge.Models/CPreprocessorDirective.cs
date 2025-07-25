using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using C_TestForge.Models.TestCases;

namespace C_TestForge.Models
{
    /// <summary>
    /// Represents a preprocessor directive in C source code
    /// </summary>
    public class CPreprocessorDirective
    {
        /// <summary>
        /// Gets or sets the unique identifier for the directive
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the type of preprocessor directive
        /// </summary>
        public PreprocessorType Type { get; set; }

        /// <summary>
        /// Gets or sets the line number in the source file where the directive appears
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the raw text of the directive as it appears in the source code
        /// </summary>
        public string RawText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value of the directive (e.g., the defined value for #define)
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the condition for conditional directives (e.g., the expression in #if)
        /// </summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the directive is currently enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the child directives within this directive's scope
        /// </summary>
        public List<CPreprocessorDirective> Children { get; set; } = new List<CPreprocessorDirective>();

        /// <summary>
        /// Gets or sets the parent directive (for nested directives)
        /// </summary>
        public CPreprocessorDirective Parent { get; set; }

        /// <summary>
        /// Gets or sets the source file where the directive is defined
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the end line number for block directives (like #if/#endif)
        /// </summary>
        public int EndLineNumber { get; set; }

        /// <summary>
        /// Gets or sets the name of the directive (e.g., the macro name in #define)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameters for function-like macros
        /// </summary>
        public List<string> Parameters { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether this is a function-like macro
        /// </summary>
        public bool IsFunctionLike { get; set; }

        /// <summary>
        /// Gets or sets whether this directive has been expanded
        /// </summary>
        public bool IsExpanded { get; set; }

        /// <summary>
        /// Gets or sets the expanded value after macro substitution
        /// </summary>
        public string ExpandedValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the include path for #include directives
        /// </summary>
        public string IncludePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this is a system include (using angle brackets)
        /// </summary>
        public bool IsSystemInclude { get; set; }

        /// <summary>
        /// Creates a deep copy of this preprocessor directive
        /// </summary>
        /// <returns>A new CPreprocessorDirective instance with the same properties</returns>
        public CPreprocessorDirective Clone()
        {
            var clone = new CPreprocessorDirective
            {
                Id = Guid.NewGuid(),
                Type = this.Type,
                LineNumber = this.LineNumber,
                RawText = this.RawText,
                Value = this.Value,
                Condition = this.Condition,
                IsEnabled = this.IsEnabled,
                SourceFile = this.SourceFile,
                EndLineNumber = this.EndLineNumber,
                Name = this.Name,
                IsFunctionLike = this.IsFunctionLike,
                IsExpanded = this.IsExpanded,
                ExpandedValue = this.ExpandedValue,
                IncludePath = this.IncludePath,
                IsSystemInclude = this.IsSystemInclude
            };

            // Clone parameters
            clone.Parameters = new List<string>(this.Parameters);

            // Clone children (without circular references)
            foreach (var child in this.Children)
            {
                var clonedChild = child.Clone();
                clonedChild.Parent = clone;
                clone.Children.Add(clonedChild);
            }

            return clone;
        }

        /// <summary>
        /// Converts the preprocessor directive to its C syntax
        /// </summary>
        /// <returns>A string representation of the directive as it would appear in C code</returns>
        public override string ToString()
        {
            switch (Type)
            {
                case PreprocessorType.Define:
                    if (IsFunctionLike)
                    {
                        return $"#define {Name}({string.Join(", ", Parameters)}) {Value}";
                    }
                    else
                    {
                        return $"#define {Name} {Value}";
                    }

                case PreprocessorType.Undef:
                    return $"#undef {Name}";

                case PreprocessorType.Include:
                    if (IsSystemInclude)
                    {
                        return $"#include <{IncludePath}>";
                    }
                    else
                    {
                        return $"#include \"{IncludePath}\"";
                    }

                case PreprocessorType.If:
                    return $"#if {Condition}";

                case PreprocessorType.IfDef:
                    return $"#ifdef {Name}";

                case PreprocessorType.IfNDef:
                    return $"#ifndef {Name}";

                case PreprocessorType.ElIf:
                    return $"#elif {Condition}";

                case PreprocessorType.Else:
                    return "#else";

                case PreprocessorType.EndIf:
                    return "#endif";

                case PreprocessorType.Pragma:
                    return $"#pragma {Value}";

                case PreprocessorType.Error:
                    return $"#error {Value}";

                case PreprocessorType.Warning:
                    return $"#warning {Value}";

                default:
                    return RawText;
            }
        }

        /// <summary>
        /// Evaluates the condition of this directive
        /// </summary>
        /// <param name="definedMacros">Dictionary of currently defined macros</param>
        /// <returns>True if the condition evaluates to true, false otherwise</returns>
        public bool EvaluateCondition(Dictionary<string, string> definedMacros)
        {
            switch (Type)
            {
                case PreprocessorType.IfDef:
                    return definedMacros.ContainsKey(Name);

                case PreprocessorType.IfNDef:
                    return !definedMacros.ContainsKey(Name);

                case PreprocessorType.If:
                case PreprocessorType.ElIf:
                    return EvaluateExpression(Condition, definedMacros);

                case PreprocessorType.Else:
                    // Else is handled in context of its parent
                    return false;

                default:
                    // Non-conditional directives
                    return true;
            }
        }

        /// <summary>
        /// Expands any macros in the given expression
        /// </summary>
        /// <param name="expression">The expression to evaluate</param>
        /// <param name="definedMacros">Dictionary of currently defined macros</param>
        /// <returns>True if the expression evaluates to a non-zero value, false otherwise</returns>
        private bool EvaluateExpression(string expression, Dictionary<string, string> definedMacros)
        {
            // This is a simplified implementation
            // A real implementation would parse and evaluate the expression properly

            // Replace defined(X) with 1 or 0
            foreach (var macro in definedMacros.Keys)
            {
                if (expression.Contains($"defined({macro})"))
                {
                    expression = expression.Replace($"defined({macro})", "1");
                }
                else if (expression.Contains($"defined {macro}"))
                {
                    expression = expression.Replace($"defined {macro}", "1");
                }
            }

            // Replace any undefined defined(X) with 0
            expression = System.Text.RegularExpressions.Regex.Replace(
                expression,
                @"defined\s*\(\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*\)",
                "0");

            expression = System.Text.RegularExpressions.Regex.Replace(
                expression,
                @"defined\s+([a-zA-Z_][a-zA-Z0-9_]*)",
                "0");

            // Replace macros with their values
            foreach (var macro in definedMacros)
            {
                expression = expression.Replace(macro.Key, macro.Value);
            }

            // Replace any remaining identifiers with 0
            expression = System.Text.RegularExpressions.Regex.Replace(
                expression,
                @"\b[a-zA-Z_][a-zA-Z0-9_]*\b",
                "0");

            // Try to evaluate the expression
            try
            {
                // This is a very simplified evaluation and would only work for simple expressions
                // A real implementation would use a proper expression evaluator
                return EvaluateSimpleExpression(expression) != 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Evaluates a simple preprocessor expression
        /// </summary>
        /// <param name="expression">Expression to evaluate</param>
        /// <returns>Result of the evaluation</returns>
        private int EvaluateSimpleExpression(string expression)
        {
            // This is a very simplified implementation that only handles basic expressions
            // Real implementation would use a proper expression evaluator

            expression = expression.Trim();

            // Handle negation
            if (expression.StartsWith("!"))
            {
                int valuee = EvaluateSimpleExpression(expression.Substring(1));
                return valuee == 0 ? 1 : 0;
            }

            // Handle parentheses
            if (expression.StartsWith("(") && expression.EndsWith(")"))
            {
                return EvaluateSimpleExpression(expression.Substring(1, expression.Length - 2));
            }

            // Handle && and ||
            if (expression.Contains("&&"))
            {
                string[] parts = expression.Split(new[] { "&&" }, StringSplitOptions.None);
                int result = 1;

                foreach (var part in parts)
                {
                    if (EvaluateSimpleExpression(part) == 0)
                    {
                        result = 0;
                        break;
                    }
                }

                return result;
            }

            if (expression.Contains("||"))
            {
                string[] parts = expression.Split(new[] { "||" }, StringSplitOptions.None);
                int result = 0;

                foreach (var part in parts)
                {
                    if (EvaluateSimpleExpression(part) != 0)
                    {
                        result = 1;
                        break;
                    }
                }

                return result;
            }

            // Try to parse as number
            if (int.TryParse(expression, out int value))
            {
                return value;
            }

            return 0;
        }

        /// <summary>
        /// Checks if this directive is a conditional directive
        /// </summary>
        /// <returns>True if the directive is conditional, false otherwise</returns>
        public bool IsConditionalDirective()
        {
            return Type == PreprocessorType.If ||
                   Type == PreprocessorType.IfDef ||
                   Type == PreprocessorType.IfNDef ||
                   Type == PreprocessorType.ElIf ||
                   Type == PreprocessorType.Else;
        }

        /// <summary>
        /// Checks if this directive forms a block with other directives
        /// </summary>
        /// <returns>True if the directive is part of a block, false otherwise</returns>
        public bool IsBlockDirective()
        {
            return Type == PreprocessorType.If ||
                   Type == PreprocessorType.IfDef ||
                   Type == PreprocessorType.IfNDef ||
                   Type == PreprocessorType.ElIf ||
                   Type == PreprocessorType.Else ||
                   Type == PreprocessorType.EndIf;
        }

        /// <summary>
        /// Gets all code affected by this directive
        /// </summary>
        /// <returns>String containing all code affected by this directive</returns>
        public string GetAffectedCode(string sourceCode)
        {
            if (LineNumber <= 0 || EndLineNumber <= 0 || EndLineNumber <= LineNumber)
                return string.Empty;

            string[] lines = sourceCode.Split('\n');

            if (LineNumber > lines.Length || EndLineNumber > lines.Length)
                return string.Empty;

            StringBuilder result = new StringBuilder();

            for (int i = LineNumber; i < EndLineNumber; i++)
            {
                result.AppendLine(lines[i - 1]);
            }

            return result.ToString();
        }
    }

}