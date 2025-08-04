using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace C_TestForge.Parser.Analysis
{
    /// <summary>
    /// Implementation of the type manager
    /// </summary>
    public class TypeManager : ITypeManager
    {
        private readonly ILogger<TypeManager> _logger;
        private readonly IFileService _fileService;
        private readonly string _configPath = "typedef_mappings.json";

        private TypedefConfig _typedefConfig;
        private Dictionary<string, TypedefMapping> _typedefMap;

        /// <summary>
        /// Constructor for TypeManager
        /// </summary>
        public TypeManager(ILogger<TypeManager> logger, IFileService fileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _typedefMap = new Dictionary<string, TypedefMapping>(StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public async Task InitializeAsync()
        {
            try
            {
                await LoadTypedefConfigAsync();
                BuildTypedefMap();
                _logger.LogInformation($"TypeManager initialized with {_typedefMap.Count} typedef mappings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing TypeManager");
                // Create default config if loading fails
                _typedefConfig = CreateDefaultConfig();
                BuildTypedefMap();
            }
        }

        /// <inheritdoc/>
        public void AddTypedef(string userType, string baseType, string source = "Runtime")
        {
            if (string.IsNullOrEmpty(userType) || string.IsNullOrEmpty(baseType))
                return;

            var mapping = new TypedefMapping
            {
                UserType = userType,
                BaseType = baseType,
                Source = source
            };

            // Try to derive size and range constraints from base type
            DeriveConstraintsFromBaseType(mapping);

            // Add to our map
            _typedefMap[userType] = mapping;

            // Add to detected typedefs list if it's from header analysis
            if (source.Contains("Header"))
            {
                // Check if it already exists
                var existing = _typedefConfig.DetectedTypedefs.FirstOrDefault(t =>
                    string.Equals(t.UserType, userType, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    // Update existing
                    existing.BaseType = baseType;
                    existing.MinValue = mapping.MinValue;
                    existing.MaxValue = mapping.MaxValue;
                    existing.Size = mapping.Size;
                    existing.Source = source;
                }
                else
                {
                    // Add new
                    _typedefConfig.DetectedTypedefs.Add(mapping);
                }
            }
            else if (source.Contains("Source") || source.Contains("Usage") || source.Contains("Inferred"))
            {
                // Đã tồn tại một typedef tương tự?
                var existing = _typedefConfig.LearnedTypedefs?.FirstOrDefault(t =>
                    string.Equals(t.UserType, userType, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    // Cập nhật
                    existing.BaseType = baseType;
                    existing.MinValue = mapping.MinValue;
                    existing.MaxValue = mapping.MaxValue;
                    existing.Size = mapping.Size;
                    existing.Source = source;
                }
                else
                {
                    // Thêm mới
                    if (_typedefConfig.LearnedTypedefs == null)
                        _typedefConfig.LearnedTypedefs = new List<TypedefMapping>();

                    _typedefConfig.LearnedTypedefs.Add(mapping);
                }
            }
        }

        /// <inheritdoc/>
        public bool TryResolveType(string typeName, out string baseType)
        {
            baseType = null;
            if (string.IsNullOrEmpty(typeName))
                return false;

            // Remove const, volatile, etc.
            string cleanTypeName = CleanTypeName(typeName);

            // Check our mapping
            if (_typedefMap.TryGetValue(cleanTypeName, out var mapping))
            {
                baseType = mapping.BaseType;
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public VariableConstraint GetConstraintForType(string typeName, string variableName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            // Clean the type name
            string cleanTypeName = CleanTypeName(typeName);

            // First try direct mapping
            if (_typedefMap.TryGetValue(cleanTypeName, out var mapping))
            {
                return CreateConstraintFromMapping(mapping, variableName);
            }

            // Try to resolve basic C types
            return GetConstraintForBasicType(cleanTypeName, variableName);
        }

        /// <inheritdoc/>
        public async Task AnalyzeHeaderFilesAsync(IEnumerable<string> headerPaths)
        {
            if (headerPaths == null || !headerPaths.Any())
                return;

            int detectedCount = 0;

            foreach (var headerPath in headerPaths)
            {
                try
                {
                    if (!_fileService.FileExists(headerPath))
                    {
                        _logger.LogWarning($"Header file not found: {headerPath}");
                        continue;
                    }

                    string content = await _fileService.ReadFileAsync(headerPath);

                    // Extract typedefs
                    var typedefPattern = new Regex(@"typedef\s+([^\s]+(?:\s+[^\s]+)*)\s+([^\s;]+)\s*;");
                    var matches = typedefPattern.Matches(content);

                    foreach (Match match in matches)
                    {
                        string baseType = match.Groups[1].Value.Trim();
                        string userType = match.Groups[2].Value.Trim();

                        // Skip if already exists in predefined mappings
                        if (_typedefConfig.TypedefMappings.Any(t =>
                            string.Equals(t.UserType, userType, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        AddTypedef(userType, baseType, $"Header: {Path.GetFileName(headerPath)}");
                        detectedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error analyzing header file: {headerPath}");
                }
            }

            _logger.LogInformation($"Detected {detectedCount} new typedefs from header files");

            if (detectedCount > 0)
            {
                await SaveTypedefConfigAsync();
            }
        }

        /// <inheritdoc/>
        public async Task SaveTypedefConfigAsync()
        {
            try
            {
                // Sắp xếp các typedef để dễ đọc
                _typedefConfig.TypedefMappings = _typedefConfig.TypedefMappings
                    .OrderBy(t => t.UserType)
                    .ToList();

                _typedefConfig.DetectedTypedefs = _typedefConfig.DetectedTypedefs
                    .OrderBy(t => t.UserType)
                    .ToList();

                if (_typedefConfig.LearnedTypedefs != null)
                {
                    _typedefConfig.LearnedTypedefs = _typedefConfig.LearnedTypedefs
                        .OrderBy(t => t.UserType)
                        .ToList();
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(_typedefConfig, options);
                await _fileService.WriteFileAsync(_configPath, json);
                _logger.LogInformation($"Saved typedef configuration to {_configPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving typedef configuration: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public string DetectOriginalTypeFromSourceCode(string normalizedType, string sourceCode, int lineNumber)
        {
            if (string.IsNullOrEmpty(sourceCode) || lineNumber <= 0)
                return normalizedType;

            try
            {
                // Lấy dòng mã nguồn tại vị trí biến
                string[] lines = sourceCode.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lineNumber > lines.Length)
                    return normalizedType;

                string line = lines[lineNumber - 1];
                _logger.LogDebug($"Examining line {lineNumber}: {line}");

                // Phân tích dòng code để tìm kiểu tùy chỉnh
                // 1. Bỏ qua các từ khóa lưu trữ và qualifier
                line = Regex.Replace(line, @"\b(static|extern|auto|register|volatile|const)\b", "").Trim();

                // 2. Tìm kiểu tùy chỉnh trong dòng
                foreach (var mapping in _typedefMap)
                {
                    string userType = mapping.Key;
                    string baseType = mapping.Value.BaseType;

                    // Kiểm tra nếu dòng chứa kiểu tùy chỉnh và kiểu chuẩn hóa khớp với kiểu cơ bản
                    if (Regex.IsMatch(line, $@"\b{Regex.Escape(userType)}\b") &&
                        IsCompatibleType(normalizedType, baseType))
                    {
                        _logger.LogInformation($"Detected original type '{userType}' for normalized type '{normalizedType}'");
                        return userType;
                    }
                }

                // 3. Kiểm tra các kiểu phổ biến mà có thể chưa được đăng ký
                var commonTypes = new Dictionary<string, string>
                {
                    { "UINT8", "unsigned char" },
                    { "UINT16", "unsigned short" },
                    { "UINT32", "unsigned (int|long)" },
                    { "UINT64", "unsigned long long" },
                    { "INT8", "(signed )?char" },
                    { "INT16", "(signed )?short" },
                    { "INT32", "(signed )?(int|long)" },
                    { "INT64", "(signed )?long long" }
                };

                foreach (var type in commonTypes)
                {
                    if (Regex.IsMatch(line, $@"\b{Regex.Escape(type.Key)}\b") &&
                        Regex.IsMatch(normalizedType, type.Value, RegexOptions.IgnoreCase))
                    {
                        // Đăng ký kiểu mới phát hiện
                        AddTypedef(type.Key, Regex.Replace(normalizedType, @"(const|volatile)\s+", ""), "DetectedFromSourceCode");
                        _logger.LogInformation($"Auto-registered type mapping: {type.Key} -> {normalizedType}");
                        return type.Key;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error detecting original type from source code: {ex.Message}");
            }

            return normalizedType;
        }

        /// <inheritdoc/>
        public void RegisterTypeAliasesFromSourceCode(string sourceCode)
        {
            if (string.IsNullOrEmpty(sourceCode))
                return;

            try
            {
                // 1. Tìm typedef trực tiếp
                var typedefPattern = new Regex(@"typedef\s+([^\s]+(?:\s+[^\s]+)*)\s+([^\s;]+)\s*;");
                var typedefMatches = typedefPattern.Matches(sourceCode);

                foreach (Match match in typedefMatches)
                {
                    string baseType = match.Groups[1].Value.Trim();
                    string userType = match.Groups[2].Value.Trim();

                    // Bỏ qua nếu đã có trong định nghĩa
                    if (!_typedefMap.ContainsKey(userType))
                    {
                        AddTypedef(userType, baseType, "DetectedFromSourceCode");
                        _logger.LogInformation($"Found typedef: {userType} -> {baseType}");
                    }
                }

                // 2. Tìm định nghĩa kiểu tùy chỉnh từ mẫu sử dụng
                var customTypePatterns = new Dictionary<string, string>
                {
                    { @"(U?INT\d+|BYTE|WORD|DWORD|QWORD)\s+\w+", @"(unsigned|signed)?\s*(char|short|int|long|long long)" },
                    { @"BOOL\s+\w+", @"(int|char)" }
                };

                foreach (var pattern in customTypePatterns)
                {
                    var regex = new Regex(pattern.Key);
                    var matches = regex.Matches(sourceCode);

                    foreach (Match match in matches)
                    {
                        // Lấy kiểu tùy chỉnh
                        string declaration = match.Value;
                        string userType = Regex.Match(declaration, @"^\s*(\w+)").Groups[1].Value;

                        // Đảm bảo đây là một kiểu tùy chỉnh
                        if (Regex.IsMatch(userType, @"^[A-Z][A-Z0-9_]*$") && !_typedefMap.ContainsKey(userType))
                        {
                            // Đoán kiểu cơ bản dựa trên mẫu
                            string baseType = GuessBaseType(userType);
                            if (!string.IsNullOrEmpty(baseType))
                            {
                                AddTypedef(userType, baseType, "InferredFromUsage");
                                _logger.LogInformation($"Inferred type: {userType} -> {baseType}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registering type aliases from source code: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public Dictionary<string, TypedefMapping> GetAllTypeMappings()
        {
            return new Dictionary<string, TypedefMapping>(_typedefMap);
        }

        /// <inheritdoc/>
        public void DeriveConstraintsFromBaseType(TypedefMapping mapping)
        {
            if (mapping == null || string.IsNullOrEmpty(mapping.BaseType))
                return;

            string baseType = mapping.BaseType.ToLowerInvariant();

            // Set default size based on base type
            if (baseType.Contains("char"))
            {
                mapping.Size = 1;
                if (baseType.Contains("unsigned"))
                {
                    mapping.MinValue = "0";
                    mapping.MaxValue = "255";
                }
                else
                {
                    mapping.MinValue = "-128";
                    mapping.MaxValue = "127";
                }
            }
            else if (baseType.Contains("short"))
            {
                mapping.Size = 2;
                if (baseType.Contains("unsigned"))
                {
                    mapping.MinValue = "0";
                    mapping.MaxValue = "65535";
                }
                else
                {
                    mapping.MinValue = "-32768";
                    mapping.MaxValue = "32767";
                }
            }
            else if (baseType.Contains("int") && !baseType.Contains("long"))
            {
                mapping.Size = 4;
                if (baseType.Contains("unsigned"))
                {
                    mapping.MinValue = "0";
                    mapping.MaxValue = "4294967295";
                }
                else
                {
                    mapping.MinValue = "-2147483648";
                    mapping.MaxValue = "2147483647";
                }
            }
            else if (baseType.Contains("long long") || baseType.Contains("int64"))
            {
                mapping.Size = 8;
                if (baseType.Contains("unsigned"))
                {
                    mapping.MinValue = "0";
                    mapping.MaxValue = "18446744073709551615";
                }
                else
                {
                    mapping.MinValue = "-9223372036854775808";
                    mapping.MaxValue = "9223372036854775807";
                }
            }
            else if (baseType.Contains("long") && !baseType.Contains("long long"))
            {
                mapping.Size = 4; // May be 8 on 64-bit platforms
                if (baseType.Contains("unsigned"))
                {
                    mapping.MinValue = "0";
                    mapping.MaxValue = "4294967295";
                }
                else
                {
                    mapping.MinValue = "-2147483648";
                    mapping.MaxValue = "2147483647";
                }
            }
            else if (baseType.Contains("float"))
            {
                mapping.Size = 4;
                mapping.MinValue = "-3.4e38";
                mapping.MaxValue = "3.4e38";
            }
            else if (baseType.Contains("double"))
            {
                mapping.Size = 8;
                mapping.MinValue = "-1.7e308";
                mapping.MaxValue = "1.7e308";
            }
            else if (baseType.Contains("bool"))
            {
                mapping.Size = 1;
                mapping.MinValue = "0";
                mapping.MaxValue = "1";
            }
        }

        /// <inheritdoc/>
        public string CleanTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeName;

            // Remove qualifiers like const, volatile, static
            string cleanedType = Regex.Replace(typeName, @"\b(const|volatile|static|extern|register|auto)\b", "").Trim();

            // Remove multiple spaces
            cleanedType = Regex.Replace(cleanedType, @"\s+", " ").Trim();

            return cleanedType;
        }

        /// <summary>
        /// Loads the typedef configuration from file
        /// </summary>
        private async Task LoadTypedefConfigAsync()
        {
            if (!_fileService.FileExists(_configPath))
            {
                _logger.LogWarning($"Typedef configuration file not found: {_configPath}");
                _typedefConfig = CreateDefaultConfig();
                await SaveTypedefConfigAsync();
                return;
            }

            string json = await _fileService.ReadFileAsync(_configPath);
            _typedefConfig = JsonSerializer.Deserialize<TypedefConfig>(json);

            if (_typedefConfig == null)
            {
                _typedefConfig = CreateDefaultConfig();
            }

            // Đảm bảo thuộc tính LearnedTypedefs được khởi tạo
            if (_typedefConfig.LearnedTypedefs == null)
            {
                _typedefConfig.LearnedTypedefs = new List<TypedefMapping>();
            }
        }

        /// <summary>
        /// Creates a default typedef configuration
        /// </summary>
        private TypedefConfig CreateDefaultConfig()
        {
            return new TypedefConfig
            {
                TypedefMappings = new List<TypedefMapping>
                {
                    new TypedefMapping { UserType = "UINT8", BaseType = "unsigned char", MinValue = "0", MaxValue = "255", Size = 1 },
                    new TypedefMapping { UserType = "UINT16", BaseType = "unsigned short", MinValue = "0", MaxValue = "65535", Size = 2 },
                    new TypedefMapping { UserType = "UINT32", BaseType = "unsigned int", MinValue = "0", MaxValue = "4294967295", Size = 4 },
                    new TypedefMapping { UserType = "UINT64", BaseType = "unsigned long long", MinValue = "0", MaxValue = "18446744073709551615", Size = 8 },
                    new TypedefMapping { UserType = "INT8", BaseType = "signed char", MinValue = "-128", MaxValue = "127", Size = 1 },
                    new TypedefMapping { UserType = "INT16", BaseType = "short", MinValue = "-32768", MaxValue = "32767", Size = 2 },
                    new TypedefMapping { UserType = "INT32", BaseType = "int", MinValue = "-2147483648", MaxValue = "2147483647", Size = 4 },
                    new TypedefMapping { UserType = "INT64", BaseType = "long long", MinValue = "-9223372036854775808", MaxValue = "9223372036854775807", Size = 8 },
                    new TypedefMapping { UserType = "BYTE", BaseType = "unsigned char", MinValue = "0", MaxValue = "255", Size = 1 },
                    new TypedefMapping { UserType = "WORD", BaseType = "unsigned short", MinValue = "0", MaxValue = "65535", Size = 2 },
                    new TypedefMapping { UserType = "DWORD", BaseType = "unsigned int", MinValue = "0", MaxValue = "4294967295", Size = 4 },
                    new TypedefMapping { UserType = "BOOL", BaseType = "int", MinValue = "0", MaxValue = "1", Size = 4 }
                },
                DetectedTypedefs = new List<TypedefMapping>(),
                LearnedTypedefs = new List<TypedefMapping>()
            };
        }

        /// <summary>
        /// Builds the typedef map from the configuration
        /// </summary>
        private void BuildTypedefMap()
        {
            _typedefMap.Clear();

            // Add predefined typedefs
            foreach (var mapping in _typedefConfig.TypedefMappings)
            {
                _typedefMap[mapping.UserType] = mapping;
            }

            // Add detected typedefs
            foreach (var mapping in _typedefConfig.DetectedTypedefs)
            {
                // Skip if already exists in predefined mappings
                if (!_typedefMap.ContainsKey(mapping.UserType))
                {
                    _typedefMap[mapping.UserType] = mapping;
                }
            }

            // Add learned typedefs
            if (_typedefConfig.LearnedTypedefs != null)
            {
                foreach (var mapping in _typedefConfig.LearnedTypedefs)
                {
                    // Skip if already exists in predefined or detected mappings
                    if (!_typedefMap.ContainsKey(mapping.UserType))
                    {
                        _typedefMap[mapping.UserType] = mapping;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a constraint from a typedef mapping
        /// </summary>
        private VariableConstraint CreateConstraintFromMapping(TypedefMapping mapping, string variableName)
        {
            if (mapping == null)
                return null;

            // Create a range constraint if min and max values are specified
            if (!string.IsNullOrEmpty(mapping.MinValue) && !string.IsNullOrEmpty(mapping.MaxValue))
            {
                return new VariableConstraint
                {
                    VariableName = variableName,
                    Type = ConstraintType.Range,
                    MinValue = mapping.MinValue,
                    MaxValue = mapping.MaxValue,
                    Source = $"Type constraint: {mapping.UserType} ({mapping.BaseType})"
                };
            }

            // Fall back to base type constraint
            return GetConstraintForBasicType(mapping.BaseType, variableName);
        }

        /// <summary>
        /// Gets constraint for a basic C type
        /// </summary>
        private VariableConstraint GetConstraintForBasicType(string typeName, string variableName)
        {
            string lowerType = typeName.ToLowerInvariant();

            if (lowerType.Contains("bool"))
            {
                return new VariableConstraint
                {
                    VariableName = variableName,
                    Type = ConstraintType.Enumeration,
                    AllowedValues = new List<string> { "0", "1", "false", "true" },
                    Source = $"Type constraint: boolean"
                };
            }
            else if (lowerType.Contains("char") && !lowerType.Contains("*"))
            {
                if (lowerType.Contains("unsigned") || lowerType.Contains("uchar"))
                {
                    return new VariableConstraint
                    {
                        VariableName = variableName,
                        Type = ConstraintType.Range,
                        MinValue = "0",
                        MaxValue = "255",
                        Source = $"Type constraint: unsigned char"
                    };
                }
                else
                {
                    return new VariableConstraint
                    {
                        VariableName = variableName,
                        Type = ConstraintType.Range,
                        MinValue = "-128",
                        MaxValue = "127",
                        Source = $"Type constraint: signed char"
                    };
                }
            }
            else if (lowerType.Contains("short"))
            {
                if (lowerType.Contains("unsigned"))
                {
                    return new VariableConstraint
                    {
                        VariableName = variableName,
                        Type = ConstraintType.Range,
                        MinValue = "0",
                        MaxValue = "65535",
                        Source = $"Type constraint: unsigned short"
                    };
                }
                else
                {
                    return new VariableConstraint
                    {
                        VariableName = variableName,
                        Type = ConstraintType.Range,
                        MinValue = "-32768",
                        MaxValue = "32767",
                        Source = $"Type constraint: short"
                    };
                }
            }
            else if (lowerType.Contains("int") && !lowerType.Contains("long"))
            {
                if (lowerType.Contains("unsigned"))
                {
                    return new VariableConstraint
                    {
                        VariableName = variableName,
                        Type = ConstraintType.Range,
                        MinValue = "0",
                        MaxValue = "4294967295",
                        Source = $"Type constraint: unsigned int"
                    };
                }
                else
                {
                    return new VariableConstraint
                    {
                        VariableName = variableName,
                        Type = ConstraintType.Range,
                        MinValue = "-2147483648",
                        MaxValue = "2147483647",
                        Source = $"Type constraint: int"
                    };
                }
            }
            // Add more types as needed...

            return null;
        }

        /// <summary>
        /// Kiểm tra tương thích giữa kiểu chuẩn hóa và kiểu cơ bản
        /// </summary>
        private bool IsCompatibleType(string normalizedType, string baseType)
        {
            // Loại bỏ const, volatile từ normalized type
            string cleanNormalizedType = CleanTypeName(normalizedType);

            // Kiểm tra nếu baseType là một phần của cleanNormalizedType
            return cleanNormalizedType.Contains(baseType) ||
                   baseType.Contains(cleanNormalizedType) ||
                   // Kiểm tra đặc biệt cho unsigned long/int
                   (baseType.Contains("unsigned int") && cleanNormalizedType.Contains("unsigned long")) ||
                   (baseType.Contains("unsigned long") && cleanNormalizedType.Contains("unsigned int"));
        }

        /// <summary>
        /// Đoán kiểu cơ bản dựa trên tên kiểu tùy chỉnh
        /// </summary>
        private string GuessBaseType(string userType)
        {
            // Kiểu số nguyên
            if (userType.StartsWith("UINT"))
            {
                if (userType.EndsWith("8")) return "unsigned char";
                if (userType.EndsWith("16")) return "unsigned short";
                if (userType.EndsWith("32")) return "unsigned int";
                if (userType.EndsWith("64")) return "unsigned long long";
            }
            else if (userType.StartsWith("INT"))
            {
                if (userType.EndsWith("8")) return "signed char";
                if (userType.EndsWith("16")) return "short";
                if (userType.EndsWith("32")) return "int";
                if (userType.EndsWith("64")) return "long long";
            }

            // Kiểu Window/DOS cũ
            switch (userType)
            {
                case "BYTE": return "unsigned char";
                case "WORD": return "unsigned short";
                case "DWORD": return "unsigned int";
                case "QWORD": return "unsigned long long";
                case "BOOL": return "int";
            }

            return null;
        }
    }
}