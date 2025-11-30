using System.Net.Http.Headers;

namespace PolicyChatbot.Server.Startup;

public static class AddAnthropicApiClientSetup
{
    public static IServiceCollection AddAnthropicApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Anthropic").Get<AnthropicOptions>()
            ?? throw new InvalidOperationException("Anthropic configuration section is missing or invalid.");

        services.AddHttpClient("Anthropic", client =>
        {
            var baseUrl = options.BaseUrl.TrimEnd('/') + "/";
            client.BaseAddress = new Uri(baseUrl);

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        });

        return services;
    }
}