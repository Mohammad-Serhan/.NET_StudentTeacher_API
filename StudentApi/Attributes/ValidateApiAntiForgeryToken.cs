using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace StudentApi.Attributes
{
    /// <summary>
    /// Minimal anti-forgery token validation - just checks if header matches cookie
    /// </summary>
    public class CustomValidateAntiForgeryToken : Attribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Skip safe methods
            var method = context.HttpContext.Request.Method;
            if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) ||
                HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method))
            {
                return;
            }

            var headerToken = context.HttpContext.Request.Headers["x-xsrf-token"].FirstOrDefault();
            var cookieToken = context.HttpContext.Request.Cookies["XSRF-TOKEN"];

            // ✅ ADD DEBUG LOGGING
            Console.WriteLine($"=== CSRF DEBUG ===");
            Console.WriteLine($"Method: {method}");
            Console.WriteLine($"URL: {context.HttpContext.Request.Path}");
            Console.WriteLine($"Header Token: {headerToken}");
            Console.WriteLine($"Cookie Token: {cookieToken}");
            Console.WriteLine($"Tokens Match: {headerToken == cookieToken}");
            Console.WriteLine($"Header Present: {!string.IsNullOrEmpty(headerToken)}");
            Console.WriteLine($"Cookie Present: {!string.IsNullOrEmpty(cookieToken)}");


            // Log all headers for debugging
            foreach (var header in context.HttpContext.Request.Headers)
            {
                Console.WriteLine($"Header: {header.Key} = {header.Value}");
            }



            // Basic validation
            if (string.IsNullOrEmpty(headerToken) || headerToken != cookieToken)
            {
                Console.WriteLine($"❌ CSRF VALIDATION FAILED");
                context.Result = new BadRequestObjectResult(new
                {
                    message = "Invalid or missing anti-forgery token"
                });
            }
            else
            {
                Console.WriteLine($"✅ CSRF VALIDATION PASSED");
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed
        }
    }

    public class IgnoreApiAntiForgeryTokenAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context) { }
        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}