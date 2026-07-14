namespace DBmcp.Services.Models;

public class MCPAnswer<T>(T? answer = default, bool answerSuccess = true, string? answerErrorMessage = null)
{
    public T? Answer { get; set; } = answer;
    public bool AnswerSuccess { get; set; } = answerSuccess;
    public string? AnswerErrorMessage { get; set; } = answerErrorMessage;

    public static MCPAnswer<T> CreateSuccessAnswer(T answer)
    {
        return new MCPAnswer<T>(answer);
    }

    public static MCPAnswer<T> CreateErrorAnswer(string errorMessage)
    {
        return new MCPAnswer<T>(default, false, errorMessage);
    }
}