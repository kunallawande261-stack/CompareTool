using System.Collections.Generic;

namespace FolderComparerUI.Services
{
    /// <summary>
    /// Validates all XML files in a folder tree.
    /// Returns a <see cref="XmlValidationResult"/> containing error details
    /// and the total number of files scanned.
    /// </summary>
    public interface IXmlValidationService
    {
        /// <summary>
        /// Scans <paramref name="folderPath"/> recursively for *.xml files and
        /// validates each one. Returns a result object with the error list and
        /// total scanned count.
        /// </summary>
        XmlValidationResult ValidateFolder(string folderPath, string logFilePath);
    }

    /// <summary>Result returned by <see cref="IXmlValidationService.ValidateFolder"/>.</summary>
    public sealed class XmlValidationResult
    {
        public int TotalScanned              { get; init; }
        public IReadOnlyList<XmlFileError> Errors { get; init; } = new List<XmlFileError>();
    }

    /// <summary>One validation error — the file that failed plus a human-readable description.</summary>
    public sealed class XmlFileError
    {
        public string FilePath    { get; init; } = "";
        public string FileName    { get; init; } = "";
        public string Description { get; init; } = "";

        public override string ToString() => $"{FileName}: {Description}";
    }
}
