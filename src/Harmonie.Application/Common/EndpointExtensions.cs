using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Harmonie.Application.Common;

/// <summary>
/// Extension methods for endpoint validation and response handling
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Validate a request using FluentValidation
    /// </summary>
    public static async Task<IResult?> ValidateAsync<TRequest>(
        this TRequest request,
        IValidator<TRequest> validator,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
            
            return Results.ValidationProblem(errors);
        }
        
        return null;
    }

    /// <summary>
    /// Create a success response with proper status code
    /// </summary>
    public static IResult Created<T>(string uri, T value) 
        => Results.Created(uri, value);

    /// <summary>
    /// Create an OK response
    /// </summary>
    public static IResult Ok<T>(T value) 
        => Results.Ok(value);
}