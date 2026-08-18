using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;

namespace Behsazan.Presentation.Extensions;

public static class GlobalExceptionHandler
{
    public static void UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(handler =>
        {
            handler.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var webHostEnv = context.RequestServices.GetRequiredService<IWebHostEnvironment>();

                if (exceptionFeature is null)
                    return;

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var problem = new
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "An unexpected error occurred.",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = webHostEnv.IsDevelopment()
                        ? exceptionFeature.Error.Message
                        : "An unexpected error occurred. Please try again later."
                };

                await context.Response.WriteAsJsonAsync(problem);
            });
        });
    }
}
