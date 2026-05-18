namespace TenantPortal.Auth.DTOs
{
    public record TestResult
    {
        public string Name { get; init; } = "";
        public string Category { get; init; } = "";
        public bool Passed { get; init; }
        public string Message { get; init; } = "";
        public List<string> Logs { get; init; } = [];
        public int DurationMs { get; init; }
    }

    public record TestSuiteResult
    {
        public DateTime RunAt { get; init; }
        public int TotalDurationMs { get; init; }
        public int Passed { get; init; }
        public int Failed { get; init; }
        public List<TestResult> Tests { get; init; } = [];
    }
}
