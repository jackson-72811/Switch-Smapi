namespace SwitchSMAPI.Framework.Logging {

    /// <summary>Severity levels for mod log messages.</summary>
    public enum LogLevel {
        /// <summary>Extremely verbose trace output.  Not shown by default.</summary>
        Trace = 0,

        /// <summary>Detailed debug information useful during mod development.</summary>
        Debug = 1,

        /// <summary>Normal informational messages.</summary>
        Info = 2,

        /// <summary>Warnings about non-critical issues the user should know about.</summary>
        Warn = 3,

        /// <summary>Errors that caused a feature to fail but did not crash the mod.</summary>
        Error = 4,

        /// <summary>Critical alerts that require immediate attention.</summary>
        Alert = 5,
    }
}
