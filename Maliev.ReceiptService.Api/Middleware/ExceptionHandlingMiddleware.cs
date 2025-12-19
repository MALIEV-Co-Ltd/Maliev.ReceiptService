using Maliev.ReceiptService.Api.Exceptions;
using System.Net;
using System.Text.Json;

namespace Maliev.ReceiptService.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        HttpStatusCode statusCode;
        object errorResponse;

        switch (exception)
        {
            case InvoiceNotFoundException ex:
                statusCode = HttpStatusCode.NotFound;
                errorResponse = new { error = "Invoice not found", invoiceId = ex.InvoiceId, message = ex.Message };
                break;
            case DuplicateReceiptException ex:
                statusCode = HttpStatusCode.Conflict;
                errorResponse = new { error = "Duplicate receipt", invoiceId = ex.InvoiceId, message = ex.Message };
                break;
            case OverReceiptingException ex:
                statusCode = HttpStatusCode.BadRequest;
                errorResponse = new
                {
                    error = "Over-receipting detected",
                    invoiceId = ex.InvoiceId,
                    requestedAmount = ex.RequestedAmount,
                    remainingBalance = ex.RemainingBalance,
                    message = ex.Message
                };
                break;
            case TaxValidationException ex:
                statusCode = HttpStatusCode.BadRequest;
                errorResponse = new { error = "Tax validation failed", validationErrors = ex.ValidationErrors, message = ex.Message };
                break;
            case InvalidOperationException ex:
                statusCode = HttpStatusCode.BadRequest;
                errorResponse = new { error = "Invalid operation", message = ex.Message };
                break;
            default:
                statusCode = HttpStatusCode.InternalServerError;
                errorResponse = new { error = "Internal server error", message = "An unexpected error occurred" };
                break;
        }

        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}
