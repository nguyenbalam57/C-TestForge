using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;
using C_TestForge.Models.TestCases;
using C_TestForge.TestCase.Repositories;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;

namespace C_TestForge.TestCase.Services
{
    public class TestCaseService : ITestCaseService
    {
        private readonly ITestCaseRepository _repository;

        public TestCaseService(ITestCaseRepository repository)
        {
            _repository = repository;
        }

        #region CRUD Operations

        public async Task<List<Models.TestCases.TestCase>> GetAllTestCasesAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Models.TestCases.TestCase> GetTestCaseByIdAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<List<Models.TestCases.TestCase>> GetTestCasesByFunctionNameAsync(string functionName)
        {
            return await _repository.GetByFunctionNameAsync(functionName);
        }

        public async Task<Models.TestCases.TestCase> CreateTestCaseAsync(Models.TestCases.TestCase testCase)
        {
            testCase.CreatedDate = DateTime.Now;
            testCase.ModifiedDate = DateTime.Now;
            return await _repository.CreateAsync(testCase);
        }

        public async Task<Models.TestCases.TestCase> UpdateTestCaseAsync(Models.TestCases.TestCase testCase)
        {
            testCase.ModifiedDate = DateTime.Now;
            return await _repository.UpdateAsync(testCase);
        }

        public async Task<bool> DeleteTestCaseAsync(Guid id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> DeleteAllTestCasesAsync()
        {
            return await _repository.DeleteAllAsync();
        }

        #endregion

        #region Import/Export

        public async Task<List<Models.TestCases.TestCase>> ImportFromTstFileAsync(string filePath)
        {
            var content = await File.ReadAllTextAsync(filePath);
            var testCases = ParseTstContent(content);

            foreach (var testCase in testCases)
            {
                await _repository.CreateAsync(testCase);
            }

            return testCases;
        }

        public async Task<List<Models.TestCases.TestCase>> ImportFromCsvFileAsync(string filePath)
        {
            var testCases = new List<Models.TestCases.TestCase>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                MissingFieldFound = null
            };

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, config))
            {
                // Read main test case records
                var records = csv.GetRecords<TestCaseCsvRecord>().ToList();

                // Convert records to test cases
                foreach (var record in records)
                {
                    // Find existing test case or create new one
                    var testCase = testCases.FirstOrDefault(tc => tc.Name == record.Name);
                    if (testCase == null)
                    {
                        testCase = new Models.TestCases.TestCase
                        {
                            Id = Guid.NewGuid(),
                            Name = record.Name,
                            Description = record.Description,
                            FunctionName = record.FunctionName,
                            Type = Enum.TryParse<TestCaseType>(record.Type, out var parsedType)
                                ? parsedType
                                : TestCaseType.UnitTest,
                            Status = Enum.TryParse<TestCaseStatus>(record.Status, out var parsedStatus)
                                ? parsedStatus
                                : TestCaseStatus.NotExecuted,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = DateTime.Now,
                            Inputs = new List<TestCaseInput>(),
                            ExpectedOutputs = new List<TestCaseOutput>(),
                            ActualOutputs = new List<TestCaseOutput>()
                        };

                        testCases.Add(testCase);
                    }
                }

                // Reset reader position
                reader.BaseStream.Position = 0;
                csv.Read();
                csv.ReadHeader();

                // Read input records
                while (csv.Read())
                {
                    var testCaseName = csv.GetField("TestCaseName");
                    var variableName = csv.GetField("VariableName");
                    var variableType = csv.GetField("VariableType");
                    var value = csv.GetField("Value");
                    var isInputStr = csv.GetField("IsInput");
                    var isStubStr = csv.GetField("IsStub");

                    if (string.IsNullOrEmpty(testCaseName) || string.IsNullOrEmpty(variableName))
                        continue;

                    var testCase = testCases.FirstOrDefault(tc => tc.Name == testCaseName);
                    if (testCase == null)
                        continue;

                    bool isInput = true;
                    if (!string.IsNullOrEmpty(isInputStr))
                    {
                        bool.TryParse(isInputStr, out isInput);
                    }

                    bool isStub = false;
                    if (!string.IsNullOrEmpty(isStubStr))
                    {
                        bool.TryParse(isStubStr, out isStub);
                    }

                    if (isInput)
                    {
                        testCase.Inputs.Add(new TestCaseInput
                        {
                            Id = Guid.NewGuid(),
                            VariableName = variableName,
                            VariableType = variableType ?? "int",
                            Value = value ?? "0",
                            IsStub = isStub
                        });
                    }
                    else
                    {
                        testCase.ExpectedOutputs.Add(new TestCaseOutput
                        {
                            Id = Guid.NewGuid(),
                            VariableName = variableName,
                            VariableType = variableType ?? "int",
                            Value = value ?? "0"
                        });
                    }
                }
            }

            // Save to repository
            foreach (var testCase in testCases)
            {
                await _repository.CreateAsync(testCase);
            }

            return testCases;
        }

        public async Task<List<Models.TestCases.TestCase>> ImportFromExcelFileAsync(string filePath)
        {
            var testCases = new List<Models.TestCases.TestCase>();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                // Process TestCases sheet
                var testCasesWorksheet = package.Workbook.Worksheets["TestCases"];
                if (testCasesWorksheet != null)
                {
                    int rows = testCasesWorksheet.Dimension.Rows;

                    // Skip header row
                    for (int row = 2; row <= rows; row++)
                    {
                        var testCase = new Models.TestCases.TestCase
                        {
                            Id = Guid.NewGuid(),
                            Name = testCasesWorksheet.Cells[row, 1].Value?.ToString(),
                            Description = testCasesWorksheet.Cells[row, 2].Value?.ToString(),
                            FunctionName = testCasesWorksheet.Cells[row, 3].Value?.ToString(),
                            Type = Enum.TryParse<TestCaseType>(testCasesWorksheet.Cells[row, 4].Value?.ToString(), out var type)
                                ? type
                                : TestCaseType.UnitTest,
                            Status = Enum.TryParse<TestCaseStatus>(testCasesWorksheet.Cells[row, 5].Value?.ToString(), out var status)
                                ? status
                                : TestCaseStatus.NotExecuted,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = DateTime.Now,
                            Inputs = new List<TestCaseInput>(),
                            ExpectedOutputs = new List<TestCaseOutput>(),
                            ActualOutputs = new List<TestCaseOutput>()
                        };

                        if (!string.IsNullOrEmpty(testCase.Name) && !string.IsNullOrEmpty(testCase.FunctionName))
                        {
                            testCases.Add(testCase);
                        }
                    }
                }

                // Process Inputs sheet
                var inputsWorksheet = package.Workbook.Worksheets["Inputs"];
                if (inputsWorksheet != null)
                {
                    int rows = inputsWorksheet.Dimension.Rows;

                    // Skip header row
                    for (int row = 2; row <= rows; row++)
                    {
                        var testCaseName = inputsWorksheet.Cells[row, 1].Value?.ToString();
                        var testCase = testCases.FirstOrDefault(tc => tc.Name == testCaseName);

                        if (testCase != null)
                        {
                            var input = new TestCaseInput
                            {
                                Id = Guid.NewGuid(),
                                VariableName = inputsWorksheet.Cells[row, 2].Value?.ToString(),
                                VariableType = inputsWorksheet.Cells[row, 3].Value?.ToString(),
                                Value = inputsWorksheet.Cells[row, 4].Value?.ToString(),
                                IsStub = Convert.ToBoolean(inputsWorksheet.Cells[row, 5].Value ?? false)
                            };

                            if (!string.IsNullOrEmpty(input.VariableName))
                            {
                                testCase.Inputs.Add(input);
                            }
                        }
                    }
                }

                // Process ExpectedOutputs sheet
                var outputsWorksheet = package.Workbook.Worksheets["ExpectedOutputs"];
                if (outputsWorksheet != null)
                {
                    int rows = outputsWorksheet.Dimension.Rows;

                    // Skip header row
                    for (int row = 2; row <= rows; row++)
                    {
                        var testCaseName = outputsWorksheet.Cells[row, 1].Value?.ToString();
                        var testCase = testCases.FirstOrDefault(tc => tc.Name == testCaseName);

                        if (testCase != null)
                        {
                            var output = new TestCaseOutput
                            {
                                Id = Guid.NewGuid(),
                                VariableName = outputsWorksheet.Cells[row, 2].Value?.ToString(),
                                VariableType = outputsWorksheet.Cells[row, 3].Value?.ToString(),
                                Value = outputsWorksheet.Cells[row, 4].Value?.ToString()
                            };

                            if (!string.IsNullOrEmpty(output.VariableName))
                            {
                                testCase.ExpectedOutputs.Add(output);
                            }
                        }
                    }
                }
            }

            // Save to repository
            foreach (var testCase in testCases)
            {
                await _repository.CreateAsync(testCase);
            }

            return testCases;
        }

        public async Task ExportToTstFileAsync(List<Models.TestCases.TestCase> testCases, string filePath)
        {
            var content = GenerateTstContent(testCases);
            await File.WriteAllTextAsync(filePath, content);
        }

        public async Task ExportToCsvFileAsync(List<Models.TestCases.TestCase> testCases, string filePath)
        {
            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                // Write header for test cases
                csv.WriteHeader<TestCaseCsvRecord>();
                csv.NextRecord();

                // Write test case records
                foreach (var testCase in testCases)
                {
                    var record = new TestCaseCsvRecord
                    {
                        Name = testCase.Name,
                        Description = testCase.Description,
                        FunctionName = testCase.FunctionName,
                        Type = testCase.Type.ToString(),
                        Status = testCase.Status.ToString(),
                        CreatedDate = testCase.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        ModifiedDate = testCase.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    csv.WriteRecord(record);
                    csv.NextRecord();
                }
            }

            // Export inputs and outputs to separate CSV files
            var inputsFilePath = Path.Combine(
                Path.GetDirectoryName(filePath),
                Path.GetFileNameWithoutExtension(filePath) + "_inputs.csv");

            var outputsFilePath = Path.Combine(
                Path.GetDirectoryName(filePath),
                Path.GetFileNameWithoutExtension(filePath) + "_outputs.csv");

            // Export inputs
            using (var writer = new StreamWriter(inputsFilePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                // Write header
                csv.WriteHeader<TestCaseVariableCsvRecord>();
                csv.NextRecord();

                // Write input records
                foreach (var testCase in testCases)
                {
                    foreach (var input in testCase.Inputs)
                    {
                        var record = new TestCaseVariableCsvRecord
                        {
                            TestCaseName = testCase.Name,
                            VariableName = input.VariableName,
                            VariableType = input.VariableType,
                            Value = input.Value,
                            IsInput = true,
                            IsStub = input.IsStub
                        };

                        csv.WriteRecord(record);
                        csv.NextRecord();
                    }
                }
            }

            // Export outputs
            using (var writer = new StreamWriter(outputsFilePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                // Write header
                csv.WriteHeader<TestCaseVariableCsvRecord>();
                csv.NextRecord();

                // Write output records
                foreach (var testCase in testCases)
                {
                    foreach (var output in testCase.ExpectedOutputs)
                    {
                        var record = new TestCaseVariableCsvRecord
                        {
                            TestCaseName = testCase.Name,
                            VariableName = output.VariableName,
                            VariableType = output.VariableType,
                            Value = output.Value,
                            IsInput = false,
                            IsStub = false
                        };

                        csv.WriteRecord(record);
                        csv.NextRecord();
                    }
                }
            }

            await Task.CompletedTask;
        }

        public async Task ExportToExcelFileAsync(List<Models.TestCases.TestCase> testCases, string filePath)
        {
            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                // Create TestCases sheet
                var testCasesWorksheet = package.Workbook.Worksheets.Add("TestCases");

                // Add headers
                testCasesWorksheet.Cells[1, 1].Value = "Name";
                testCasesWorksheet.Cells[1, 2].Value = "Description";
                testCasesWorksheet.Cells[1, 3].Value = "FunctionName";
                testCasesWorksheet.Cells[1, 4].Value = "Type";
                testCasesWorksheet.Cells[1, 5].Value = "Status";
                testCasesWorksheet.Cells[1, 6].Value = "CreatedDate";
                testCasesWorksheet.Cells[1, 7].Value = "ModifiedDate";

                // Format headers
                for (int i = 1; i <= 7; i++)
                {
                    testCasesWorksheet.Cells[1, i].Style.Font.Bold = true;
                }

                // Add data
                for (int i = 0; i < testCases.Count; i++)
                {
                    var testCase = testCases[i];
                    int row = i + 2;

                    testCasesWorksheet.Cells[row, 1].Value = testCase.Name;
                    testCasesWorksheet.Cells[row, 2].Value = testCase.Description;
                    testCasesWorksheet.Cells[row, 3].Value = testCase.FunctionName;
                    testCasesWorksheet.Cells[row, 4].Value = testCase.Type.ToString();
                    testCasesWorksheet.Cells[row, 5].Value = testCase.Status.ToString();
                    testCasesWorksheet.Cells[row, 6].Value = testCase.CreatedDate;
                    testCasesWorksheet.Cells[row, 7].Value = testCase.ModifiedDate;
                }

                // Create Inputs sheet
                var inputsWorksheet = package.Workbook.Worksheets.Add("Inputs");

                // Add headers
                inputsWorksheet.Cells[1, 1].Value = "TestCaseName";
                inputsWorksheet.Cells[1, 2].Value = "VariableName";
                inputsWorksheet.Cells[1, 3].Value = "VariableType";
                inputsWorksheet.Cells[1, 4].Value = "Value";
                inputsWorksheet.Cells[1, 5].Value = "IsStub";

                // Format headers
                for (int i = 1; i <= 5; i++)
                {
                    inputsWorksheet.Cells[1, i].Style.Font.Bold = true;
                }

                // Add inputs data
                int inputRow = 2;
                foreach (var testCase in testCases)
                {
                    foreach (var input in testCase.Inputs)
                    {
                        inputsWorksheet.Cells[inputRow, 1].Value = testCase.Name;
                        inputsWorksheet.Cells[inputRow, 2].Value = input.VariableName;
                        inputsWorksheet.Cells[inputRow, 3].Value = input.VariableType;
                        inputsWorksheet.Cells[inputRow, 4].Value = input.Value;
                        inputsWorksheet.Cells[inputRow, 5].Value = input.IsStub;

                        inputRow++;
                    }
                }

                // Create ExpectedOutputs sheet
                var outputsWorksheet = package.Workbook.Worksheets.Add("ExpectedOutputs");

                // Add headers
                outputsWorksheet.Cells[1, 1].Value = "TestCaseName";
                outputsWorksheet.Cells[1, 2].Value = "VariableName";
                outputsWorksheet.Cells[1, 3].Value = "VariableType";
                outputsWorksheet.Cells[1, 4].Value = "Value";

                // Format headers
                for (int i = 1; i <= 4; i++)
                {
                    outputsWorksheet.Cells[1, i].Style.Font.Bold = true;
                }

                // Add outputs data
                int outputRow = 2;
                foreach (var testCase in testCases)
                {
                    foreach (var output in testCase.ExpectedOutputs)
                    {
                        outputsWorksheet.Cells[outputRow, 1].Value = testCase.Name;
                        outputsWorksheet.Cells[outputRow, 2].Value = output.VariableName;
                        outputsWorksheet.Cells[outputRow, 3].Value = output.VariableType;
                        outputsWorksheet.Cells[outputRow, 4].Value = output.Value;

                        outputRow++;
                    }
                }

                // Auto fit columns
                testCasesWorksheet.Cells.AutoFitColumns();
                inputsWorksheet.Cells.AutoFitColumns();
                outputsWorksheet.Cells.AutoFitColumns();

                // Save the Excel file
                await package.SaveAsAsync(new FileInfo(filePath));
            }
        }

        #endregion

        #region Analysis

        public async Task<TestCaseComparisonResult> CompareTestCasesAsync(Models.TestCases.TestCase testCase1, Models.TestCases.TestCase testCase2)
        {
            var result = new TestCaseComparisonResult
            {
                TestCase1 = testCase1,
                TestCase2 = testCase2,
                Differences = new List<TestCaseDifference>()
            };

            // Compare basic properties
            if (testCase1.Name != testCase2.Name)
            {
                result.Differences.Add(new TestCaseDifference
                {
                    PropertyName = "Name",
                    Value1 = testCase1.Name,
                    Value2 = testCase2.Name
                });
            }

            if (testCase1.Description != testCase2.Description)
            {
                result.Differences.Add(new TestCaseDifference
                {
                    PropertyName = "Description",
                    Value1 = testCase1.Description,
                    Value2 = testCase2.Description
                });
            }

            if (testCase1.FunctionName != testCase2.FunctionName)
            {
                result.Differences.Add(new TestCaseDifference
                {
                    PropertyName = "FunctionName",
                    Value1 = testCase1.FunctionName,
                    Value2 = testCase2.FunctionName
                });
            }

            if (testCase1.Type != testCase2.Type)
            {
                result.Differences.Add(new TestCaseDifference
                {
                    PropertyName = "Type",
                    Value1 = testCase1.Type.ToString(),
                    Value2 = testCase2.Type.ToString()
                });
            }

            if (testCase1.Status != testCase2.Status)
            {
                result.Differences.Add(new TestCaseDifference
                {
                    PropertyName = "Status",
                    Value1 = testCase1.Status.ToString(),
                    Value2 = testCase2.Status.ToString()
                });
            }

            // Compare inputs
            foreach (var input1 in testCase1.Inputs)
            {
                var matchingInput = testCase2.Inputs.FirstOrDefault(i => i.VariableName == input1.VariableName);
                if (matchingInput == null)
                {
                    result.Differences.Add(new TestCaseDifference
                    {
                        PropertyName = $"Input: {input1.VariableName}",
                        Value1 = "Present",
                        Value2 = "Missing"
                    });
                }
                else if (input1.Value != matchingInput.Value || input1.VariableType != matchingInput.VariableType || input1.IsStub != matchingInput.IsStub)
                {
                    result.Differences.Add(new TestCaseDifference
                    {
                        PropertyName = $"Input: {input1.VariableName}",
                        Value1 = $"Type: {input1.VariableType}, Value: {input1.Value}, IsStub: {input1.IsStub}",
                        Value2 = $"Type: {matchingInput.VariableType}, Value: {matchingInput.Value}, IsStub: {matchingInput.IsStub}"
                    });
                }
            }

            foreach (var input2 in testCase2.Inputs)
            {
                if (!testCase1.Inputs.Any(i => i.VariableName == input2.VariableName))
                {
                    result.Differences.Add(new TestCaseDifference
                    {
                        PropertyName = $"Input: {input2.VariableName}",
                        Value1 = "Missing",
                        Value2 = "Present"
                    });
                }
            }

            // Compare expected outputs
            foreach (var output1 in testCase1.ExpectedOutputs)
            {
                var matchingOutput = testCase2.ExpectedOutputs.FirstOrDefault(o => o.VariableName == output1.VariableName);
                if (matchingOutput == null)
                {
                    result.Differences.Add(new TestCaseDifference
                    {
                        PropertyName = $"ExpectedOutput: {output1.VariableName}",
                        Value1 = "Present",
                        Value2 = "Missing"
                    });
                }
                else if (output1.Value != matchingOutput.Value || output1.VariableType != matchingOutput.VariableType)
                {
                    result.Differences.Add(new TestCaseDifference
                    {
                        PropertyName = $"ExpectedOutput: {output1.VariableName}",
                        Value1 = $"Type: {output1.VariableType}, Value: {output1.Value}",
                        Value2 = $"Type: {matchingOutput.VariableType}, Value: {matchingOutput.Value}"
                    });
                }
            }

            foreach (var output2 in testCase2.ExpectedOutputs)
            {
                if (!testCase1.ExpectedOutputs.Any(o => o.VariableName == output2.VariableName))
                {
                    result.Differences.Add(new TestCaseDifference
                    {
                        PropertyName = $"ExpectedOutput: {output2.VariableName}",
                        Value1 = "Missing",
                        Value2 = "Present"
                    });
                }
            }

            return await Task.FromResult(result);
        }

        public async Task<TestCaseCoverageResult> AnalyzeTestCaseCoverageAsync(List<Models.TestCases.TestCase> testCases, CFunction function)
        {
            // Simplified implementation for now
            var result = new TestCaseCoverageResult
            {
                Function = function,
                TestCases = testCases,
                CoveragePercentage = 0,
                UncoveredPaths = new List<string>(),
                CoveredPaths = new List<string>()
            };

            // Basic coverage calculation - this would be more sophisticated in a real implementation
            if (function != null && testCases.Count > 0)
            {
                // Just a placeholder for now - would need actual code path analysis
                result.CoveragePercentage = Math.Min(100, testCases.Count * 10);

                // Add some sample paths
                result.CoveredPaths.Add("Main execution path");

                if (result.CoveragePercentage < 100)
                {
                    result.UncoveredPaths.Add("Error handling path");
                    result.UncoveredPaths.Add("Edge case: input validation");
                }
            }

            return await Task.FromResult(result);
        }

        #endregion

        #region Test Case Generation

        public async Task<Models.TestCases.TestCase> GenerateUnitTestCaseAsync(CFunction function)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));

            var testCase = new Models.TestCases.TestCase
            {
                Id = Guid.NewGuid(),
                Name = $"Test_{function.Name}",
                Description = $"Auto-generated unit test for function {function.Name}",
                FunctionName = function.Name,
                Type = TestCaseType.UnitTest,
                Status = TestCaseStatus.NotExecuted,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                Inputs = new List<TestCaseInput>(),
                ExpectedOutputs = new List<TestCaseOutput>(),
                ActualOutputs = new List<TestCaseOutput>()
            };

            // Generate inputs from function parameters
            foreach (var param in function.Parameters)
            {
                testCase.Inputs.Add(new TestCaseInput
                {
                    Id = Guid.NewGuid(),
                    VariableName = param.Name,
                    VariableType = param.Type,
                    Value = GenerateDefaultValueForType(param.Type),
                    IsStub = false
                });
            }

            // Generate expected output for return value
            if (function.ReturnType != "void")
            {
                testCase.ExpectedOutputs.Add(new TestCaseOutput
                {
                    Id = Guid.NewGuid(),
                    VariableName = "return",
                    VariableType = function.ReturnType,
                    Value = GenerateDefaultValueForType(function.ReturnType)
                });
            }

            // This is a simple implementation - a more sophisticated version would use the Z3 solver
            // to find interesting input values

            return await Task.FromResult(testCase);
        }

        public async Task<Models.TestCases.TestCase> GenerateIntegrationTestCaseAsync(List<CFunction> functions)
        {
            if (functions == null || functions.Count == 0)
                throw new ArgumentException("At least one function must be provided", nameof(functions));

            // For integration test, we'll use the first function as the main function
            var mainFunction = functions[0];

            var testCase = new Models.TestCases.TestCase
            {
                Id = Guid.NewGuid(),
                Name = $"IntegrationTest_{mainFunction.Name}",
                Description = $"Auto-generated integration test for function {mainFunction.Name} and related functions",
                FunctionName = mainFunction.Name,
                Type = TestCaseType.IntegrationTest,
                Status = TestCaseStatus.NotExecuted,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                Inputs = new List<TestCaseInput>(),
                ExpectedOutputs = new List<TestCaseOutput>(),
                ActualOutputs = new List<TestCaseOutput>()
            };

            // Generate inputs from main function parameters
            foreach (var param in mainFunction.Parameters)
            {
                testCase.Inputs.Add(new TestCaseInput
                {
                    Id = Guid.NewGuid(),
                    VariableName = param.Name,
                    VariableType = param.Type,
                    Value = GenerateDefaultValueForType(param.Type),
                    IsStub = false
                });
            }

            // Add stub inputs for other functions
            for (int i = 1; i < functions.Count; i++)
            {
                var function = functions[i];

                foreach (var param in function.Parameters)
                {
                    // Skip if parameter already exists
                    if (testCase.Inputs.Any(input => input.VariableName == param.Name))
                        continue;

                    testCase.Inputs.Add(new TestCaseInput
                    {
                        Id = Guid.NewGuid(),
                        VariableName = $"{function.Name}_{param.Name}",
                        VariableType = param.Type,
                        Value = GenerateDefaultValueForType(param.Type),
                        IsStub = true
                    });
                }
            }

            // Generate expected output for main function return value
            if (mainFunction.ReturnType != "void")
            {
                testCase.ExpectedOutputs.Add(new TestCaseOutput
                {
                    Id = Guid.NewGuid(),
                    VariableName = "return",
                    VariableType = mainFunction.ReturnType,
                    Value = GenerateDefaultValueForType(mainFunction.ReturnType)
                });
            }

            // This is a simple implementation - a more sophisticated version would use the Z3 solver
            // to find interesting input values and consider function interactions

            return await Task.FromResult(testCase);
        }

        #endregion

        #region Helper Methods

        private string GenerateTstContent(List<Models.TestCases.TestCase> testCases)
        {
            var sb = new StringBuilder();

            foreach (var testCase in testCases)
            {
                sb.AppendLine($"[TestCase]");
                sb.AppendLine($"Name={testCase.Name}");
                sb.AppendLine($"Description={testCase.Description}");
                sb.AppendLine($"Function={testCase.FunctionName}");
                sb.AppendLine($"Type={testCase.Type}");
                sb.AppendLine($"Status={testCase.Status}");

                sb.AppendLine("[Inputs]");
                foreach (var input in testCase.Inputs)
                {
                    sb.AppendLine($"{input.VariableName}({input.VariableType})={input.Value}|{input.IsStub}");
                }

                sb.AppendLine("[ExpectedOutputs]");
                foreach (var output in testCase.ExpectedOutputs)
                {
                    sb.AppendLine($"{output.VariableName}({output.VariableType})={output.Value}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private List<Models.TestCases.TestCase> ParseTstContent(string content)
        {
            var testCases = new List<Models.TestCases.TestCase>();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Models.TestCases.TestCase currentTestCase = null;
            bool inInputs = false;
            bool inOutputs = false;

            foreach (var line in lines)
            {
                if (line.Trim() == "[TestCase]")
                {
                    if (currentTestCase != null)
                    {
                        testCases.Add(currentTestCase);
                    }

                    currentTestCase = new Models.TestCases.TestCase
                    {
                        Id = Guid.NewGuid(),
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        Inputs = new List<TestCaseInput>(),
                        ExpectedOutputs = new List<TestCaseOutput>(),
                        ActualOutputs = new List<TestCaseOutput>()
                    };

                    inInputs = false;
                    inOutputs = false;
                }
                else if (line.Trim() == "[Inputs]")
                {
                    inInputs = true;
                    inOutputs = false;
                }
                else if (line.Trim() == "[ExpectedOutputs]")
                {
                    inInputs = false;
                    inOutputs = true;
                }
                else if (currentTestCase != null)
                {
                    if (inInputs)
                    {
                        ParseInputLine(line, currentTestCase);
                    }
                    else if (inOutputs)
                    {
                        ParseOutputLine(line, currentTestCase);
                    }
                    else
                    {
                        ParseTestCaseLine(line, currentTestCase);
                    }
                }
            }

            if (currentTestCase != null)
            {
                testCases.Add(currentTestCase);
            }

            return testCases;
        }

        private void ParseTestCaseLine(string line, Models.TestCases.TestCase testCase)
        {
            var parts = line.Split(new[] { '=' }, 2);
            if (parts.Length != 2)
                return;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            switch (key)
            {
                case "Name":
                    testCase.Name = value;
                    break;
                case "Description":
                    testCase.Description = value;
                    break;
                case "Function":
                    testCase.FunctionName = value;
                    break;
                case "Type":
                    if (Enum.TryParse<TestCaseType>(value, out var type))
                    {
                        testCase.Type = type;
                    }
                    break;
                case "Status":
                    if (Enum.TryParse<TestCaseStatus>(value, out var status))
                    {
                        testCase.Status = status;
                    }
                    break;
            }
        }

        private void ParseInputLine(string line, Models.TestCases.TestCase testCase)
        {
            // Format: variableName(variableType)=value|isStub
            var parts = line.Split(new[] { '=' }, 2);
            if (parts.Length != 2)
                return;

            var nameTypePart = parts[0].Trim();
            var valueStubPart = parts[1].Trim();

            var nameType = ParseVariableNameAndType(nameTypePart);
            if (nameType == null)
                return;

            var valueStub = valueStubPart.Split('|');
            var value = valueStub[0];
            var isStub = valueStub.Length > 1 && bool.TryParse(valueStub[1], out var stub) ? stub : false;

            testCase.Inputs.Add(new TestCaseInput
            {
                Id = Guid.NewGuid(),
                VariableName = nameType.Item1,
                VariableType = nameType.Item2,
                Value = value,
                IsStub = isStub
            });
        }

        private void ParseOutputLine(string line, Models.TestCases.TestCase testCase)
        {
            // Format: variableName(variableType)=value
            var parts = line.Split(new[] { '=' }, 2);
            if (parts.Length != 2)
                return;

            var nameTypePart = parts[0].Trim();
            var value = parts[1].Trim();

            var nameType = ParseVariableNameAndType(nameTypePart);
            if (nameType == null)
                return;

            testCase.ExpectedOutputs.Add(new TestCaseOutput
            {
                Id = Guid.NewGuid(),
                VariableName = nameType.Item1,
                VariableType = nameType.Item2,
                Value = value
            });
        }

        private Tuple<string, string> ParseVariableNameAndType(string nameTypePart)
        {
            // Format: variableName(variableType)
            var openParen = nameTypePart.IndexOf('(');
            var closeParen = nameTypePart.IndexOf(')');

            if (openParen <= 0 || closeParen <= openParen)
                return null;

            var name = nameTypePart.Substring(0, openParen).Trim();
            var type = nameTypePart.Substring(openParen + 1, closeParen - openParen - 1).Trim();

            return new Tuple<string, string>(name, type);
        }

        private string GenerateDefaultValueForType(string type)
        {
            // Generate a sensible default value for the given C type
            switch (type.Trim())
            {
                case "int":
                case "long":
                case "short":
                    return "0";
                case "unsigned int":
                case "unsigned long":
                case "unsigned short":
                case "size_t":
                    return "0";
                case "float":
                case "double":
                    return "0.0";
                case "char":
                    return "'a'";
                case "bool":
                    return "false";
                case "char*":
                case "const char*":
                    return "\"test\"";
                default:
                    if (type.Contains("*"))
                        return "NULL";
                    return "0";
            }
        }

        #endregion
    }

    #region CSV Record Classes

    public class TestCaseCsvRecord
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
        public string ModifiedDate { get; set; } = string.Empty;
    }

    public class TestCaseVariableCsvRecord
    {
        public string TestCaseName { get; set; } = string.Empty;
        public string VariableName { get; set; } = string.Empty;
        public string VariableType { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsInput { get; set; }
        public bool IsStub { get; set; }
    }

    #endregion
}