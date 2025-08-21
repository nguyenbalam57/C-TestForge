using C_TestForge.Models.CodeAnalysis.BaseClasss;
using C_TestForge.Models.CodeAnalysis.ClangASTNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace C_TestForge.Models.CodeAnalysis
{
    // Class cho hàm
    public class Function : CodeElement
    {
        public DataType ReturnType { get; set; }
        public List<Parameter> Parameters { get; set; }
        public StorageClass StorageClass { get; set; }
        public FunctionSpecifier Specifier { get; set; }
        public bool IsInline { get; set; }
        public bool IsStatic { get; set; }
        public bool IsExtern { get; set; }
        public bool IsVariadic { get; set; }
        public bool IsPrototype { get; set; }
        public bool IsRecursive { get; set; }
        public List<Variable> LocalVariables { get; set; }
        public List<Function> CalledFunctions { get; set; }
        public List<Function> CallingFunctions { get; set; }
        public string Body { get; set; }
        public int CyclomaticComplexity { get; set; }
        public AccessLevel AccessLevel { get; set; }

        // Additional analysis properties
        public bool IsMain { get; set; }
        public bool IsCallback { get; set; }
        public int ParameterCount => Parameters?.Count ?? 0;
        public int LineCount { get; set; }
        public List<string> CalledFunctionNames { get; set; }
        public List<Variable> AccessedGlobalVariables { get; set; }
        public Dictionary<string, int> VariableUsageCount { get; set; }
        public bool HasReturnStatement { get; set; }
        public List<SourceLocation> ReturnStatements { get; set; }

        public Function()
        {
            Parameters = new List<Parameter>();
            LocalVariables = new List<Variable>();
            CalledFunctions = new List<Function>();
            CallingFunctions = new List<Function>();
            CalledFunctionNames = new List<string>();
            AccessedGlobalVariables = new List<Variable>();
            VariableUsageCount = new Dictionary<string, int>();
            ReturnStatements = new List<SourceLocation>();
            AccessLevel = AccessLevel.Public;
            StorageClass = StorageClass.Auto;
            CyclomaticComplexity = 1; // Default complexity
        }

        public Function(string name, DataType returnType) : this()
        {
            Name = name;
            ReturnType = returnType;
            IsMain = name == "main";
        }

        public void AddParameter(Parameter parameter)
        {
            if (parameter != null)
            {
                parameter.Position = Parameters.Count;
                parameter.OwningFunction = this;
                Parameters.Add(parameter);
            }
        }

        public void AddLocalVariable(Variable variable)
        {
            if (variable != null && !LocalVariables.Contains(variable))
            {
                variable.IsLocal = true;
                variable.Scope = ScopeType.FunctionScope;
                LocalVariables.Add(variable);
            }
        }

        public void AddFunctionCall(Function calledFunction)
        {
            if (calledFunction != null && !CalledFunctions.Contains(calledFunction))
            {
                CalledFunctions.Add(calledFunction);
                calledFunction.CallingFunctions.Add(this);
                AddDependency(calledFunction);

                if (!CalledFunctionNames.Contains(calledFunction.Name))
                    CalledFunctionNames.Add(calledFunction.Name);

                // Check for recursion
                if (calledFunction == this || calledFunction.Name == this.Name)
                    IsRecursive = true;
            }
        }

        public void AddFunctionCallByName(string functionName)
        {
            if (!string.IsNullOrEmpty(functionName) && !CalledFunctionNames.Contains(functionName))
            {
                CalledFunctionNames.Add(functionName);
            }
        }

        public Parameter GetParameter(string paramName)
        {
            return Parameters.FirstOrDefault(p => p.Name == paramName);
        }

        public Parameter GetParameter(int index)
        {
            return index >= 0 && index < Parameters.Count ? Parameters[index] : null;
        }

        public Variable GetLocalVariable(string varName)
        {
            return LocalVariables.FirstOrDefault(v => v.Name == varName);
        }

        public bool IsPublic()
        {
            return !IsStatic && AccessLevel == AccessLevel.Public;
        }

        public bool IsLibraryFunction()
        {
            return IsInSystemHeader() || IsExtern;
        }

        public bool HasVoidReturn()
        {
            return ReturnType?.BaseType == "void";
        }

        public bool ReturnsPointer()
        {
            return ReturnType?.IsPointer == true;
        }

        public bool HasPointerParameters()
        {
            return Parameters.Any(p => p.Type?.IsPointer == true);
        }

        public bool HasConstParameters()
        {
            return Parameters.Any(p => p.IsConst);
        }

        public int GetEstimatedLineCount()
        {
            if (LineCount > 0) return LineCount;

            if (SourceRange?.IsValid == true)
            {
                LineCount = SourceRange.GetLineCount();
                return LineCount;
            }

            return 0;
        }

        public bool IsComplex()
        {
            return CyclomaticComplexity > 10 || GetEstimatedLineCount() > 50;
        }

        public bool IsSimple()
        {
            return CyclomaticComplexity <= 3 && GetEstimatedLineCount() <= 10;
        }

        public List<Parameter> GetInputParameters()
        {
            return Parameters.Where(p => ParameterAnalyzer.IsLikelyInputParameter(p)).ToList();
        }

        public List<Parameter> GetOutputParameters()
        {
            return Parameters.Where(p => ParameterAnalyzer.IsLikelyOutputParameter(p)).ToList();
        }

        public List<Parameter> GetStringParameters()
        {
            return Parameters.Where(p => ParameterAnalyzer.IsLikelyStringParameter(p)).ToList();
        }

        public override string GetSignature()
        {
            var sb = new StringBuilder();

            // Storage class and specifiers
            if (StorageClass != StorageClass.Auto)
                sb.Append(StorageClass.ToString().ToLower() + " ");

            if (Specifier != FunctionSpecifier.None)
                sb.Append(Specifier.ToString().ToLower() + " ");

            // Return type
            if (ReturnType != null)
                sb.Append(ReturnType.GetSignature());
            else
                sb.Append("void");

            sb.Append(" " + Name + "(");

            // Parameters
            var paramStrings = Parameters.Select(p => p.GetSignature());
            if (IsVariadic)
                paramStrings = paramStrings.Concat(new[] { "..." });

            sb.Append(string.Join(", ", paramStrings));
            sb.Append(")");

            return sb.ToString();
        }

        public string GetDeclarationSignature()
        {
            return GetSignature() + ";";
        }

        public string GetDefinitionSignature()
        {
            return GetSignature();
        }

        public override void PopulateFromASTNode(ClangASTNode node)
        {
            base.PopulateFromASTNode(node);

            if (node == null) return;

            // Extract function-specific information
            if (node.HasProperty("storageClass"))
            {
                var storageClassStr = node.GetProperty<string>("storageClass");
                if (Enum.TryParse<StorageClass>(storageClassStr, true, out var storageClass))
                    StorageClass = storageClass;

                IsStatic = storageClassStr == "static";
                IsExtern = storageClassStr == "extern";
            }

            if (node.HasProperty("inline"))
                IsInline = node.GetProperty<bool>("inline");

            if (node.HasProperty("variadic"))
                IsVariadic = node.GetProperty<bool>("variadic");

            // Check if it's main function
            IsMain = Name == "main";

            // Parse return type
            if (!string.IsNullOrEmpty(node.QualType))
            {
                // Function type format: "returnType (paramTypes)"
                var funcType = node.QualType;
                var parenIndex = funcType.IndexOf('(');
                if (parenIndex > 0)
                {
                    var returnTypeStr = funcType.Substring(0, parenIndex).Trim();
                    ReturnType = DataType.Parse(returnTypeStr);
                }
            }

            // Extract parameters from child nodes
            var paramNodes = node.Children.Where(c => c.Kind == ClangASTNodeKind.ParmVarDecl);
            foreach (var paramNode in paramNodes)
            {
                var param = new Parameter();
                param.PopulateFromASTNode(paramNode);
                AddParameter(param);
            }

            // Extract function body if present
            var bodyNode = node.Children.FirstOrDefault(c => c.Kind == ClangASTNodeKind.CompoundStmt);
            if (bodyNode != null)
            {
                IsPrototype = false;
                AnalyzeBody(bodyNode);
            }
            else
            {
                IsPrototype = true;
            }

            // Calculate estimated line count
            GetEstimatedLineCount();
        }

        private void AnalyzeBody(ClangASTNode bodyNode)
        {
            if (bodyNode == null) return;

            // Extract local variables
            var declNodes = bodyNode.FindByKind(ClangASTNodeKind.VarDecl);
            foreach (var declNode in declNodes)
            {
                var variable = new Variable();
                variable.PopulateFromASTNode(declNode);
                AddLocalVariable(variable);
            }

            // Extract function calls
            var callNodes = bodyNode.FindByKind(ClangASTNodeKind.CallExpr);
            foreach (var callNode in callNodes)
            {
                if (callNode.HasProperty("callee"))
                {
                    var calleeName = callNode.GetProperty<string>("callee");
                    if (!string.IsNullOrEmpty(calleeName))
                        AddFunctionCallByName(calleeName);
                }
            }

            // Find return statements
            var returnNodes = bodyNode.FindByKind(ClangASTNodeKind.ReturnStmt);
            foreach (var returnNode in returnNodes)
            {
                HasReturnStatement = true;
                if (returnNode.Location?.Begin != null)
                    ReturnStatements.Add(returnNode.Location.Begin);
            }

            // Calculate cyclomatic complexity (simplified)
            CalculateCyclomaticComplexity(bodyNode);
        }

        private void CalculateCyclomaticComplexity(ClangASTNode bodyNode)
        {
            if (bodyNode == null) return;

            int complexity = 1; // Base complexity

            // Count decision points
            var decisionNodes = new[]
            {
                ClangASTNodeKind.IfStmt,
                ClangASTNodeKind.WhileStmt,
                ClangASTNodeKind.ForStmt,
                ClangASTNodeKind.SwitchStmt,
                ClangASTNodeKind.CaseStmt,
                ClangASTNodeKind.ConditionalOperator
            };

            foreach (var nodeKind in decisionNodes)
            {
                var nodes = bodyNode.FindByKind(nodeKind);
                complexity += nodes.Count;
            }

            CyclomaticComplexity = complexity;
        }

        public override string ToString()
        {
            var typeStr = IsPrototype ? "declaration" : "definition";
            var accessStr = IsStatic ? "static" : "public";
            return $"{accessStr} function {typeStr}: {Name}({ParameterCount} params)";
        }

        public override bool Equals(object obj)
        {
            if (obj is Function other)
            {
                return Name == other.Name &&
                       ReturnType?.Equals(other.ReturnType) == true &&
                       Parameters.Count == other.Parameters.Count &&
                       Parameters.SequenceEqual(other.Parameters);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, ReturnType, Parameters.Count);
        }

        // Static factory methods
        public static Function CreatePrototype(string name, DataType returnType, params Parameter[] parameters)
        {
            var function = new Function(name, returnType)
            {
                IsPrototype = true
            };

            foreach (var param in parameters)
                function.AddParameter(param);

            return function;
        }

        public static Function CreateMain()
        {
            var intType = new DataType("int");
            var function = new Function("main", intType)
            {
                IsMain = true
            };

            // Standard main signatures: main() or main(int argc, char* argv[])
            var intParam = new Parameter("argc", intType);
            var charPtrType = new DataType("char") { IsPointer = true, PointerLevel = 1, IsArray = true };
            var argvParam = new Parameter("argv", charPtrType);

            function.AddParameter(intParam);
            function.AddParameter(argvParam);

            return function;
        }
    }
}
