using MentoraX.Api.Common;
using MentoraX.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;
using MentoraX.Domain.Exceptions;

namespace MentoraX.Api.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleException(context, ex);
        }
    }

    private static async Task HandleException(HttpContext context, Exception ex)
    {
        var response = new ErrorResponse();

        switch (ex)
        {
            case AppValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Error.Code = validationException.Code;
                response.Error.Message = validationException.Message;
                response.Error.ValidationErrors = validationException.Errors
                    .Select(x => new ValidationErrorItem
                    {
                        Property = x.Property,
                        Message = x.Message
                    })
                    .ToList();
                break;

            case AppNotFoundException notFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Error.Code = notFoundException.Code;
                response.Error.Message = notFoundException.Message;
                break;
            case AppUnauthorizedException unauthorizedException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Error.Code = unauthorizedException.Code;
                response.Error.Message = unauthorizedException.Message;
                break;

            case AppForbiddenException forbiddenException:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.Error.Code = forbiddenException.Code;
                response.Error.Message = forbiddenException.Message;
                break;

            case AppConflictException conflictException:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Error.Code = conflictException.Code;
                response.Error.Message = conflictException.Message;
                break;

            case AppException appException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Error.Code = appException.Code;
                response.Error.Message = appException.Message;
                break;
            case DomainConflictException domainConflictException:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Error.Code = domainConflictException.Code;
                response.Error.Message = domainConflictException.Message;
                break;

            case DomainException domainException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Error.Code = domainException.Code;
                response.Error.Message = domainException.Message;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Error.Code = "internal_error";
                response.Error.Message =  ex.Message;
                break;
        }

        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}