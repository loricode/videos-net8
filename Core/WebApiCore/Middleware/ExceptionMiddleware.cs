using System.Net;
using System.Text.Json;
using DomainCore.Exceptions;
using FluentValidation;

namespace WebApiCore.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
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
                await HandleException(
                       context,
                       StatusCodes.Status400BadRequest,
                       ex.Message);
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

        private static async Task HandleGenericException(HttpContext context, Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var result = JsonSerializer.Serialize(new
            {
                error = "Error interno del servidor" + ex.Message
            });

            await context.Response.WriteAsync(result);
        }

    }
}
