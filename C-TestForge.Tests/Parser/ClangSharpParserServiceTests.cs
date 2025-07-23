using System;
using System.Collections.Generic;
using System.IO;
using C_TestForge.Models;
using C_TestForge.Parser;
using FluentAssertions;
using Xunit;

namespace C_TestForge.Tests.Parser
{
    public class ClangSharpParserServiceTests
    {
        private readonly ClangSharpParserService _parser;
        private readonly string _tempFolder;

        public ClangSharpParserServiceTests()
        {
            _parser = new ClangSharpParserService();
            _tempFolder = Path.Combine(Path.GetTempPath(), "C-TestForge-Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempFolder);
        }

        [Fact]
        public void ParseFile_SimpleFile_ShouldParseCorrectly()
        {
            // Arrange
            var filePath = Path.Combine(_tempFolder, "simple.c");
            File.WriteAllText(filePath, @"
#include <stdio.h>

#define MAX_VALUE 100
#define SQR(x) ((x) * (x))

int globalVar = 10;
static float pi = 3.14f;

int add(int a, int b) {
    return a + b;
}

void printMessage(const char* message) {
    printf(""%s\n"", message);
}

int main() {
    int localVar = 5;
    float result = pi * SQR(localVar);
    printf(""Result: %f\n"", result);
    printMessage(""Hello, World!"");
    return 0;
}
");

            // Act
            var sourceFile = _parser.ParseFile(filePath);

            // Assert
            sourceFile.Should().NotBeNull();
            sourceFile.FilePath.Should().Be(filePath);

            // Check #define
            sourceFile.Definitions.Should().HaveCountGreaterThanOrEqualTo(2);
            sourceFile.Definitions.Should().Contain(d => d.Name == "MAX_VALUE" && d.Value == "100");
            sourceFile.Definitions.Should().Contain(d => d.Name == "SQR" && d.Type == DefinitionType.FunctionLike);

            // Check functions
            sourceFile.Functions.Should().HaveCountGreaterThanOrEqualTo(3);
            sourceFile.Functions.Should().Contain(f => f.Name == "add" && f.ReturnType == "int" && f.Parameters.Count == 2);
            sourceFile.Functions.Should().Contain(f => f.Name == "printMessage" && f.ReturnType == "void" && f.Parameters.Count == 1);
            sourceFile.Functions.Should().Contain(f => f.Name == "main" && f.ReturnType == "int" && f.Parameters.Count == 0);

            // Check variables
            var addFunction = sourceFile.Functions.Find(f => f.Name == "add");
            addFunction.Should().NotBeNull();
            addFunction.Parameters.Should().HaveCount(2);
            addFunction.Parameters[0].Name.Should().Be("a");
            addFunction.Parameters[0].Type.Should().Be("int");
            addFunction.Parameters[1].Name.Should().Be("b");
            addFunction.Parameters[1].Type.Should().Be("int");

            var mainFunction = sourceFile.Functions.Find(f => f.Name == "main");
            mainFunction.Should().NotBeNull();
            mainFunction.LocalVariables.Should().HaveCountGreaterThanOrEqualTo(2);
            mainFunction.LocalVariables.Should().Contain(v => v.Name == "localVar" && v.Type == "int");
            mainFunction.LocalVariables.Should().Contain(v => v.Name == "result" && v.Type == "float");

            // Check function calls
            mainFunction.CalledFunctions.Should().Contain("printf");
            mainFunction.CalledFunctions.Should().Contain("printMessage");
        }

        [Fact]
        public void ParseFile_WithPreprocessorDirectives_ShouldParseCorrectly()
        {
            // Arrange
            var filePath = Path.Combine(_tempFolder, "preprocessor.c");
            File.WriteAllText(filePath, @"
#include <stdio.h>

#define DEBUG 1

#ifdef DEBUG
#define LOG(msg) printf(""%s\n"", msg)
#else
#define LOG(msg)
#endif

#if DEBUG > 0
int debugMode = 1;
#else
int debugMode = 0;
#endif

int main() {
    LOG(""Debug mode is active"");
    
    #ifdef DEBUG
    printf(""Debug build\n"");
    #else
    printf(""Release build\n"");
    #endif
    
    return debugMode;
}
");

            // Act
            var sourceFile = _parser.ParseFile(filePath);

            // Assert
            sourceFile.Should().NotBeNull();

            // Check preprocessor directives
            sourceFile.PreprocessorDirectives.Should().HaveCountGreaterThanOrEqualTo(6);
            sourceFile.PreprocessorDirectives.Should().Contain(d => d.Type == PreprocessorType.Include);
            sourceFile.PreprocessorDirectives.Should().Contain(d => d.Type == PreprocessorType.Ifdef && d.Condition == "DEBUG");
            sourceFile.PreprocessorDirectives.Should().Contain(d => d.Type == PreprocessorType.If);

            // Check definitions
            sourceFile.Definitions.Should().HaveCountGreaterThanOrEqualTo(2);
            sourceFile.Definitions.Should().Contain(d => d.Name == "DEBUG" && d.Value == "1");
            sourceFile.Definitions.Should().Contain(d => d.Name == "LOG" && d.Type == DefinitionType.FunctionLike);

            // Check functions
            sourceFile.Functions.Should().HaveCount(1);
            sourceFile.Functions[0].Name.Should().Be("main");

            // Variables
            sourceFile.Variables.Should().Contain(v => v.Name == "debugMode" && v.Type == "int");
        }

        [Fact]
        public void ParseFile_WithGlobalVariables_ShouldParseCorrectly()
        {
            // Arrange
            var filePath = Path.Combine(_tempFolder, "globals.c");
            File.WriteAllText(filePath, @"
#include <stdio.h>

// Constants
const int MAX_USERS = 100;
const char* APP_NAME = ""TestApp"";

// Global variables
int g_userCount = 0;
float g_version = 1.0f;

// Static globals
static char g_buffer[1024];
static unsigned int g_errorCode = 0;

typedef struct {
    int id;
    char name[50];
    float balance;
} User;

User g_users[MAX_USERS];

int main() {
    printf(""App: %s (v%.1f)\n"", APP_NAME, g_version);
    printf(""Max users: %d\n"", MAX_USERS);
    return 0;
}
");

            // Act
            var sourceFile = _parser.ParseFile(filePath);

            // Assert
            sourceFile.Should().NotBeNull();

            // Check variables
            sourceFile.Variables.Should().HaveCountGreaterThanOrEqualTo(6);
            sourceFile.Variables.Should().Contain(v => v.Name == "MAX_USERS" && v.IsConstant);
            sourceFile.Variables.Should().Contain(v => v.Name == "APP_NAME" && v.IsConstant && v.IsPointer);
            sourceFile.Variables.Should().Contain(v => v.Name == "g_userCount" && v.DefaultValue == "0");
            sourceFile.Variables.Should().Contain(v => v.Name == "g_version");
            sourceFile.Variables.Should().Contain(v => v.Name == "g_buffer" && v.IsArray);
            sourceFile.Variables.Should().Contain(v => v.Name == "g_errorCode" && v.StorageClass == VariableStorageClass.Static);
            sourceFile.Variables.Should().Contain(v => v.Name == "g_users" && v.IsArray);
        }

        public void Dispose()
        {
            // Clean up temp files
            if (Directory.Exists(_tempFolder))
            {
                Directory.Delete(_tempFolder, true);
            }
        }
    }
}
