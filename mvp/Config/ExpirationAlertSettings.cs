namespace mvp.Config
{
    public class ExpirationAlertSettings
    {
        public bool Enabled { get; set; } = true;
        public bool RunOnStartup { get; set; } = true;
        public double IntervalHours { get; set; } = 24;
        public int DaysAhead { get; set; } = 30;
        public bool AutoResolve { get; set; } = true;
    }
}
