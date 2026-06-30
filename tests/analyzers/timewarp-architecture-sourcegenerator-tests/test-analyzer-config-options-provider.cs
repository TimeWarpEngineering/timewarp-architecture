namespace TimeWarp.Architecture.SourceGenerator.Tests;

internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly TestAnalyzerConfigOptions _globalOptions;

    public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> options)
    {
        _globalOptions = new TestAnalyzerConfigOptions(options);
    }

    public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _globalOptions;
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _globalOptions;
}

internal sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> _options;

    public TestAnalyzerConfigOptions(Dictionary<string, string> options)
    {
        _options = options;
    }

    public override bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value)
    {
        if (_options.TryGetValue(key, out string? existing))
        {
            value = existing;
            return true;
        }

        value = null;
        return false;
    }
}
