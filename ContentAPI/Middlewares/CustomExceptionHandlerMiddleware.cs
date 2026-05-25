using ContentAPI.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ContentAPI.Middlewares
{
    public class CustomExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CustomExceptionHandlerMiddleware> _logger;

        public CustomExceptionHandlerMiddleware(RequestDelegate next, ILogger<CustomExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred during request execution on path: {Path}", context.Request.Path);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var problemDetails = new ProblemDetails
            {
                Instance = context.Request.Path,
                Extensions = { ["traceId"] = context.TraceIdentifier }
            };

            switch (exception)
            {
                case NotFoundException ex:
                    problemDetails.Status = StatusCodes.Status404NotFound;
                    problemDetails.Title = "Resource Not Found";
                    problemDetails.Detail = ex.Message;
                    break;

                case HttpRequestException ex when ex.StatusCode == HttpStatusCode.Unauthorized || ex.StatusCode == HttpStatusCode.Forbidden:
                    problemDetails.Status = (int?)ex.StatusCode;
                    problemDetails.Title = "AI Service Authentication Failed";
                    problemDetails.Detail = "The upstream server rejected the authorization credentials. Please verify server keys configuration.";
                    break;

                case HttpRequestException ex when ex.StatusCode == (HttpStatusCode)429:
                    problemDetails.Status = StatusCodes.Status429TooManyRequests;
                    problemDetails.Title = "AI Service Rate Limit Exceeded";
                    problemDetails.Detail = "Too many requests are being sent to the AI backend. Please retry the request later.";
                    break;

                case TaskCanceledException:
                case TimeoutException:
                    problemDetails.Status = StatusCodes.Status504GatewayTimeout;
                    problemDetails.Title = "AI Service Timeout";
                    problemDetails.Detail = "The request to the external AI engine timed out. The server took too long to respond.";
                    break;

                case HttpRequestException ex when ex.StatusCode >= HttpStatusCode.InternalServerError:
                    problemDetails.Status = StatusCodes.Status502BadGateway;
                    problemDetails.Title = "AI Service Unavailable";
                    problemDetails.Detail = "The upstream AI server encountered an error or is temporarily offline.";
                    break;

                default:
                    problemDetails.Status = StatusCodes.Status500InternalServerError;
                    problemDetails.Title = "Internal Server Error";
                    problemDetails.Detail = "An unexpected error occurred. Please try again later.";
                    break;
            }

            context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}