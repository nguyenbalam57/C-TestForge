using C_TestForge.Models;
using C_TestForge.Models.TestCases;
using CsvHelper;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.TestCase.Services
{
    // C-TestForge.TestCase/Services/TestCaseService.cs
    public class TestCaseService : ITestCaseService
    {
        private readonly ITestCaseRepository _repository;

        public TestCaseService(ITestCaseRepository repository)
        {
            _repository = repository;
        }

        // Triển khai các phương thức import/export TST
        public async Task<List<TestCaseCustom>> ImportFromTstFileAsync(string filePath)
        {
            // Đọc file .tst (định dạng riêng)
            var content = await File.ReadAllTextAsync(filePath);
            var testCases = ParseTstContent(content);

            // Lưu vào database
            foreach (var testCase in testCases)
            {
                await _repository.CreateAsync(testCase);
            }

            return testCases;
        }

        public async Task ExportToTstFileAsync(List<TestCaseCustom> testCases, string filePath)
        {
            var content = GenerateTstContent(testCases);
            await File.WriteAllTextAsync(filePath, content);
        }

        // Triển khai các phương thức import/export CSV sử dụng CsvHelper
        public async Task<List<TestCaseCustom>> ImportFromCsvFileAsync(string filePath)
        {
            var testCases = new List<TestCaseCustom>();

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                // Cấu hình CSV mapping
                csv.Context.RegisterClassMap<TestCaseMap>();

                // Đọc records từ CSV
                var records = csv.GetRecords<TestCaseCsvRecord>().ToList();

                // Chuyển đổi records thành TestCase objects
                testCases = ConvertCsvRecordsToTestCases(records);

                // Lưu vào database
                foreach (var testCase in testCases)
                {
                    await _repository.CreateAsync(testCase);
                }
            }

            return testCases;
        }

        // Các phương thức helper cho việc parse và generate
        private List<TestCaseCustom> ParseTstContent(string content) { /* Implementation */ }
        private string GenerateTstContent(List<TestCaseCustom> testCases) { /* Implementation */ }
        private List<TestCaseCustom> ConvertCsvRecordsToTestCases(List<TestCaseCsvRecord> records) { /* Implementation */ }

        // Các phương thức CRUD và so sánh khác...

        public async Task<List<TestCaseCustom>> ImportFromExcelFileAsync(string filePath)
        {
            var testCases = new List<TestCaseCustom>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Lấy sheet đầu tiên

                // Đọc số hàng và cột
                int rows = worksheet.Dimension.Rows;
                int cols = worksheet.Dimension.Columns;

                // Đọc headers
                var headers = new List<string>();
                for (int col = 1; col <= cols; col++)
                {
                    headers.Add(worksheet.Cells[1, col].Value?.ToString());
                }

                // Đọc dữ liệu từ Excel và chuyển đổi thành TestCase
                for (int row = 2; row <= rows; row++)
                {
                    var testCase = new TestCaseCustom
                    {
                        Id = Guid.NewGuid(),
                        Name = worksheet.Cells[row, headers.IndexOf("Name") + 1].Value?.ToString(),
                        Description = worksheet.Cells[row, headers.IndexOf("Description") + 1].Value?.ToString(),
                        FunctionName = worksheet.Cells[row, headers.IndexOf("FunctionName") + 1].Value?.ToString(),
                        // Đọc các thuộc tính khác...
                    };

                    // Đọc Inputs và ExpectedOutputs từ các cột khác
                    // (Cần xử lý thêm để đọc đúng cấu trúc)

                    testCases.Add(testCase);
                    await _repository.CreateAsync(testCase);
                }
            }

            return testCases;
        }

        public async Task ExportToExcelFileAsync(List<TestCaseCustom> testCases, string filePath)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("TestCases");

                // Thêm headers
                worksheet.Cells[1, 1].Value = "Name";
                worksheet.Cells[1, 2].Value = "Description";
                worksheet.Cells[1, 3].Value = "FunctionName";
                // Thêm các headers khác...

                // Thêm dữ liệu
                for (int i = 0; i < testCases.Count; i++)
                {
                    var testCase = testCases[i];
                    int row = i + 2;

                    worksheet.Cells[row, 1].Value = testCase.Name;
                    worksheet.Cells[row, 2].Value = testCase.Description;
                    worksheet.Cells[row, 3].Value = testCase.FunctionName;
                    // Thêm các giá trị khác...

                    // Thêm Inputs và ExpectedOutputs vào các cột riêng
                    // (Cần xử lý thêm để ghi đúng cấu trúc)
                }

                // Lưu file
                await package.SaveAsAsync(new FileInfo(filePath));
            }
        }
    }
}
