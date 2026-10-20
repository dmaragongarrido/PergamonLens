using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PergamonLens.Application.Common.Behaviors;

namespace PergamonLens.Application;

/// <summary>
/// Métodos de extensión para configurar la inyección de dependencias de la capa de Aplicación.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

