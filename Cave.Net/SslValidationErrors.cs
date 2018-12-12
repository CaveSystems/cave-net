using System;

namespace Cave.Net
{
    /// <summary>
    /// Provides ssl validation errors
    /// </summary>
    [Flags]
    public enum SslValidationErrors
    {
        /// <summary>no validation errors</summary>
        None = 0,

        /// <summary>Certificate is not jet valid</summary>
        NotJetValid = 1,

        /// <summary>Certificate is no longer valid</summary>
        NoLongerValid = 2,
    }
}
