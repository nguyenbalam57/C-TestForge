// File: C-TestForge.Parser/ClangSharpParserService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ClangSharp;
using ClangSharp.Interop;
using C_TestForge.Models;

namespace C_TestForge.Parser
{
    public class ClangSharpParserService : IParser
    {
        private readonly Dictionary<string, CSourceFile> _parsedFiles = new Dictionary<string, CSourceFile>();
        private CXTranslationUnit _translationUnit;
        private CXIndex _index;
        private readonly Dictionary<string, CDefinition> _definitions = new Dictionary<string, CDefinition>();
        private readonly Dictionary<CXCursor, CPreprocessorDirective> _preprocessorDirectives = new Dictionary<CXCursor, CPreprocessorDirective>();
        private readonly Stack<CPreprocessorDirective> _directiveStack = new Stack<CPreprocessorDirective>();
        private readonly Dictionary<CXCursor, CFunction> _functions = new Dictionary<CXCursor, CFunction>();

        public unsafe CSourceFile ParseFile(string filePath, IEnumerable<string> includePaths = null, IEnumerable<KeyValuePair<string, string>> defines = null)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Source file not found: {filePath}");
            }

            var content = File.ReadAllText(filePath);
            var sourceFile = new CSourceFile(filePath, content);

            try
            {
                // Create Clang index
                _index = CXIndex.Create();

                // Prepare command line arguments for clang
                var args = PrepareClangArguments(includePaths, defines);

                // Parse the translation unit
                _translationUnit = CXTranslationUnit.Parse(_index, filePath, args, Array.Empty<CXUnsavedFile>(), CXTranslationUnit_Flags.CXTranslationUnit_DetailedPreprocessingRecord);

                if (_translationUnit.NumDiagnostics > 0)
                {
                    // Log diagnostics but continue if possible
                    for (uint i = 0; i < _translationUnit.NumDiagnostics; i++)
                    {
                        using var diagnostic = _translationUnit.GetDiagnostic(i);
                        Console.WriteLine($"Diagnostic: {diagnostic.Format(CXDiagnosticDisplayOptions.CXDiagnostic_DisplayOption)}");
                    }
                }

                // Extract preprocessor directives and definitions
                ExtractPreprocessorInfo(sourceFile);

                // Create a cursor visitor to process the AST
                var cursor = _translationUnit.Cursor;
                cursor.VisitChildren(VisitNode, new CXClientData(IntPtr.Zero));

                // Process functions and variables
                foreach (var functionPair in _functions)
                {
                    sourceFile.Functions.Add(functionPair.Value);
                }

                // Add all definitions to the source file
                sourceFile.Definitions.AddRange(_definitions.Values);

                _parsedFiles[filePath] = sourceFile;
                return sourceFile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing file {filePath}: {ex.Message}");
                return sourceFile;
            }
            finally
            {
                // Clean up resources
                _translationUnit.Dispose();
                _index.Dispose();
            }
        }

        private string[] PrepareClangArguments(IEnumerable<string> includePaths, IEnumerable<KeyValuePair<string, string>> defines)
        {
            var args = new List<string>
            {
                "-x", "c",  // Force C language
                "--std=c99" // Use C99 standard
            };

            // Add include paths
            if (includePaths != null)
            {
                foreach (var path in includePaths)
                {
                    args.Add($"-I{path}");
                }
            }

            // Add preprocessor definitions
            if (defines != null)
            {
                foreach (var define in defines)
                {
                    if (string.IsNullOrEmpty(define.Value))
                    {
                        args.Add($"-D{define.Key}");
                    }
                    else
                    {
                        args.Add($"-D{define.Key}={define.Value}");
                    }
                }
            }

            return args.ToArray();
        }

        private void ExtractPreprocessorInfo(CSourceFile sourceFile)
        {
            var fileName = Path.GetFileName(sourceFile.FilePath);
            var fileContent = sourceFile.Content;
            var lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            _translationUnit.Cursor.VisitChildren((cursor, parent, clientData) =>
            {
                if (cursor.Location.IsFromMainFile &&
                    (cursor.Kind == CXCursorKind.CXCursor_MacroDefinition ||
                     cursor.Kind == CXCursorKind.CXCursor_MacroExpansion ||
                     cursor.Kind == CXCursorKind.CXCursor_InclusionDirective ||
                     cursor.Kind == CXCursorKind.CXCursor_PreprocessingDirective))
                {
                    var location = cursor.Location;
                    location.GetFileLocation(out _, out uint line, out uint column, out _);

                    if (line - 1 < lines.Length)
                    {
                        var lineText = lines[line - 1].Trim();

                        if (cursor.Kind == CXCursorKind.CXCursor_MacroDefinition)
                        {
                            ProcessMacroDefinition(cursor, (int)line, lineText, sourceFile);
                        }
                        else if (lineText.StartsWith("#if") || lineText.StartsWith("#ifdef") ||
                                 lineText.StartsWith("#ifndef") || lineText.StartsWith("#else") ||
                                 lineText.StartsWith("#elif") || lineText.StartsWith("#endif"))
                        {
                            ProcessPreprocessorDirective(cursor, (int)line, lineText, sourceFile);
                        }
                        else if (lineText.StartsWith("#include"))
                        {
                            var directive = new CPreprocessorDirective
                            {
                                Type = PreprocessorType.Include,
                                LineNumber = (int)line,
                                RawText = lineText,
                                Value = lineText.Substring(8).Trim(),
                                IsEnabled = true
                            };

                            sourceFile.PreprocessorDirectives.Add(directive);
                        }
                    }
                }

                return CXChildVisitResult.CXChildVisit_Continue;
            }, new CXClientData(IntPtr.Zero));
        }

        private void ProcessMacroDefinition(CXCursor cursor, int lineNumber, string lineText, CSourceFile sourceFile)
        {
            var name = cursor.Spelling.ToString();
            var tokens = GetTokens(cursor);

            if (tokens.Count > 1)
            {
                var definition = new CDefinition
                {
                    Name = name,
                    LineNumber = lineNumber,
                    RawText = lineText,
                    IsEnabled = true
                };

                // Check if it's a function-like macro
                if (tokens.Count > 2 && tokens[1].Spelling.ToString() == "(")
                {
                    definition.Type = DefinitionType.FunctionLike;

                    // Extract parameters
                    int paramStart = 2;
                    int paramEnd = tokens.FindIndex(paramStart, t => t.Spelling.ToString() == ")");

                    if (paramEnd > paramStart)
                    {
                        for (int i = paramStart; i < paramEnd; i++)
                        {
                            var token = tokens[i].Spelling.ToString();
                            if (token != "," && !string.IsNullOrWhiteSpace(token))
                            {
                                definition.Parameters.Add(token);
                            }
                        }
                    }

                    // Extract value (everything after the closing parenthesis)
                    if (paramEnd < tokens.Count - 1)
                    {
                        var valueBuilder = new StringBuilder();
                        for (int i = paramEnd + 1; i < tokens.Count; i++)
                        {
                            valueBuilder.Append(tokens[i].Spelling.ToString());
                            if (i < tokens.Count - 1) valueBuilder.Append(" ");
                        }
                        definition.Value = valueBuilder.ToString().Trim();
                    }
                }
                else if (tokens.Count > 1)
                {
                    // Simple define or simple macro
                    definition.Type = tokens.Count > 2 ? DefinitionType.Macro : DefinitionType.Simple;

                    // Extract value (everything after the name)
                    var valueBuilder = new StringBuilder();
                    for (int i = 1; i < tokens.Count; i++)
                    {
                        valueBuilder.Append(tokens[i].Spelling.ToString());
                        if (i < tokens.Count - 1) valueBuilder.Append(" ");
                    }
                    definition.Value = valueBuilder.ToString().Trim();
                }

                sourceFile.Definitions.Add(definition);
                _definitions[name] = definition;

                // Check if this define is inside a conditional directive
                if (_directiveStack.Count > 0)
                {
                    definition.ParentDirective = _directiveStack.Peek();
                }
            }
        }

        private void ProcessPreprocessorDirective(CXCursor cursor, int lineNumber, string lineText, CSourceFile sourceFile)
        {
            var directive = new CPreprocessorDirective
            {
                LineNumber = lineNumber,
                RawText = lineText,
                IsEnabled = true
            };

            if (lineText.StartsWith("#if "))
            {
                directive.Type = PreprocessorType.If;
                directive.Condition = lineText.Substring(4).Trim();
            }
            else if (lineText.StartsWith("#ifdef "))
            {
                directive.Type = PreprocessorType.Ifdef;
                directive.Condition = lineText.Substring(7).Trim();
            }
            else if (lineText.StartsWith("#ifndef "))
            {
                directive.Type = PreprocessorType.Ifndef;
                directive.Condition = lineText.Substring(8).Trim();
            }
            else if (lineText.StartsWith("#else"))
            {
                directive.Type = PreprocessorType.Else;
            }
            else if (lineText.StartsWith("#elif "))
            {
                directive.Type = PreprocessorType.Elif;
                directive.Condition = lineText.Substring(6).Trim();
            }
            else if (lineText.StartsWith("#endif"))
            {
                directive.Type = PreprocessorType.Endif;

                // Pop from stack if we have items
                if (_directiveStack.Count > 0)
                {
                    _directiveStack.Pop();
                }
            }

            // Add to source file
            sourceFile.PreprocessorDirectives.Add(directive);

            // Manage the stack of active directives
            if (directive.Type == PreprocessorType.If ||
                directive.Type == PreprocessorType.Ifdef ||
                directive.Type == PreprocessorType.Ifndef)
            {
                _directiveStack.Push(directive);
            }
            else if (directive.Type == PreprocessorType.Else ||
                     directive.Type == PreprocessorType.Elif)
            {
                if (_directiveStack.Count > 0)
                {
                    var parent = _directiveStack.Pop();
                    parent.Children.Add(directive);
                    _directiveStack.Push(directive);
                }
                else
                {
                    _directiveStack.Push(directive);
                }
            }

            // Store in dictionary
            _preprocessorDirectives[cursor] = directive;
        }

        private unsafe CXChildVisitResult VisitNode(CXCursor cursor, CXCursor parent, IntPtr data)
        {
            // Only process nodes from the main file
            if (!cursor.Location.IsFromMainFile)
            {
                return CXChildVisitResult.CXChildVisit_Continue;
            }

            switch (cursor.Kind)
            {
                case CXCursorKind.CXCursor_FunctionDecl:
                    ProcessFunction(cursor);
                    break;
                case CXCursorKind.CXCursor_VarDecl:
                    ProcessVariable(cursor, parent);
                    break;
                case CXCursorKind.CXCursor_ParmDecl:
                    // Parameters are processed within the function processing
                    break;
            }

            return CXChildVisitResult.CXChildVisit_Recurse;
        }

        private unsafe void ProcessFunction(CXCursor cursor)
        {
            var functionName = cursor.Spelling.ToString();
            var returnType = cursor.ResultType.Spelling.ToString();

            // Get location information
            var location = cursor.Location;
            location.GetFileLocation(out _, out uint line, out _, out _);

            // Get end location
            var extent = cursor.Extent;
            extent.End.GetFileLocation(out _, out uint endLine, out _, out _);

            // Create function object
            var function = new CFunction
            {
                Name = functionName,
                ReturnType = returnType,
                LineNumber = (int)line,
                EndLineNumber = (int)endLine,
                IsEnabled = true
            };

            // Determine storage class
            if (cursor.IsDynamicCall)
            {
                function.StorageClass = FunctionStorageClass.Extern;
            }
            else if (cursor.StorageClass == CX_StorageClass.CX_SC_Static)
            {
                function.StorageClass = FunctionStorageClass.Static;
            }
            else
            {
                function.StorageClass = FunctionStorageClass.None;
            }

            // Process parameters
            cursor.VisitChildren((childCursor, parentCursor, clientData) =>
            {
                if (childCursor.Kind == CXCursorKind.CXCursor_ParmDecl)
                {
                    var param = ProcessParameter(childCursor, function);
                    function.Parameters.Add(param);
                }

                return CXChildVisitResult.CXChildVisit_Continue;
            }, new CXClientData(IntPtr.Zero));

            // Process function body
            StringBuilder bodyBuilder = new StringBuilder();
            cursor.VisitChildren((childCursor, parentCursor, clientData) =>
            {
                if (childCursor.Kind == CXCursorKind.CXCursor_CompoundStmt)
                {
                    var range = childCursor.Extent;
                    var bodyText = GetSourceText(range);
                    bodyBuilder.Append(bodyText);

                    // Extract local variables and function calls from the body
                    childCursor.VisitChildren((localCursor, localParent, localData) =>
                    {
                        if (localCursor.Kind == CXCursorKind.CXCursor_VarDecl)
                        {
                            var localVar = ProcessLocalVariable(localCursor, function);
                            function.LocalVariables.Add(localVar);
                        }
                        else if (localCursor.Kind == CXCursorKind.CXCursor_CallExpr)
                        {
                            var calledFunction = localCursor.Spelling.ToString();
                            if (!string.IsNullOrEmpty(calledFunction) && !function.CalledFunctions.Contains(calledFunction))
                            {
                                function.CalledFunctions.Add(calledFunction);
                            }
                        }

                        return CXChildVisitResult.CXChildVisit_Recurse;
                    }, new CXClientData(IntPtr.Zero));
                }

                return CXChildVisitResult.CXChildVisit_Continue;
            }, new CXClientData(IntPtr.Zero));

            function.Body = bodyBuilder.ToString();

            // Check if this function is inside a conditional directive
            if (_directiveStack.Count > 0)
            {
                function.ParentDirective = _directiveStack.Peek();
            }

            _functions[cursor] = function;
        }

        private CVariable ProcessParameter(CXCursor cursor, CFunction parentFunction)
        {
            var paramName = cursor.Spelling.ToString();
            var paramType = cursor.Type.Spelling.ToString();

            // Get location
            var location = cursor.Location;
            location.GetFileLocation(out _, out uint line, out _, out _);

            var param = new CVariable
            {
                Name = paramName,
                Type = paramType,
                Scope = VariableScope.Parameter,
                StorageClass = VariableStorageClass.Auto,
                LineNumber = (int)line,
                IsEnabled = true,
                ParentFunction = parentFunction
            };

            // Check if it's a pointer or array
            param.IsPointer = paramType.Contains("*");
            param.IsArray = paramType.Contains("[") && paramType.Contains("]");

            return param;
        }

        private CVariable ProcessLocalVariable(CXCursor cursor, CFunction parentFunction)
        {
            var varName = cursor.Spelling.ToString();
            var varType = cursor.Type.Spelling.ToString();

            // Get location
            var location = cursor.Location;
            location.GetFileLocation(out _, out uint line, out _, out _);

            var variable = new CVariable
            {
                Name = varName,
                Type = varType,
                Scope = VariableScope.Local,
                LineNumber = (int)line,
                IsEnabled = true,
                ParentFunction = parentFunction
            };

            // Determine storage class
            if (cursor.StorageClass == CX_StorageClass.CX_SC_Static)
            {
                variable.StorageClass = VariableStorageClass.Static;
            }
            else
            {
                variable.StorageClass = VariableStorageClass.Auto;
            }

            // Check if it's constant
            variable.IsConstant = cursor.Type.IsConstQualified;

            // Check if it's a pointer or array
            variable.IsPointer = varType.Contains("*");
            variable.IsArray = varType.Contains("[") && varType.Contains("]");

            // Try to extract default value
            cursor.VisitChildren((childCursor, childParent, clientData) =>
            {
                if (childCursor.Kind == CXCursorKind.CXCursor_IntegerLiteral ||
                    childCursor.Kind == CXCursorKind.CXCursor_FloatingLiteral ||
                    childCursor.Kind == CXCursorKind.CXCursor_StringLiteral ||
                    childCursor.Kind == CXCursorKind.CXCursor_CharacterLiteral)
                {
                    var tokenList = GetTokens(childCursor);
                    if (tokenList.Count > 0)
                    {
                        variable.DefaultValue = string.Join(" ", tokenList.Select(t => t.Spelling.ToString()));
                    }
                }
                return CXChildVisitResult.CXChildVisit_Continue;
            }, new CXClientData(IntPtr.Zero));

            return variable;
        }

        private void ProcessVariable(CXCursor cursor, CXCursor parent)
        {
            // Skip if parent is a function - we handle local variables separately
            if (parent.Kind == CXCursorKind.CXCursor_FunctionDecl)
            {
                return;
            }

            var varName = cursor.Spelling.ToString();
            var varType = cursor.Type.Spelling.ToString();

            // Get location
            var location = cursor.Location;
            location.GetFileLocation(out _, out uint line, out _, out _);

            var variable = new CVariable
            {
                Name = varName,
                Type = varType,
                Scope = VariableScope.Global,
                LineNumber = (int)line,
                IsEnabled = true
            };

            // Determine storage class
            if (cursor.StorageClass == CX_StorageClass.CX_SC_Static)
            {
                variable.StorageClass = VariableStorageClass.Static;
            }
            else if (cursor.StorageClass == CX_StorageClass.CX_SC_Extern)
            {
                variable.StorageClass = VariableStorageClass.Extern;
            }
            else
            {
                variable.StorageClass = VariableStorageClass.None;
            }

            // Check if it's constant
            variable.IsConstant = cursor.Type.IsConstQualified;

            // Check if it's a pointer or array
            variable.IsPointer = varType.Contains("*");
            variable.IsArray = varType.Contains("[") && varType.Contains("]");

            // Try to extract default value
            cursor.VisitChildren((childCursor, childParent, clientData) =>
            {
                if (childCursor.Kind == CXCursorKind.CXCursor_IntegerLiteral ||
                    childCursor.Kind == CXCursorKind.CXCursor_FloatingLiteral ||
                    childCursor.Kind == CXCursorKind.CXCursor_StringLiteral ||
                    childCursor.Kind == CXCursorKind.CXCursor_CharacterLiteral)
                {
                    var tokenList = GetTokens(childCursor);
                    if (tokenList.Count > 0)
                    {
                        variable.DefaultValue = string.Join(" ", tokenList.Select(t => t.Spelling.ToString()));
                    }
                }
                return CXChildVisitResult.CXChildVisit_Continue;
            }, new CXClientData(IntPtr.Zero));

            // Check if this variable is inside a conditional directive
            if (_directiveStack.Count > 0)
            {
                variable.ParentDirective = _directiveStack.Peek();
            }
        }

        private List<CXToken> GetTokens(CXCursor cursor)
        {
            var range = cursor.Extent;
            var tokenList = new List<CXToken>();

            CXToken.Tokenize(_translationUnit, range, out CXToken[] tokens);

            if (tokens != null && tokens.Length > 0)
            {
                tokenList.AddRange(tokens);
            }

            return tokenList;
        }

        private string GetSourceText(CXSourceRange range)
        {
            var start = range.Start;
            var end = range.End;

            start.GetFileLocation(out CXFile startFile, out uint startLine, out uint startColumn, out uint startOffset);
            end.GetFileLocation(out CXFile endFile, out uint endLine, out uint endColumn, out uint endOffset);

            if (startFile.Name != endFile.Name)
            {
                return string.Empty;
            }

            var length = endOffset - startOffset;
            var fileContent = File.ReadAllText(startFile.Name);

            if (startOffset < fileContent.Length && startOffset + length <= fileContent.Length)
            {
                return fileContent.Substring((int)startOffset, (int)length);
            }

            return string.Empty;
        }
    }
}