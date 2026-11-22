using System.Net;
using System.Text.Json;
using DomainCore.Exceptions;
using FluentValidation;

namespace WebApiCore.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly IHostEnvironment _env;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, IHostEnvironment env, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _env = env;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try { 

                  await _next(context);
                  
            }

            catch (IdentityValidationException ex)
            {
                await HandleException(
                      context,
                      StatusCodes.Status400BadRequest,
                      "Error de validación de usuario",
                      ex.Errors);

            }
            catch (ConflictException ex)
            {
                await HandleException(
                      context,
                      StatusCodes.Status409Conflict,
                      ex.Message);

            }
            catch (NotFoundException ex)
            {
                await HandleException(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (ForbiddenException ex)
            {
                await HandleException(context, StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                await HandleException(context, StatusCodes.Status401Unauthorized, ex.Message);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // el cliente cerró la conexión (p. ej. canceló una subida)
            }
            catch (BadHttpRequestException ex)
            {
                await HandleException(context, ex.StatusCode, ex.StatusCode == 413
                    ? "El archivo supera el tamaño permitido." : "Solicitud inválida.");
            }
            catch (ValidationException ex)
            {
                await HandleValidationException(context, ex);
            }
            catch (Exception ex)
            {
                await HandleGenericException(context, ex);
            }
        }

        private static async Task HandleValidationException(HttpContext context, ValidationException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var errors = ex.Errors
                .Select(e => e.ErrorMessage)
                .ToList();

            var result = JsonSerializer.Serialize(new
            {
                status = 400,
                message = errors.FirstOrDefault() ?? "Datos inválidos.",
                errors
            });

            await context.Response.WriteAsync(result);
        }

        private static async Task HandleException(
            HttpContext context,
            int statusCode,
            string message,
            object? errors = null)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = statusCode,
                message,
                errors
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private async Task HandleGenericException(HttpContext context, Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en {Path}", context.Request.Path);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            // En producción no se exponen detalles internos
            var message = _env.IsDevelopment() ? $"Error interno del servidor: {ex.Message}" : "Error interno del servidor.";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { status = 500, message }));
        }

    }
}
