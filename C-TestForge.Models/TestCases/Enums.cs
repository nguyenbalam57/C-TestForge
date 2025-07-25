namespace C_TestForge.Models.TestCases
{
    /// <summary>
    /// Represents the type of a test case
    /// </summary>
    public enum TestCaseType 
    {
        /// <summary>
        /// Test for a single function in isolation
        /// </summary>
        UnitTest,

        /// <summary>
        /// Test for multiple functions working together
        /// </summary>
        IntegrationTest,

        /// <summary>
        /// Test to verify a previously fixed bug doesn't recur
        /// </summary>
        RegressionTest,

        /// <summary>
        /// Test focused on performance
        /// </summary>
        PerformanceTest,

        /// <summary>
        /// Test focused on security aspects
        /// </summary>
        SecurityTest
    }
    /// <summary>
    /// Represents the status of a test case
    /// </summary>
    public enum TestCaseStatus
    {
        /// <summary>
        /// Test case has not been run
        /// </summary>
        NotRun,

        /// <summary>
        /// Test case passed successfully
        /// </summary>
        Passed,

        /// <summary>
        /// Test case failed
        /// </summary>
        Failed,

        /// <summary>
        /// Test case execution resulted in an error
        /// </summary>
        Error,

        /// <summary>
        /// Test case was skipped
        /// </summary>
        Skipped
    }

    public enum StubParameterAction
    {
        SetValue,
        CopyBuffer,
        CallCallback
    }

    /// <summary>
    /// Specifies the storage class of a variable in C
    /// </summary>
    public enum VariableStorageClass
    {
        /// <summary>
        /// Default storage class for local variables
        /// </summary>
        Auto,

        /// <summary>
        /// Variables that have a static lifetime but limited scope
        /// </summary>
        Static,

        /// <summary>
        /// Variables stored in CPU registers for faster access
        /// </summary>
        Register,

        /// <summary>
        /// Variables declared but defined elsewhere
        /// </summary>
        Extern,

        /// <summary>
        /// Indicates the variable is declared inside a typedef
        /// </summary>
        Typedef,
        Primitive,
        Array,
        Pointer,
        Struct,
        Union,
        Enum,

        None
    }

    /// <summary>
    /// Specifies the scope of a variable in C
    /// </summary>
    public enum VariableScope
    {
        /// <summary>
        /// Variable accessible only within a block
        /// </summary>
        Local,

        /// <summary>
        /// Variable accessible throughout the Static
        /// </summary>
        Static,

        /// <summary>
        /// Variable accessible across multiple files
        /// </summary>
        Global,

        /// <summary>
        /// Variable is a function parameter
        /// </summary>
        Parameter,

        Rom
    }

    /// <summary>
    /// Specifies the type of a preprocessor directive
    /// </summary>
    public enum PreprocessorType
    {
        /// <summary>
        /// Unknown or unsupported directive
        /// </summary>
        Unknown,
        /// <summary>
        /// #include directive
        /// </summary>
        Include,

        /// <summary>
        /// #define directive
        /// </summary>
        Define,

        /// <summary>
        /// #undef directive
        /// </summary>
        Undef,

        /// <summary>
        /// #if directive
        /// </summary>
        If,

        /// <summary>
        /// #ifdef directive
        /// </summary>
        IfDef,

        /// <summary>
        /// #ifndef directive
        /// </summary>
        IfNDef,

        /// <summary>
        /// #else directive
        /// </summary>
        Else,

        /// <summary>
        /// #elif directive
        /// </summary>
        ElIf,

        /// <summary>
        /// #endif directive
        /// </summary>
        EndIf,

        /// <summary>
        /// #pragma directive
        /// </summary>
        Pragma,

        /// <summary>
        /// #error directive
        /// </summary>
        Error,

        /// <summary>
        /// #warning directive
        /// </summary>
        Warning,

        /// <summary>
        /// #line directive - controls line numbers in diagnostics
        /// </summary>
        Line,
        /// <summary>
        /// Other directives
        /// </summary>
        Other
    }
    /// <summary>
    /// Specifies the storage class of a function in C
    /// </summary>
    public enum FunctionStorageClass
    {
        /// <summary>
        /// No specific storage class
        /// </summary>
        None,

        /// <summary>
        /// Function visible only within the current file
        /// </summary>
        Static,

        /// <summary>
        /// Function declared but defined elsewhere
        /// </summary>
        Extern,
        Inline,
    }

    /// <summary>
    /// Specifies the direction of a parameter
    /// </summary>
    public enum ParameterDirection
    {
        None,
        /// <summary>
        /// Parameter is used for input only
        /// </summary>
        Input,

        /// <summary>
        /// Parameter is used for output only
        /// </summary>
        Output,

        /// <summary>
        /// Parameter is used for both input and output
        /// </summary>
        InputOutput
    }

    public enum DefinitionType
    {
        MacroConstant,
        MacroFunction,
        EnumValue
    }

    public enum VariableType
    {
        Primitive,
        Array,
        Pointer,
        Struct,
        Union,
        Enum
    }
}
