namespace AzureAIStreamer.Console;

public class AppConfig
{
    public required string Endpoint { get; set; }
    public required string ApiKey { get; set; }
    public required string DeploymentName { get; set; }
    public required string SystemPrompt { get; set; }
    
    public bool IsValid => !string.IsNullOrWhiteSpace(Endpoint) &&
                           !string.IsNullOrWhiteSpace(ApiKey) &&
                           !string.IsNullOrWhiteSpace(DeploymentName) &&
                           !string.IsNullOrWhiteSpace(SystemPrompt);
}
