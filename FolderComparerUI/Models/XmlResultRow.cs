namespace FolderComparerUI.Models
{
    /// <summary>One row shown in the XML validation results panel.</summary>
    public sealed class XmlResultRow
    {
        public string FileName    { get; init; } = "";
        public string FilePath    { get; init; } = "";
        public string Description { get; init; } = "";
        /// <summary>"Valid" | "Invalid" | "Error"</summary>
        public string Status      { get; init; } = "";
    }
}
