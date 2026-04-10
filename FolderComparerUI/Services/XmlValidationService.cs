using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace FolderComparerUI.Services
{
    /// <inheritdoc cref="IXmlValidationService"/>
    public class XmlValidationService : IXmlValidationService
    {
        /// <inheritdoc/>
        public XmlValidationResult ValidateFolder(string folderPath, string logFilePath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("Folder path must not be empty.", nameof(folderPath));

            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

            var errors = new List<XmlFileError>();

            // Ensure the log directory exists
            string? logDir = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(logDir))
                Directory.CreateDirectory(logDir);

            using var log = new StreamWriter(logFilePath, append: false);
            log.WriteLine($"XML Validation — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            log.WriteLine($"Folder: {folderPath}");
            log.WriteLine(new string('-', 60));

            string[] xmlFiles;
            try
            {
                xmlFiles = Directory.GetFiles(folderPath, "*.xml",
                    SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                throw new IOException($"Could not enumerate files in '{folderPath}': {ex.Message}", ex);
            }

            if (xmlFiles.Length == 0)
            {
                log.WriteLine("No XML files found.");
                return new XmlValidationResult { TotalScanned = 0, Errors = errors };
            }

            log.WriteLine($"Files found: {xmlFiles.Length}");
            log.WriteLine();

            foreach (string path in xmlFiles)
            {
                var error = ValidateSingleFile(path);
                if (error != null)
                {
                    errors.Add(error);
                    log.WriteLine($"INVALID  {error.FileName}");
                    log.WriteLine($"         {error.Description}");
                    log.WriteLine(new string('-', 60));
                }
                else
                {
                    log.WriteLine($"OK       {Path.GetFileName(path)}");
                }
            }

            log.WriteLine();
            if (errors.Count == 0)
                log.WriteLine("Result: All XML files are valid.");
            else
                log.WriteLine($"Result: {errors.Count} invalid file(s) out of {xmlFiles.Length}.");

            return new XmlValidationResult { TotalScanned = xmlFiles.Length, Errors = errors };
        }

        // ── private helpers ────────────────────────────────────────────────────

        private static XmlFileError? ValidateSingleFile(string filePath)
        {
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing    = DtdProcessing.Ignore,
                    ValidationType   = ValidationType.None,
                    IgnoreWhitespace = false,
                    IgnoreComments   = false
                };

                using var reader = XmlReader.Create(filePath, settings);
                while (reader.Read()) { }
                return null; // valid
            }
            catch (XmlException ex)
            {
                return new XmlFileError
                {
                    FilePath    = filePath,
                    FileName    = Path.GetFileName(filePath),
                    Description = $"Line {ex.LineNumber}, Col {ex.LinePosition} — {ex.Message}"
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                return new XmlFileError
                {
                    FilePath    = filePath,
                    FileName    = Path.GetFileName(filePath),
                    Description = $"Access denied: {ex.Message}"
                };
            }
            catch (IOException ex)
            {
                return new XmlFileError
                {
                    FilePath    = filePath,
                    FileName    = Path.GetFileName(filePath),
                    Description = $"I/O error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new XmlFileError
                {
                    FilePath    = filePath,
                    FileName    = Path.GetFileName(filePath),
                    Description = $"Unexpected error: {ex.Message}"
                };
            }
        }
    }
}
