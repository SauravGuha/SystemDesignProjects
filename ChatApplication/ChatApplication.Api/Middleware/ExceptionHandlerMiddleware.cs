
namespace ChatApplication.Api.Middleware;

public class ExceptionHandlerMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (KeyNotFoundException ex)
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = "text/xml";
            await context.Response.WriteAsJsonAsync(ex.ToString());
        }
        catch (ArgumentException ex)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "text/xml";
            await context.Response.WriteAsJsonAsync(ex.ToString());
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/xml";
            await context.Response.WriteAsJsonAsync(ex.ToString());
        }
    }
}