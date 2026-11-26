namespace TargetPlatform.Models
{
    /// <summary>
    /// 表示输入参数校验或解析过程中发现的无效输入项。
    /// </summary>
    public sealed class TargetInputError
    {
        /// <summary>
        /// 初始化无效输入项描述。
        /// </summary>
        /// <param name="inputArgument">原始输入参数文本。</param>
        /// <param name="errorMessage">具体的校验失败说明。</param>
        public TargetInputError(string inputArgument, string errorMessage)
        {
            InputArgument = inputArgument;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// 获取原始输入参数文本。
        /// </summary>
        public string InputArgument { get; }

        /// <summary>
        /// 获取具体的错误描述。
        /// </summary>
        public string ErrorMessage { get; }
    }
}
