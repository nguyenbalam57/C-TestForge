using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using C_TestForge.Core.Interfaces;
using C_TestForge.Models;
using C_TestForge.TestCase.Models;

namespace C_TestForge.TestCase.Services
{
    public class StubGeneratorService : IStubGeneratorService
    {
        public StubFunction GenerateStub(CFunction originalFunction)
        {
            if (originalFunction == null)
                throw new ArgumentNullException(nameof(originalFunction));

            var stub = new StubFunction
            {
                Name = originalFunction.Name,
                ReturnType = originalFunction.ReturnType,
                Parameters = originalFunction.Parameters.ToList(),
                ReturnValue = GetDefaultValue(originalFunction.ReturnType),
                ParameterBehaviors = new List<StubParameterBehavior>()
            };

            // For output parameters (pointers), add default behaviors
            foreach (var param in originalFunction.Parameters)
            {
                if (IsOutputParameter(param))
                {
                    stub.ParameterBehaviors.Add(new StubParameterBehavior
                    {
                        ParameterName = param.Name,
                        Action = StubParameterAction.SetValue,
                        Value = GetDefaultValue(GetBaseType(param.Type))
                    });
                }
            }

            return stub;
        }

        public string GenerateStubImplementation(StubFunction stub, string language = "c")
        {
            if (stub == null)
                throw new ArgumentNullException(nameof(stub));

            StringBuilder code = new StringBuilder();

            switch (language.ToLower())
            {
                case "c":
                    // Function signature
                    code.Append($"{stub.ReturnType} {stub.Name}(");

                    // Parameters
                    if (stub.Parameters != null && stub.Parameters.Any())
                    {
                        code.Append(string.Join(", ", stub.Parameters.Select(p => $"{p.Type} {p.Name}")));
                    }
                    else
                    {
                        code.Append("void");
                    }

                    code.AppendLine(")");
                    code.AppendLine("{");

                    // Add stub implementation
                    code.AppendLine($"    // Stub implementation for {stub.Name}");

                    // Handle parameter behaviors
                    if (stub.ParameterBehaviors != null)
                    {
                        foreach (var behavior in stub.ParameterBehaviors)
                        {
                            switch (behavior.Action)
                            {
                                case StubParameterAction.SetValue:
                                    var param = stub.Parameters.FirstOrDefault(p => p.Name == behavior.ParameterName);
                                    if (param != null)
                                    {
                                        string baseType = GetBaseType(param.Type);
                                        code.AppendLine($"    // Set output parameter {behavior.ParameterName}");
                                        code.AppendLine($"    if ({behavior.ParameterName} != NULL)");
                                        code.AppendLine($"    {{");
                                        code.AppendLine($"        *{behavior.ParameterName} = {FormatValueForC(behavior.Value, baseType)};");
                                        code.AppendLine($"    }}");
                                    }
                                    break;

                                case StubParameterAction.CopyBuffer:
                                    code.AppendLine($"    // Copy buffer to {behavior.ParameterName}");
                                    code.AppendLine($"    if ({behavior.ParameterName} != NULL && {behavior.BufferSize} > 0)");
                                    code.AppendLine($"    {{");
                                    if (behavior.Value is string strValue)
                                    {
                                        code.AppendLine($"        const char* source = \"{strValue}\";");
                                        code.AppendLine($"        size_t sourceLen = strlen(source);");
                                        code.AppendLine($"        size_t copyLen = sourceLen < {behavior.BufferSize} ? sourceLen : {behavior.BufferSize} - 1;");
                                        code.AppendLine($"        memcpy({behavior.ParameterName}, source, copyLen);");
                                        code.AppendLine($"        {behavior.ParameterName}[copyLen] = '\\0';");
                                    }
                                    else
                                    {
                                        code.AppendLine($"        // No string value provided for buffer copy");
                                        code.AppendLine($"        memset({behavior.ParameterName}, 0, {behavior.BufferSize});");
                                    }
                                    code.AppendLine($"    }}");
                                    break;
                            }
                        }
                    }

                    // Return statement
                    if (stub.ReturnType != "void")
                    {
                        code.AppendLine();
                        code.AppendLine($"    return {FormatValueForC(stub.ReturnValue, stub.ReturnType)};");
                    }

                    code.AppendLine("}");
                    break;

                default:
                    code.AppendLine($"// Unsupported language: {language}");
                    break;
            }

            return code.ToString();
        }

        private bool IsOutputParameter(CParameter parameter)
        {
            if (parameter == null)
                return false;

            // In C, output parameters are typically pointers
            return parameter.Type.Contains("*") && !parameter.Type.Contains("const");
        }

        private string GetBaseType(string pointerType)
        {
            if (string.IsNullOrEmpty(pointerType))
                return "void";

            // Remove pointer
            return pointerType.Replace("*", "").Trim();
        }

        private object GetDefaultValue(string type)
        {
            // Return appropriate default values based on C type
            switch (type.ToLower())
            {
                case "int":
                case "int32_t":
                case "int16_t":
                case "int8_t":
                case "uint32_t":
                case "uint16_t":
                case "uint8_t":
                case "unsigned int":
                case "unsigned char":
                case "short":
                case "long":
                    return 0;

                case "float":
                case "double":
                    return 0.0;

                case "char":
                    return '\0';

                case "bool":
                    return false;

                case "char*":
                case "const char*":
                    return "";

                default:
                    if (type.EndsWith("*"))
                        return "NULL";
                    return 0;
            }
        }

        private string FormatValueForC(object value, string type)
        {
            if (value == null)
                return "NULL";

            // Format the value based on C type
            switch (type.ToLower())
            {
                case "char*":
                case "const char*":
                    return $"\"{value}\"";

                case "char":
                    if (value is string s && s.Length > 0)
                        return $"'{s[0]}'";
                    else if (value is char c)
                        return $"'{c}'";
                    return "'\\0'";

                case "float":
                    return $"{value}f";

                case "bool":
                    return (value is bool b && b) || (value is string str && (str == "true" || str == "1")) ? "true" : "false";

                default:
                    if (type.EndsWith("*") && !(value is string) && value.GetType().IsValueType)
                        return "NULL";
                    return value.ToString();
            }
        }
    }

    public interface IStubGeneratorService
    {
        StubFunction GenerateStub(CFunction originalFunction);
        string GenerateStubImplementation(StubFunction stub, string language = "c");
    }
}