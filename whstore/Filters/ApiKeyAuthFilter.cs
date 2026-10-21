using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace whstore.Filters
{
    public class ApiKeyAuthFilter : IAsyncActionFilter
    {
        private readonly IConfiguration _configuration;
        public ApiKeyAuthFilter(IConfiguration configuration) => _configuration = configuration;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var adminKey = _configuration["AdminApiKey"];
            if (string.IsNullOrEmpty(adminKey))
            {
                await next();
                return;
            }

            var provided = context.HttpContext.Request.Headers["X-ADMIN-KEY"].FirstOrDefault()
                           ?? context.HttpContext.Request.Query["adminKey"].FirstOrDefault();

            if (string.IsNullOrEmpty(provided) || provided != adminKey)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            await next();
        }
    }
}