namespace LibZ.Tool.Tasks
{
    /// <summary>
    /// 
    /// </summary>
    public class InjectDllResult
    {
        /// <inheritdoc />
        public InjectDllResult(bool success, string guidString)
        {
            Success = success;
            Guid = guidString;
        }

        /// <inheritdoc />
        public InjectDllResult(bool success)
        {
            Success = success;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Guid { get; set; }
        
    }
}