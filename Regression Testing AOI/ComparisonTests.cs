using NUnit.Framework;
using System.Text.Json;
using System.Text;

public class TestConfiguration
{
    public List<FileComparisonConfig> FileComparisons { get; set; }
}

public class FileComparisonConfig
{
    public string ExpectedFilePath { get; set; }
    public string ActualFilePath { get; set; }
}

[TestFixture]
public class ComparisonTests
{
    private TestConfiguration _config;
    private StringBuilder _reportBuilder;
    private string _reportPath = "TestReport.txt";

    [OneTimeSetUp]
    public void Setup()
    {
        string jsonConfig = File.ReadAllText("TestConfig.json");
        _config = JsonSerializer.Deserialize<TestConfiguration>(jsonConfig);
        _reportBuilder = new StringBuilder();
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        File.WriteAllText(_reportPath, _reportBuilder.ToString());
    }

    [Test]
    public void TestFileComparisons()
    {
        _reportBuilder.AppendLine("=== 文件比較測試報告 ===\n");
        
        foreach (var comparison in _config.FileComparisons)
        {
            _reportBuilder.AppendLine($"比較文件: {Path.GetFileName(comparison.ActualFilePath)}");
            
            bool expectedExists = File.Exists(comparison.ExpectedFilePath);
            bool actualExists = File.Exists(comparison.ActualFilePath);

            if (!expectedExists)
            {
                _reportBuilder.AppendLine($"錯誤: 預期文件不存在: {comparison.ExpectedFilePath}");
                Assert.Fail($"Expected file does not exist: {comparison.ExpectedFilePath}");
            }

            if (!actualExists)
            {
                _reportBuilder.AppendLine($"錯誤: 實際文件不存在: {comparison.ActualFilePath}");
                Assert.Fail($"Actual file does not exist: {comparison.ActualFilePath}");
            }

            if (expectedExists && actualExists)
            {
                string[] expectedLines = File.ReadAllLines(comparison.ExpectedFilePath);
                string[] actualLines = File.ReadAllLines(comparison.ActualFilePath);

                var differences = new List<(int LineNumber, string Expected, string Actual)>();

                // 使用最長的行數來比較
                int maxLines = Math.Max(expectedLines.Length, actualLines.Length);
                
                for (int i = 0; i < maxLines; i++)
                {
                    string expectedLine = i < expectedLines.Length ? expectedLines[i] : null;
                    string actualLine = i < actualLines.Length ? actualLines[i] : null;

                    if (expectedLine != actualLine)
                    {
                        differences.Add((i + 1, expectedLine, actualLine));
                    }
                }

                if (differences.Count == 0)
                {
                    _reportBuilder.AppendLine("結果: 文件內容完全相同\n");
                }
                else
                {
                    _reportBuilder.AppendLine($"結果: 發現 {differences.Count} 處差異");
                    
                    if (differences.Count > 10)
                    {
                        _reportBuilder.AppendLine("注意: 差異過多，僅顯示前10處差異\n");
                        differences = differences.Take(10).ToList();
                    }

                    _reportBuilder.AppendLine("差異詳情:");
                    foreach (var diff in differences)
                    {
                        _reportBuilder.AppendLine($"\n第 {diff.LineNumber} 行:");
                        _reportBuilder.AppendLine($"  預期內容: {(diff.Expected ?? "<空行>")}");
                        _reportBuilder.AppendLine($"  實際內容: {(diff.Actual ?? "<空行>")}");
                    }
                    _reportBuilder.AppendLine();

                    Assert.Fail($"File contents do not match for {comparison.ActualFilePath}");
                }
            }
        }
    }
} 