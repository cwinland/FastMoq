using FastMoq.Analyzers;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace FastMoq.Analyzers.Tests
{
    public class AnalyzerDocumentationTests
    {
        [Fact]
        public void MigrationGuideAnalyzerCatalog_ShouldListEveryPublicDiagnosticId()
        {
            var catalogSection = ReadAnalyzerCatalogSection();
            const string AnalyzerCatalogTableRowPattern = @"^\|\s*`(FMOQ\d{4})`\s*\|";
            var documentedIds = Regex.Matches(catalogSection, AnalyzerCatalogTableRowPattern, RegexOptions.Multiline)
                .Select(match => match.Groups[1].Value)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();

            var expectedIds = typeof(DiagnosticIds)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(string) && field.IsLiteral && !field.IsInitOnly)
                .Select(field => (string) field.GetRawConstantValue()!)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expectedIds, documentedIds);
        }

        private static string ReadAnalyzerCatalogSection()
        {
            var readmePath = Path.Combine(AppContext.BaseDirectory, "docs", "migration", "README.md");
            var readme = File.ReadAllText(readmePath);
            const string startMarker = "## Analyzer catalog";
            const string endMarker = "## Migration summary";

            var startIndex = readme.IndexOf(startMarker, StringComparison.Ordinal);
            Assert.True(startIndex >= 0, $"Could not find '{startMarker}' in '{readmePath}'.");

            var endIndex = readme.IndexOf(endMarker, startIndex, StringComparison.Ordinal);
            Assert.True(endIndex > startIndex, $"Could not find '{endMarker}' after '{startMarker}' in '{readmePath}'.");

            return readme.Substring(startIndex, endIndex - startIndex);
        }
    }
}