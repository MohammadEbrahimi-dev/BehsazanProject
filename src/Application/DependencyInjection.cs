using Microsoft.Extensions.DependencyInjection;

namespace Behsazan.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        #region Application services will be registered here
        #endregion

        return services;
    }
}
