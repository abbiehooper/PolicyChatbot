using PolicyChatbot.Server.Services;

namespace PolicyChatbot.Server.Startup;

public static class DependencyInjection
{
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPolicyRepository, PolicyRepository>();
        return services;
    }
}
