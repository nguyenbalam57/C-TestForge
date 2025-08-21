using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Projects
{
    /// <summary>
    /// Đồ thị phụ thuộc giữa các tệp nguồn trong dự án C/C++
    /// </summary>
    public class DependencyGraph
    {
        /// <summary>
        /// Danh sách các tệp nguồn trong dự án (node của đồ thị)
        /// </summary>
        public List<SourceFileDependency> SourceFiles { get; set; } = new();

        /// <summary>
        /// Tổng số tệp nguồn trong đồ thị
        /// </summary>
        public int FileCount => SourceFiles.Count;

        /// <summary>
        /// Lấy tệp nguồn theo tên file
        /// </summary>
        public SourceFileDependency? GetFile(string fileName)
        {
            return SourceFiles.FirstOrDefault(f =>
                string.Equals(f.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Lấy tất cả các tệp mà file này phụ thuộc (direct include)
        /// </summary>
        public List<SourceFileDependency> GetDirectDependencies(string fileName)
        {
            var file = GetFile(fileName);
            if (file == null) return new List<SourceFileDependency>();
            return file.DirectDependencies
                .Select(depName => GetFile(depName))
                .Where(f => f != null)
                .ToList()!;
        }

        /// <summary>
        /// Lấy tất cả các tệp phụ thuộc ngược (các tệp include file này)
        /// </summary>
        public List<SourceFileDependency> GetDependents(string fileName)
        {
            return SourceFiles
                .Where(f => f.DirectDependencies.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Kiểm tra có chu trình phụ thuộc không (circular dependency)
        /// </summary>
        public bool HasCircularDependency()
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in SourceFiles)
            {
                if (HasCycle(file, visited, stack))
                    return true;
            }
            return false;
        }

        private bool HasCycle(SourceFileDependency file, HashSet<string> visited, HashSet<string> stack)
        {
            if (!visited.Add(file.FileName))
                return false;
            stack.Add(file.FileName);

            foreach (var dep in file.DirectDependencies)
            {
                var depFile = GetFile(dep);
                if (depFile == null) continue;
                if (stack.Contains(depFile.FileName) || HasCycle(depFile, visited, stack))
                    return true;
            }
            stack.Remove(file.FileName);
            return false;
        }

        /// <summary>
        /// Thống kê số lượng tệp theo loại (header/source)
        /// </summary>
        public (int headerCount, int sourceCount) GetFileTypeStatistics()
        {
            int header = SourceFiles.Count(f => f.FileType == SourceFileType.CHeader || f.FileType == SourceFileType.CPPHeader);
            int source = SourceFiles.Count(f => f.FileType == SourceFileType.CSource || f.FileType == SourceFileType.CPPSource);
            return (header, source);
        }
    }
}
