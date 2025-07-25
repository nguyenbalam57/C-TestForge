using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace C_TestForge.Models
{
    #region Core Models

    /// <summary>
    /// Type of preprocessor definition
    /// </summary>
    public enum DefinitionType
    {
        MacroConstant,
        MacroFunction,
        EnumValue
    }

    /// <summary>
    /// Represents a preprocessor definition in C code
    /// </summary>
    public class CDefinition : SourceCodeEntity
    {
        /// <summary>
        /// Value of the definition
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Whether the definition is a function-like macro
        /// </summary>
        public bool IsFunctionLike { get; set; }

        /// <summary>
        /// Parameters of a function-like macro
        /// </summary>
        public List<string> Parameters { get; set; } = new List<string>();

        /// <summary>
        /// Type of the definition
        /// </summary>
        public DefinitionType DefinitionType { get; set; }

        /// <summary>
        /// List of definitions that this definition depends on
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Whether the definition is enabled in the current configuration
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Get a string representation of the definition
        /// </summary>
        public override string ToString()
        {
            if (IsFunctionLike)
            {
                string parameters = string.Join(", ", Parameters);
                return $"#define {Name}({parameters}) {Value}";
            }
            else
            {
                return $"#define {Name} {Value}";
            }
        }

        /// <summary>
        /// Create a clone of the definition
        /// </summary>
        public CDefinition Clone()
        {
            return new CDefinition
            {
                Id = Id,
                Name = Name,
                Value = Value,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                IsFunctionLike = IsFunctionLike,
                Parameters = Parameters != null ? new List<string>(Parameters) : new List<string>(),
                DefinitionType = DefinitionType,
                Dependencies = Dependencies != null ? new List<string>(Dependencies) : new List<string>(),
                IsEnabled = IsEnabled
            };
        }
    }

    /// <summary>
    /// Scope of a variable in C code
    /// </summary>
    public enum VariableScope
    {
        Global,
        Static,
        Local,
        Parameter,
        Rom
    }

    /// <summary>
    /// Type of a variable in C code
    /// </summary>
    public enum VariableType
    {
        Primitive,
        Array,
        Pointer,
        Struct,
        Union,
        Enum
    }

    /// <summary>
    /// Represents a variable in C code
    /// </summary>
    public class CVariable : SourceCodeEntity
    {
        /// <summary>
        /// Type of the variable as a string
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Type of the variable
        /// </summary>
        public VariableType VariableType { get; set; }

        /// <summary>
        /// Scope of the variable
        /// </summary>
        public VariableScope Scope { get; set; }

        /// <summary>
        /// Default value of the variable
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Whether the variable is constant
        /// </summary>
        public bool IsConst { get; set; }

        /// <summary>
        /// Whether the variable is read-only
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Whether the variable is volatile
        /// </summary>
        public bool IsVolatile { get; set; }

        /// <summary>
        /// Size of the variable in bytes
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Constraints on the variable
        /// </summary>
        public List<VariableConstraint> Constraints { get; set; } = new List<VariableConstraint>();

        /// <summary>
        /// Functions that use this variable
        /// </summary>
        [JsonIgnore]
        public List<string> UsedByFunctions { get; set; } = new List<string>();

        /// <summary>
        /// Get a string representation of the variable
        /// </summary>
        public override string ToString()
        {
            string constModifier = IsConst ? "const " : "";
            string volatileModifier = IsVolatile ? "volatile " : "";
            string defaultValueStr = DefaultValue != null ? $" = {DefaultValue}" : "";

            return $"{constModifier}{volatileModifier}{TypeName} {Name}{defaultValueStr}";
        }

        /// <summary>
        /// Create a clone of the variable
        /// </summary>
        public CVariable Clone()
        {
            return new CVariable
            {
                Id = Id,
                Name = Name,
                TypeName = TypeName,
                VariableType = VariableType,
                Scope = Scope,
                DefaultValue = DefaultValue,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                IsConst = IsConst,
                IsReadOnly = IsReadOnly,
                IsVolatile = IsVolatile,
                Size = Size,
                Constraints = Constraints?.Select(c => c.Clone()).ToList() ?? new List<VariableConstraint>(),
                UsedByFunctions = UsedByFunctions != null ? new List<string>(UsedByFunctions) : new List<string>()
            };
        }
    }

    /// <summary>
    /// Type of constraint on a variable
    /// </summary>
    public enum ConstraintType
    {
        MinValue,
        MaxValue,
        Enumeration,
        Range,
        Custom
    }

    /// <summary>
    /// Represents a constraint on a variable
    /// </summary>
    public class VariableConstraint : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Type of the constraint
        /// </summary>
        public ConstraintType Type { get; set; }

        /// <summary>
        /// Minimum value of the variable
        /// </summary>
        public string MinValue { get; set; }

        /// <summary>
        /// Maximum value of the variable
        /// </summary>
        public string MaxValue { get; set; }

        /// <summary>
        /// List of allowed values for an enumeration
        /// </summary>
        public List<string> AllowedValues { get; set; } = new List<string>();

        /// <summary>
        /// Custom constraint expression
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Source of the constraint (e.g., function name, line number)
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Get a string representation of the constraint
        /// </summary>
        public override string ToString()
        {
            switch (Type)
            {
                case ConstraintType.MinValue:
                    return $">= {MinValue}";
                case ConstraintType.MaxValue:
                    return $"<= {MaxValue}";
                case ConstraintType.Range:
                    return $"{MinValue} to {MaxValue}";
                case ConstraintType.Enumeration:
                    return $"One of: {string.Join(", ", AllowedValues)}";
                case ConstraintType.Custom:
                    return Expression;
                default:
                    return "Unknown constraint";
            }
        }

        /// <summary>
        /// Check if a value satisfies this constraint
        /// </summary>
        public bool IsSatisfied(string value)
        {
            // Implementation depends on the constraint type and value type
            // This is a placeholder for the actual implementation
            return true;
        }

        /// <summary>
        /// Create a clone of the constraint
        /// </summary>
        public VariableConstraint Clone()
        {
            return new VariableConstraint
            {
                Id = Id,
                Type = Type,
                MinValue = MinValue,
                MaxValue = MaxValue,
                AllowedValues = AllowedValues != null ? new List<string>(AllowedValues) : new List<string>(),
                Expression = Expression,
                Source = Source
            };
        }
    }

    /// <summary>
    /// Represents a function in C code
    /// </summary>
    public class CFunction : SourceCodeEntity
    {
        /// <summary>
        /// Return type of the function
        /// </summary>
        public string ReturnType { get; set; }

        /// <summary>
        /// List of parameters
        /// </summary>
        public List<CVariable> Parameters { get; set; } = new List<CVariable>();

        /// <summary>
        /// Whether the function is static
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Whether the function is inline
        /// </summary>
        public bool IsInline { get; set; }

        /// <summary>
        /// Whether the function is external
        /// </summary>
        public bool IsExternal { get; set; }

        /// <summary>
        /// Body of the function
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// List of functions called by this function
        /// </summary>
        public List<string> CalledFunctions { get; set; } = new List<string>();

        /// <summary>
        /// List of variables used by this function
        /// </summary>
        public List<string> UsedVariables { get; set; } = new List<string>();

        /// <summary>
        /// Get the function signature
        /// </summary>
        [JsonIgnore]
        public string Signature
        {
            get
            {
                string staticModifier = IsStatic ? "static " : "";
                string inlineModifier = IsInline ? "inline " : "";
                string paramList = string.Join(", ", Parameters.Select(p => p.ToString()));

                return $"{staticModifier}{inlineModifier}{ReturnType} {Name}({paramList})";
            }
        }

        /// <summary>
        /// Get a string representation of the function
        /// </summary>
        public override string ToString()
        {
            return Signature;
        }

        /// <summary>
        /// Create a clone of the function
        /// </summary>
        public CFunction Clone()
        {
            return new CFunction
            {
                Id = Id,
                Name = Name,
                ReturnType = ReturnType,
                Parameters = Parameters?.Select(p => p.Clone()).ToList() ?? new List<CVariable>(),
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                IsStatic = IsStatic,
                IsInline = IsInline,
                IsExternal = IsExternal,
                Body = Body,
                CalledFunctions = CalledFunctions != null ? new List<string>(CalledFunctions) : new List<string>(),
                UsedVariables = UsedVariables != null ? new List<string>(UsedVariables) : new List<string>()
            };
        }
    }

    /// <summary>
    /// Type of conditional directive
    /// </summary>
    public enum ConditionalType
    {
        If,
        IfDef,
        IfNDef,
        ElseIf,
        Else
    }

    /// <summary>
    /// Represents a conditional directive in C code
    /// </summary>
    public class ConditionalDirective : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Type of the conditional directive
        /// </summary>
        public ConditionalType Type { get; set; }

        /// <summary>
        /// Condition of the directive
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Line number in the source file
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// End line number in the source file
        /// </summary>
        public int EndLineNumber { get; set; }

        /// <summary>
        /// Source file where the directive is defined
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Parent directive (for else/elif)
        /// </summary>
        [JsonIgnore]
        public ConditionalDirective ParentDirective { get; set; }

        /// <summary>
        /// List of branch directives (else/elif)
        /// </summary>
        public List<ConditionalDirective> Branches { get; set; } = new List<ConditionalDirective>();

        /// <summary>
        /// List of definitions that this directive depends on
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Whether the condition is currently satisfied
        /// </summary>
        [JsonIgnore]
        public bool IsConditionSatisfied { get; set; }

        /// <summary>
        /// ID of the parent directive
        /// </summary>
        public string ParentDirectiveId => ParentDirective?.Id;

        /// <summary>
        /// Get a string representation of the conditional directive
        /// </summary>
        public override string ToString()
        {
            switch (Type)
            {
                case ConditionalType.If:
                    return $"#if {Condition}";
                case ConditionalType.IfDef:
                    return $"#ifdef {Condition}";
                case ConditionalType.IfNDef:
                    return $"#ifndef {Condition}";
                case ConditionalType.ElseIf:
                    return $"#elif {Condition}";
                case ConditionalType.Else:
                    return "#else";
                default:
                    return "Unknown conditional";
            }
        }

        /// <summary>
        /// Create a clone of the conditional directive
        /// </summary>
        public ConditionalDirective Clone()
        {
            var clone = new ConditionalDirective
            {
                Id = Id,
                Type = Type,
                Condition = Condition,
                LineNumber = LineNumber,
                EndLineNumber = EndLineNumber,
                SourceFile = SourceFile,
                // Don't clone parent to avoid circular references
                IsConditionSatisfied = IsConditionSatisfied,
                Dependencies = Dependencies != null ? new List<string>(Dependencies) : new List<string>()
            };

            // Clone branches
            clone.Branches = Branches?.Select(b => b.Clone()).ToList() ?? new List<ConditionalDirective>();
            foreach (var branch in clone.Branches)
            {
                branch.ParentDirective = clone;
            }

            return clone;
        }
    }

    /// <summary>
    /// Represents an include directive in C code
    /// </summary>
    public class IncludeDirective : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Path to the included file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Line number in the source file
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Whether the include is a system include (<>) or a local include ("")
        /// </summary>
        public bool IsSystemInclude { get; set; }

        /// <summary>
        /// Get a string representation of the include directive
        /// </summary>
        public override string ToString()
        {
            if (IsSystemInclude)
            {
                return $"#include <{FilePath}>";
            }
            else
            {
                return $"#include \"{FilePath}\"";
            }
        }

        /// <summary>
        /// Create a clone of the include directive
        /// </summary>
        public IncludeDirective Clone()
        {
            return new IncludeDirective
            {
                Id = Id,
                FilePath = FilePath,
                LineNumber = LineNumber,
                IsSystemInclude = IsSystemInclude
            };
        }
    }

    /// <summary>
    /// Severity of a parse error
    /// </summary>
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Represents an error that occurred during parsing
    /// </summary>
    public class ParseError : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Line number in the source file
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Column number in the source file
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// Source file where the error occurred
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Severity of the error
        /// </summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>
        /// Get a string representation of the parse error
        /// </summary>
        public override string ToString()
        {
            return $"{Severity}: {FileName}({LineNumber},{ColumnNumber}): {Message}";
        }

        /// <summary>
        /// Create a clone of the parse error
        /// </summary>
        public ParseError Clone()
        {
            return new ParseError
            {
                Id = Id,
                Message = Message,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                FileName = FileName,
                Severity = Severity
            };
        }
    }

    /// <summary>
    /// Represents a relationship between two functions
    /// </summary>
    public class FunctionRelationship : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the caller function
        /// </summary>
        public string CallerName { get; set; }

        /// <summary>
        /// Name of the callee function
        /// </summary>
        public string CalleeName { get; set; }

        /// <summary>
        /// Line number in the caller function
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Source file where the call occurs
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Get a string representation of the function relationship
        /// </summary>
        public override string ToString()
        {
            return $"{CallerName} calls {CalleeName} at {SourceFile}:{LineNumber}";
        }

        /// <summary>
        /// Create a clone of the function relationship
        /// </summary>
        public FunctionRelationship Clone()
        {
            return new FunctionRelationship
            {
                Id = Id,
                CallerName = CallerName,
                CalleeName = CalleeName,
                LineNumber = LineNumber,
                SourceFile = SourceFile
            };
        }
    }

    #endregion
}
