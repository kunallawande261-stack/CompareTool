namespace FolderComparerUI.Services
{
    /// <summary>
    /// Finds file names that appear in both column A and column B of an Excel
    /// worksheet and writes the common set to column D.
    /// </summary>
    public interface IExcelFindCommonFilesService
    {
        /// <summary>
        /// Opens <paramref name="filePath"/>, computes the intersection of
        /// column A and column B values (case-insensitive, header row skipped),
        /// writes the result to column D, and returns the count of common files.
        /// </summary>
        int FindCommonFiles(string filePath);
    }
}
