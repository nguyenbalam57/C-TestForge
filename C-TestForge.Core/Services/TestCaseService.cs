// File: C-TestForge.Core/Services/TestCaseService.cs
using C_TestForge.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Text;

namespace C_TestForge.Core.Services
{
    public class TestCaseService : ITestCaseService
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<TestCaseService> _logger;

        public TestCaseService(IProjectService projectService, ILogger<TestCaseService> logger)
        {
            _projectService = projectService;
            _logger = logger;
        }

        public TestCase CreateTestCase(string name, string description, TestCaseType type, string targetFunction)
        {
            _logger.LogInformation($"Creating test case: {name} for function: {targetFunction}");

            var testCase = new TestCase
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                Type = type,
                TargetFunction = targetFunction,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            return testCase;
        }

        public void AddTestCaseToProject(TestCase testCase)
        {
            if (_projectService.CurrentProject == null)
            {
                throw new InvalidOperationException("No project is currently loaded");
            }

            _logger.LogInformation($"Adding test case {testCase.Name} to project {_projectService.CurrentProject.Name}");

            _projectService.CurrentProject.TestCases.Add(testCase);
            _projectService.SaveProject(_projectService.CurrentProject);
        }

        public void UpdateTestCase(TestCase testCase)
        {
            if (_projectService.CurrentProject == null)
            {
                throw new InvalidOperationException("No project is currently loaded");
            }

            _logger.LogInformation($"Updating test case: {testCase.Name}");

            var index = _projectService.CurrentProject.TestCases.FindIndex(tc => tc.Id == testCase.Id);
            if (index >= 0)
            {
                testCase.ModifiedDate = DateTime.Now;
                _projectService.CurrentProject.TestCases[index] = testCase;
                _projectService.SaveProject(_projectService.CurrentProject);
            }
            else
            {
                throw new KeyNotFoundException($"Test case with ID {testCase.Id} not found in current project");
            }
        }

        public void DeleteTestCase(string testCaseId)
        {
            if (_projectService.CurrentProject == null)
            {
                throw new InvalidOperationException("No project is currently loaded");
            }

            _logger.LogInformation($"Deleting test case with ID: {testCaseId}");

            var index = _projectService.CurrentProject.TestCases.FindIndex(tc => tc.Id == testCaseId);
            if (index >= 0)
            {
                _projectService.CurrentProject.TestCases.RemoveAt(index);
                _projectService.SaveProject(_projectService.CurrentProject);
            }
            else
            {
                throw new KeyNotFoundException($"Test case with ID {testCaseId} not found in current project");
            }
        }

        public List<TestCase> GetAllTestCases()
        {
            if (_projectService.CurrentProject == null)
            {
                throw new InvalidOperationException("No project is currently loaded");
            }

            _logger.LogInformation($"Getting all test cases for project: {_projectService.CurrentProject.Name}");

            return _projectService.CurrentProject.TestCases;
        }

        public TestCase GetTestCase(string id)
        {
            if (_projectService.CurrentProject == null)
            {
                throw new InvalidOperationException("No project is currently loaded");
            }

            _logger.LogInformation($"Getting test case with ID: {id}");

            return _projectService.CurrentProject.TestCases.FirstOrDefault(tc => tc.Id == id);
        }

        public void ImportTestCasesFromFile(string filePath)
        {
            if (_projectService.CurrentProject == null)
            {
                throw new InvalidOperationException("No project is currently loaded");
            }

            _logger.LogInformation($"Importing test cases from: {filePath}");

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            List<TestCase> testCases = new List<TestCase>();

            switch (extension)
            {
                case ".tst":
                    testCases = ImportFromTstFile(filePath);
                    break;
                case ".csv":
                    testCases = ImportFromCsvFile(filePath);
                    break;
                case ".xlsx":
                    testCases = ImportFromExcelFile(filePath);
                    break;
                case ".json":
                    testCases = ImportFromJsonFile(filePath);
                    break;
                default:
                    throw new NotSupportedException($"File format not supported: {extension}");
            }

            foreach (var testCase in testCases)
            {
                // Ensure the test case has a unique ID
                testCase.Id = Guid.NewGuid().ToString();
                testCase.CreatedDate = DateTime.Now;
                testCase.ModifiedDate = DateTime.Now;

                // Add to project
                _projectService.CurrentProject.TestCases.Add(testCase);
            }

            _projectService.SaveProject(_projectService.CurrentProject);
        }

        public void ExportTestCasesToFile(List<TestCase> testCases, string filePath)
        {
            _logger.LogInformation($"Exporting {testCases.Count} test cases to: {filePath}");

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            switch (extension)
            {
                case ".tst":
                    ExportToTstFile(testCases, filePath);
                    break;
                case ".csv":
                    ExportToCsvFile(testCases, filePath);
                    break;
                case ".xlsx":
                    ExportToExcelFile(testCases, filePath);
                    break;
                case ".json":
                    ExportToJsonFile(testCases, filePath);
                    break;
                default:
                    throw new NotSupportedException($"File format not supported: {extension}");
            }
        }

        private List<TestCase> ImportFromTstFile(string filePath)
        {
            // Simplified TST format parser
            var lines = File.ReadAllLines(filePath);
            var testCases = new List<TestCase>();
            TestCase currentTestCase = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//"))
                {
                    continue;
                }

                if (trimmedLine.StartsWith("TEST_CASE"))
                {
                    // Parse TEST_CASE(name, function)
                    var content = GetContentBetweenParentheses(trimmedLine);
                    var parts = content.Split(',').Select(p => p.Trim()).ToArray();

                    if (parts.Length >= 2)
                    {
                        currentTestCase = new TestCase
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = parts[0].Trim('"'),
                            TargetFunction = parts[1],
                            Type = TestCaseType.UnitTest,
                            InputVariables = new List<TestCaseVariable>(),
                            OutputVariables = new List<TestCaseVariable>(),
                            Stubs = new List<TestCaseStub>()
                        };

                        testCases.Add(currentTestCase);
                    }
                }
                else if (trimmedLine.StartsWith("INPUT") && currentTestCase != null)
                {
                    // Parse INPUT(name, type, value)
                    var content = GetContentBetweenParentheses(trimmedLine);
                    var parts = content.Split(',').Select(p => p.Trim()).ToArray();

                    if (parts.Length >= 3)
                    {
                        var variable = new TestCaseVariable
                        {
                            Name = parts[0],
                            Type = parts[1],
                            Value = parts[2]
                        };

                        currentTestCase.InputVariables.Add(variable);
                    }
                }
                else if (trimmedLine.StartsWith("EXPECTED") && currentTestCase != null)
                {
                    // Parse EXPECTED(name, type, value)
                    var content = GetContentBetweenParentheses(trimmedLine);
                    var parts = content.Split(',').Select(p => p.Trim()).ToArray();

                    if (parts.Length >= 3)
                    {
                        var variable = new TestCaseVariable
                        {
                            Name = parts[0],
                            Type = parts[1],
                            Value = parts[2]
                        };

                        currentTestCase.OutputVariables.Add(variable);
                    }
                }
                else if (trimmedLine.StartsWith("STUB") && currentTestCase != null)
                {
                    // Parse STUB(function, return_value)
                    var content = GetContentBetweenParentheses(trimmedLine);
                    var parts = content.Split(',').Select(p => p.Trim()).ToArray();

                    if (parts.Length >= 2)
                    {
                        var stub = new TestCaseStub
                        {
                            FunctionName = parts[0],
                            ReturnValue = parts[1],
                            Parameters = new List<TestCaseVariable>()
                        };

                        currentTestCase.Stubs.Add(stub);
                    }
                }
            }

            return testCases;
        }

        private string GetContentBetweenParentheses(string line)
        {
            var start = line.IndexOf('(');
            var end = line.LastIndexOf(')');

            if (start >= 0 && end > start)
            {
                return line.Substring(start + 1, end - start - 1);
            }

            return string.Empty;
        }

        private List<TestCase> ImportFromCsvFile(string filePath)
        {
            var testCases = new List<TestCase>();

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)))
            {
                // Read header
                csv.Read();
                csv.ReadHeader();

                // Expected CSV format: TestCaseName,TargetFunction,Type,InputName,InputType,InputValue,ExpectedName,ExpectedType,ExpectedValue
                while (csv.Read())
                {
                    var testCaseName = csv.GetField("TestCaseName");
                    var targetFunction = csv.GetField("TargetFunction");
                    var typeStr = csv.GetField("Type");
                    var inputName = csv.GetField("InputName");
                    var inputType = csv.GetField("InputType");
                    var inputValue = csv.GetField("InputValue");
                    var expectedName = csv.GetField("ExpectedName");
                    var expectedType = csv.GetField("ExpectedType");
                    var expectedValue = csv.GetField("ExpectedValue");

                    TestCaseType type = Enum.TryParse<TestCaseType>(typeStr, out var parsedType)
                        ? parsedType
                        : TestCaseType.UnitTest;

                    // Find existing test case or create new one
                    var testCase = testCases.FirstOrDefault(tc => tc.Name == testCaseName);
                    if (testCase == null)
                    {
                        testCase = new TestCase
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = testCaseName,
                            TargetFunction = targetFunction,
                            Type = type,
                            InputVariables = new List<TestCaseVariable>(),
                            OutputVariables = new List<TestCaseVariable>(),
                            Stubs = new List<TestCaseStub>()
                        };

                        testCases.Add(testCase);
                    }

                    // Add input variable
                    if (!string.IsNullOrEmpty(inputName) && !string.IsNullOrEmpty(inputType))
                    {
                        testCase.InputVariables.Add(new TestCaseVariable
                        {
                            Name = inputName,
                            Type = inputType,
                            Value = inputValue
                        });
                    }

                    // Add expected output variable
                    if (!string.IsNullOrEmpty(expectedName) && !string.IsNullOrEmpty(expectedType))
                    {
                        testCase.OutputVariables.Add(new TestCaseVariable
                        {
                            Name = expectedName,
                            Type = expectedType,
                            Value = expectedValue
                        });
                    }
                }
            }

            return testCases;
        }

        private List<TestCase> ImportFromExcelFile(string filePath)
        {
            var testCases = new List<TestCase>();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;

                // Assume first row is header
                // Expected format: TestCaseName,TargetFunction,Type,InputName,InputType,InputValue,ExpectedName,ExpectedType,ExpectedValue
                for (int row = 2; row <= rowCount; row++)
                {
                    var testCaseName = worksheet.Cells[row, 1].Value?.ToString();
                    var targetFunction = worksheet.Cells[row, 2].Value?.ToString();
                    var typeStr = worksheet.Cells[row, 3].Value?.ToString();
                    var inputName = worksheet.Cells[row, 4].Value?.ToString();
                    var inputType = worksheet.Cells[row, 5].Value?.ToString();
                    var inputValue = worksheet.Cells[row, 6].Value?.ToString();
                    var expectedName = worksheet.Cells[row, 7].Value?.ToString();
                    var expectedType = worksheet.Cells[row, 8].Value?.ToString();
                    var expectedValue = worksheet.Cells[row, 9].Value?.ToString();

                    if (string.IsNullOrEmpty(testCaseName) || string.IsNullOrEmpty(targetFunction))
                    {
                        continue;
                    }

                    TestCaseType type = Enum.TryParse<TestCaseType>(typeStr, out var parsedType)
                        ? parsedType
                        : TestCaseType.UnitTest;

                    // Find existing test case or create new one
                    var testCase = testCases.FirstOrDefault(tc => tc.Name == testCaseName);
                    if (testCase == null)
                    {
                        testCase = new TestCase
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = testCaseName,
                            TargetFunction = targetFunction,
                            Type = type,
                            InputVariables = new List<TestCaseVariable>(),
                            OutputVariables = new List<TestCaseVariable>(),
                            Stubs = new List<TestCaseStub>()
                        };

                        testCases.Add(testCase);
                    }

                    // Add input variable
                    if (!string.IsNullOrEmpty(inputName) && !string.IsNullOrEmpty(inputType))
                    {
                        testCase.InputVariables.Add(new TestCaseVariable
                        {
                            Name = inputName,
                            Type = inputType,
                            Value = inputValue
                        });
                    }

                    // Add expected output variable
                    if (!string.IsNullOrEmpty(expectedName) && !string.IsNullOrEmpty(expectedType))
                    {
                        testCase.OutputVariables.Add(new TestCaseVariable
                        {
                            Name = expectedName,
                            Type = expectedType,
                            Value = expectedValue
                        });
                    }
                }
            }

            return testCases;
        }

        private List<TestCase> ImportFromJsonFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<TestCase>>(json) ?? new List<TestCase>();
        }

        private void ExportToTstFile(List<TestCase> testCases, string filePath)
        {
            var sb = new StringBuilder();

            foreach (var testCase in testCases)
            {
                sb.AppendLine($"// Test Case: {testCase.Name}");
                sb.AppendLine($"TEST_CASE(\"{testCase.Name}\", {testCase.TargetFunction})");

                foreach (var input in testCase.InputVariables)
                {
                    sb.AppendLine($"    INPUT({input.Name}, {input.Type}, {input.Value})");
                }

                foreach (var output in testCase.OutputVariables)
                {
                    sb.AppendLine($"    EXPECTED({output.Name}, {output.Type}, {output.Value})");
                }

                foreach (var stub in testCase.Stubs)
                {
                    sb.AppendLine($"    STUB({stub.FunctionName}, {stub.ReturnValue})");

                    foreach (var param in stub.Parameters)
                    {
                        sb.AppendLine($"        PARAM({param.Name}, {param.Type}, {param.Value})");
                    }
                }

                sb.AppendLine("END_TEST_CASE");
                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString());
        }

        private void ExportToCsvFile(List<TestCase> testCases, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)))
            {
                // Write header
                csv.WriteField("TestCaseName");
                csv.WriteField("TargetFunction");
                csv.WriteField("Type");
                csv.WriteField("InputName");
                csv.WriteField("InputType");
                csv.WriteField("InputValue");
                csv.WriteField("ExpectedName");
                csv.WriteField("ExpectedType");
                csv.WriteField("ExpectedValue");
                csv.NextRecord();

                foreach (var testCase in testCases)
                {
                    // Handle the case where there are multiple inputs/outputs
                    int maxVariables = Math.Max(
                        testCase.InputVariables.Count,
                        testCase.OutputVariables.Count);

                    for (int i = 0; i < maxVariables; i++)
                    {
                        csv.WriteField(testCase.Name);
                        csv.WriteField(testCase.TargetFunction);
                        csv.WriteField(testCase.Type.ToString());

                        // Input variables
                        if (i < testCase.InputVariables.Count)
                        {
                            var input = testCase.InputVariables[i];
                            csv.WriteField(input.Name);
                            csv.WriteField(input.Type);
                            csv.WriteField(input.Value);
                        }
                        else
                        {
                            csv.WriteField(string.Empty);
                            csv.WriteField(string.Empty);
                            csv.WriteField(string.Empty);
                        }

                        // Output variables
                        if (i < testCase.OutputVariables.Count)
                        {
                            var output = testCase.OutputVariables[i];
                            csv.WriteField(output.Name);
                            csv.WriteField(output.Type);
                            csv.WriteField(output.Value);
                        }
                        else
                        {
                            csv.WriteField(string.Empty);
                            csv.WriteField(string.Empty);
                            csv.WriteField(string.Empty);
                        }

                        csv.NextRecord();
                    }
                }
            }
        }

        private void ExportToExcelFile(List<TestCase> testCases, string filePath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("TestCases");

                // Write header
                worksheet.Cells[1, 1].Value = "TestCaseName";
                worksheet.Cells[1, 2].Value = "TargetFunction";
                worksheet.Cells[1, 3].Value = "Type";
                worksheet.Cells[1, 4].Value = "InputName";
                worksheet.Cells[1, 5].Value = "InputType";
                worksheet.Cells[1, 6].Value = "InputValue";
                worksheet.Cells[1, 7].Value = "ExpectedName";
                worksheet.Cells[1, 8].Value = "ExpectedType";
                worksheet.Cells[1, 9].Value = "ExpectedValue";

                int row = 2;
                foreach (var testCase in testCases)
                {
                    // Handle the case where there are multiple inputs/outputs
                    int maxVariables = Math.Max(
                        testCase.InputVariables.Count,
                        testCase.OutputVariables.Count);

                    for (int i = 0; i < maxVariables; i++)
                    {
                        worksheet.Cells[row, 1].Value = testCase.Name;
                        worksheet.Cells[row, 2].Value = testCase.TargetFunction;
                        worksheet.Cells[row, 3].Value = testCase.Type.ToString();

                        // Input variables
                        if (i < testCase.InputVariables.Count)
                        {
                            var input = testCase.InputVariables[i];
                            worksheet.Cells[row, 4].Value = input.Name;
                            worksheet.Cells[row, 5].Value = input.Type;
                            worksheet.Cells[row, 6].Value = input.Value;
                        }

                        // Output variables
                        if (i < testCase.OutputVariables.Count)
                        {
                            var output = testCase.OutputVariables[i];
                            worksheet.Cells[row, 7].Value = output.Name;
                            worksheet.Cells[row, 8].Value = output.Type;
                            worksheet.Cells[row, 9].Value = output.Value;
                        }

                        row++;
                    }
                }

                // Format as table
                var range = worksheet.Cells[1, 1, row - 1, 9];
                var table = worksheet.Tables.Add(range, "TestCasesTable");
                table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium2;

                package.SaveAs(new FileInfo(filePath));
            }
        }

        private void ExportToJsonFile(List<TestCase> testCases, string filePath)
        {
            var json = JsonConvert.SerializeObject(testCases, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }

    public interface ITestCaseService
    {
        TestCase CreateTestCase(string name, string description, TestCaseType type, string targetFunction);
        void AddTestCaseToProject(TestCase testCase);
        void UpdateTestCase(TestCase testCase);
        void DeleteTestCase(string testCaseId);
        List<TestCase> GetAllTestCases();
        TestCase GetTestCase(string id);
        void ImportTestCasesFromFile(string filePath);
        void ExportTestCasesToFile(List<TestCase> testCases, string filePath);
    }
}