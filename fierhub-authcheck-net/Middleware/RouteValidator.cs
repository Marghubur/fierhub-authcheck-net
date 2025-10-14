using Bt.Ems.Lib.PipelineConfig.Model.ExceptionModel;
using fierhub_authcheck_net.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace fierhub_authcheck_net.Middleware
{
    public class RouteValidator
    {
        // private readonly List<string> _routes = new List<string>();

        public async Task TestRoute(RequestDelegate next, HttpContext context)
        {
            if (await Task.FromResult(new[] { "Authenticate" }.Any(x => context.Request.Path.ToString().Contains(x))))
            {
                await next(context);
            }
        }

        public async Task TestConnection(CurrentSession currentSession)
        {
            if (string.IsNullOrEmpty(currentSession.LocalConnectionString))
            {
                throw new EmstumException("Unable to find database detail. Please contact to admin.");
            }

            await Task.CompletedTask;
        }

        public async Task TestAnonymous(RequestDelegate next, HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                await next(context);
            }
        }
    }
}
