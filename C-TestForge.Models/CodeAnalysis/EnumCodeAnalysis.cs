using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.CodeAnalysis
{
    // Enum cho các loại storage class
    public enum StorageClass
    {
        Auto,
        Register,
        Static,
        Extern,
        Typedef
    }
    // Enum cho các loại qualifier
    public enum TypeQualifier
    {
        None,
        Const,
        Volatile,
        Restrict
    }

    // Enum cho các loại function specifier
    public enum FunctionSpecifier
    {
        None,
        Inline,
        Noreturn
    }

    // Enum cho các loại AST node từ Clang
    public enum ClangASTNodeKind
    {
        TranslationUnitDecl,
        FunctionDecl,
        VarDecl,
        ParmVarDecl,
        RecordDecl,
        FieldDecl,
        EnumDecl,
        EnumConstantDecl,
        TypedefDecl,
        InclusionDirective,
        MacroDefinition,
        MacroExpansion,
        CompoundStmt,
        IfStmt,
        WhileStmt,
        ForStmt,
        SwitchStmt,
        CaseStmt,
        DefaultStmt,
        CallExpr,
        DeclRefExpr,
        BinaryOperator,
        UnaryOperator,
        CStyleCastExpr,
        ImplicitCastExpr,
        StringLiteral,
        IntegerLiteral,
        FloatingLiteral,
        CharacterLiteral,
        ArraySubscriptExpr,
        MemberExpr,
        ConditionalOperator,
        InitListExpr,
        CompoundLiteralExpr,
        GotoStmt,
        LabelStmt,
        ReturnStmt,
        BreakStmt,
        ContinueStmt,
        DoStmt,
        NullStmt,
        DeclStmt
    }

    // Enum cho các loại linkage
    public enum LinkageKind
    {
        None,
        Internal,
        External,
        UniqueExternal,
        Common,
        LocalizedExternal
    }

    public enum ScopeType
    {
        Global,
        FileScope,
        BlockScope,
        FunctionScope,
        FunctionPrototype
    }

    // Enum cho access level
    public enum AccessLevel
    {
        Public,
        Private,
        Protected,
        Internal
    }

}
