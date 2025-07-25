using System;
using System.Collections.Generic;
using System.Text;
using C_TestForge.Models.TestCases;

namespace C_TestForge.Models
{
    /// <summary>
    /// Represents a variable in C source code
    /// </summary>
    public class CVariable
    {
        /// <summary>
        /// Gets or sets the unique identifier for the variable
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the name of the parameter
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data type of the parameter
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the storage class of the variable (auto, register, static, extern)
        /// </summary>
        public VariableStorageClass StorageClass { get; set; }

        /// <summary>
        /// Gets or sets the scope of the variable (local, global, file)
        /// </summary>
        public VariableScope Scope { get; set; }

        /// <summary>
        /// Gets or sets the default value of the variable as a string
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the variable is used as a test case
        /// </summary>
        public bool IsTestCase { get; set; } = false;

        /// <summary>
        /// Gets or sets the setter expression for the variable được sử dụng trong tạo testcase
        /// </summary>
        public object Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the variable is constant
        /// </summary>
        public bool IsConstant { get; set; }

        /// <summary>
        /// Gets or sets whether the variable is an array
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Gets or sets whether the variable is a pointer
        /// </summary>
        public bool IsPointer { get; set; }

        /// <summary>
        /// Gets or sets the array size (0 means not an array or dynamic array)
        /// </summary>
        public int ArraySize { get; set; } = 0;

        /// <summary>
        /// Gets or sets the line number where the variable is defined
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets whether the variable is enabled (not disabled by preprocessing)
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum value for the variable (for numeric types)
        /// </summary>
        public string MinValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum value for the variable (for numeric types)
        /// </summary>
        public string MaxValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameter direction (input, output, or input/output)
        /// </summary>
        public ParameterDirection Direction { get; set; } = ParameterDirection.None;

        /// <summary>
        /// Gets or sets the description of the parameter
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the parameter is an output parameter
        /// </summary>
        public bool IsOutput { get; set; }

        /// <summary>
        /// Gets or sets whether the variable should be stubbed
        /// </summary>
        public bool IsStub { get; set; } = false;

        /// <summary>
        /// Gets or sets the possible values for the variable (for enums or limited value sets)
        /// </summary>
        public List<string> PossibleValues { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the parent function of this variable (if it's a local variable)
        /// </summary>
        public CFunction ParentFunction { get; set; } = new CFunction();

        /// <summary>
        /// Gets or sets the parent preprocessor directive (if defined in a conditional block)
        /// </summary>
        public CPreprocessorDirective ParentDirective { get; set; } = new CPreprocessorDirective();

        /// <summary>
        /// Gets or sets the possible enum values for this variable (if applicable)
        /// </summary>
        public List<object> EnumValues { get; set; } = new List<object>();

        /// <summary>
        /// Gets or sets the source file where the variable is defined
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional attributes of the variable
        /// </summary>
        public string Attributes { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the pointer level (1 for *, 2 for **, etc.)
        /// </summary>
        public int PointerLevel { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether the variable is volatile
        /// </summary>
        public bool IsVolatile { get; set; }

        /// <summary>
        /// Creates a deep copy of this variable
        /// </summary>
        /// <returns>A new CVariable instance with the same properties</returns>
        public CVariable Clone()
        {
            var clone = new CVariable
            {
                Id = Guid.NewGuid(),
                Name = this.Name,
                Type = this.Type,
                StorageClass = this.StorageClass,
                Scope = this.Scope,
                DefaultValue = this.DefaultValue,
                IsTestCase = this.IsTestCase,
                Value = this.Value,
                IsConstant = this.IsConstant,
                IsArray = this.IsArray,
                IsPointer = this.IsPointer,
                ArraySize = this.ArraySize,
                LineNumber = this.LineNumber,
                IsEnabled = this.IsEnabled,
                MinValue = this.MinValue,
                MaxValue = this.MaxValue,
                Direction = this.Direction,
                Description = this.Description,
                IsOutput = this.IsOutput,
                IsStub = this.IsStub,
                SourceFile = this.SourceFile,
                Attributes = this.Attributes,
                PointerLevel = this.PointerLevel,
                IsVolatile = this.IsVolatile
            };

            // Deep copy the lists
            foreach (var value in this.PossibleValues)
            {
                clone.PossibleValues.Add(value);
            }

            foreach (var value in this.EnumValues)
            {
                clone.EnumValues.Add(value);
            }

            // Clone parent objects if needed
            if (this.ParentFunction != null)
            {
                // Avoid circular references in cloning
                clone.ParentFunction = new CFunction
                {
                    Id = this.ParentFunction.Id,
                    Name = this.ParentFunction.Name
                    // Add other essential properties without causing infinite recursion
                };
            }

            if (this.ParentDirective != null)
            {
                // Avoid circular references in cloning
                clone.ParentDirective = new CPreprocessorDirective
                {
                    Id = this.ParentDirective.Id,
                    RawText = this.ParentDirective.RawText
                    // Add other essential properties without causing infinite recursion
                };
            }

            return clone;
        }

        /// <summary>
        /// Converts the variable to its C declaration
        /// </summary>
        /// <returns>A string representation of the variable as a C declaration</returns>
        public override string ToString()
        {
            var builder = new StringBuilder();

            // Add storage class if applicable
            if (StorageClass != VariableStorageClass.Auto)
            {
                builder.Append(StorageClass.ToString().ToLower()).Append(' ');
            }

            // Add const if applicable
            if (IsConstant)
            {
                builder.Append("const ");
            }

            // Add volatile if applicable
            if (IsVolatile)
            {
                builder.Append("volatile ");
            }

            // Add type
            builder.Append(Type).Append(' ');

            // Add pointer asterisks
            if (IsPointer)
            {
                builder.Append(new string('*', PointerLevel));
            }

            // Add name
            builder.Append(Name);

            // Add array brackets if needed
            if (IsArray)
            {
                if (ArraySize > 0)
                {
                    builder.Append($"[{ArraySize}]");
                }
                else
                {
                    builder.Append("[]");
                }
            }

            // Add initialization if available
            if (!string.IsNullOrEmpty(DefaultValue))
            {
                builder.Append(" = ").Append(DefaultValue);
            }

            // End with semicolon
            builder.Append(';');

            return builder.ToString();
        }

        /// <summary>
        /// Converts the variable to a C declaration without semicolon (for parameter declarations)
        /// </summary>
        /// <returns>A string representation of the variable as a C parameter declaration</returns>
        public string ToCString()
        {
            var declaration = ToString();
            return declaration.EndsWith(";") ? declaration.Substring(0, declaration.Length - 1) : declaration;
        }

        /// <summary>
        /// Checks if this variable is a local variable
        /// </summary>
        /// <returns>True if the variable is local, false otherwise</returns>
        public bool IsLocalVariable()
        {
            return Scope == VariableScope.Local;
        }

        /// <summary>
        /// Checks if this variable is a global variable
        /// </summary>
        /// <returns>True if the variable is global, false otherwise</returns>
        public bool IsGlobalVariable()
        {
            return Scope == VariableScope.Global;
        }

        /// <summary>
        /// Checks if this variable is a function parameter
        /// </summary>
        /// <returns>True if the variable is a function parameter, false otherwise</returns>
        public bool IsFunctionParameter()
        {
            return Scope == VariableScope.Parameter;
        }

        /// <summary>
        /// Checks if this variable is read-only
        /// </summary>
        /// <returns>True if the variable is read-only, false otherwise</returns>
        public bool IsReadOnly()
        {
            return IsConstant || (IsPointer && PointerLevel == 1 && Type.Contains("const"));
        }

        /// <summary>
        /// Determines if the variable is an output parameter
        /// </summary>
        /// <returns>True if the variable is an output parameter, false otherwise</returns>
        public bool IsOutputParameter()
        {
            return IsOutput ||
                   Direction == ParameterDirection.Output ||
                   Direction == ParameterDirection.InputOutput ||
                   (IsPointer && !IsConstant && IsFunctionParameter());
        }

        /// <summary>
        /// Gets the estimated size in bytes of this variable based on its type
        /// </summary>
        /// <returns>The estimated size in bytes</returns>
        public int GetEstimatedSize()
        {
            int baseSize = GetBaseSizeForType();

            if (IsArray)
            {
                return ArraySize > 0 ? baseSize * ArraySize : baseSize;
            }

            return baseSize;
        }

        /// <summary>
        /// Generates a setter code snippet for this variable
        /// </summary>
        /// <param name="value">The value to set</param>
        /// <returns>A string containing C code to set the variable's value</returns>
        public string GenerateSetter(string value)
        {
            // If custom setter is specified, use it with the value
            if (!string.IsNullOrEmpty(value))
            {
                return value.Replace("${value}", value);
            }

            // Otherwise generate a default setter
            if (IsPointer)
            {
                return $"*{Name} = {value};";
            }

            return $"{Name} = {value};";
        }

        /// <summary>
        /// Determines if the variable should be included in a test case
        /// </summary>
        /// <returns>True if the variable should be included in a test case, false otherwise</returns>
        public bool ShouldIncludeInTestCase()
        {
            // Include if explicitly marked as test case
            if (IsTestCase)
                return true;

            // Include if it's a parameter
            if (IsFunctionParameter())
                return true;

            // Include if it's a global and enabled
            if (IsGlobalVariable() && IsEnabled)
                return true;

            // Don't include if it's a constant
            if (IsConstant)
                return false;

            // Include if it's a local with default value and enabled
            return IsLocalVariable() && !string.IsNullOrEmpty(DefaultValue) && IsEnabled;
        }

        /// <summary>
        /// Converts string value to appropriate type based on the C type
        /// </summary>
        private object ConvertToAppropriateType(string value, string type)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            try
            {
                string lowerType = type.ToLower();

                if (lowerType.Contains("char") && value.Length == 1)
                    return value[0];

                if (lowerType.Contains("int") || lowerType.Contains("long") || lowerType.Contains("short"))
                {
                    if (int.TryParse(value, out int intResult))
                        return intResult;

                    // Try to evaluate hexadecimal values
                    if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        string hexValue = value.Substring(2);
                        if (int.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out int hexResult))
                            return hexResult;
                    }
                }

                if (lowerType.Contains("float") || lowerType.Contains("double"))
                {
                    if (double.TryParse(value, out double doubleResult))
                        return doubleResult;
                }

                if (lowerType.Contains("bool"))
                {
                    if (bool.TryParse(value, out bool boolResult))
                        return boolResult;
                    return value == "1";
                }

                // For pointers, return null or the string based on value
                if (IsPointer)
                {
                    if (value == "NULL" || value == "0")
                        return null;

                    // For string literals like "abc"
                    if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                        return value.Substring(1, value.Length - 2);
                }
            }
            catch
            {
                // If any conversion fails, return the original string
            }

            return value;
        }

        private int GetBaseSizeForType()
        {
            string baseType = Type.ToLower();

            if (baseType.Contains("char"))
                return 1;
            if (baseType.Contains("short") || baseType.Contains("int16"))
                return 2;
            if (baseType.Contains("int") || baseType.Contains("long") || baseType.Contains("float") || baseType.Contains("int32"))
                return 4;
            if (baseType.Contains("double") || baseType.Contains("int64"))
                return 8;
            if (baseType.Contains("pointer") || IsPointer || baseType.EndsWith("*"))
                return 4; // Assuming 32-bit pointers, use 8 for 64-bit

            return 4; // Default size
        }
    }
    
}