using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Models.CodeAnalysis.BranchAnalysis;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Parser
{
    /// <summary>
    /// Service for analyzing branches in code
    /// </summary>
    public class BranchAnalysisService : IBranchAnalysisService
    {
        private readonly IParserService _parserService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parserService">The parser service</param>
        public BranchAnalysisService(IParserService parserService)
        {
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
        }

        /// <summary>
        /// Analyzes branches in the given function
        /// </summary>
        public async Task<BranchAnalysisResult> AnalyzeBranchesAsync(string functionName, SourceFile sourceFile)
        {
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentNullException(nameof(functionName));

            // Get function analysis
            var functionAnalysis = await _parserService.AnalyzeFunctionParserAsync(functionName, sourceFile);
            if (functionAnalysis == null)
                throw new InvalidOperationException($"Function not found: {functionName}");

            // Create branch analysis result
            var result = new BranchAnalysisResult
            {
                FunctionName = functionName,
                Branches = new List<CFunctionBranch>(),
                Paths = new List<CFunctionPath>()
            };

            // Extract branches
            foreach (var branch in functionAnalysis.Branches)
            {
                var functionBranch = new CFunctionBranch
                {
                    Id = branch.LineNumber, // Use line number as ID for simplicity
                    LineNumber = branch.LineNumber,
                    Condition = branch.Condition,
                    TrueBranchTarget = branch.TrueBlockId,
                    FalseBranchTarget = branch.FalseBlockId,
                    IsFeasible = true // Assume all branches are feasible by default
                };

                result.Branches.Add(functionBranch);
            }

            // Extract paths
            int pathId = 0;
            foreach (var path in functionAnalysis.Paths)
            {
                var functionPath = new CFunctionPath
                {
                    Id = pathId++,
                    PathCondition = path.PathCondition,
                    IsFeasible = path.IsExecutable
                };

                // Extract branch IDs from the path
                foreach (var blockId in path.BlockSequence)
                {
                    // Find branches related to this block
                    var branchesInBlock = functionAnalysis.Branches
                        .Where(b => b.TrueBlockId == blockId || b.FalseBlockId == blockId)
                        .Select(b => b.LineNumber);

                    functionPath.Branches.AddRange(branchesInBlock);
                }

                result.Paths.Add(functionPath);
            }

            // Calculate statistics
            result.TotalBranches = result.Branches.Count;
            result.FeasibleBranches = result.Branches.Count(b => b.IsFeasible);
            result.InfeasibleBranches = result.TotalBranches - result.FeasibleBranches;

            return result;
        }

        /// <summary>
        /// Finds paths through the function that cover the given branches
        /// </summary>
        public async Task<List<CFunctionPath>> FindPathsCoveringBranchesAsync(string functionName, SourceFile sourceFile, IEnumerable<int> branchIds)
        {
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentNullException(nameof(functionName));
            if (branchIds == null)
                throw new ArgumentNullException(nameof(branchIds));

            // Get branch analysis
            var branchAnalysis = await AnalyzeBranchesAsync(functionName, sourceFile);

            // Find paths that cover the branches
            var branchIdSet = new HashSet<int>(branchIds);
            var coveringPaths = new List<CFunctionPath>();

            foreach (var path in branchAnalysis.Paths)
            {
                // Check if this path covers any of the requested branches
                if (path.Branches.Any(branchId => branchIdSet.Contains(branchId)))
                {
                    coveringPaths.Add(path);
                }
            }

            return coveringPaths;
        }

        /// <summary>
        /// Determines if a branch is feasible (can be executed)
        /// </summary>
        public async Task<bool> IsBranchFeasibleAsync(string functionName, SourceFile sourceFile, int branchId)
        {
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentNullException(nameof(functionName));

            // Get branch analysis
            var branchAnalysis = await AnalyzeBranchesAsync(functionName, sourceFile);

            // Find the branch
            var branch = branchAnalysis.Branches.FirstOrDefault(b => b.Id == branchId);
            if (branch == null)
                throw new ArgumentException($"Branch not found: {branchId}", nameof(branchId));

            return branch.IsFeasible;
        }
    }
}
