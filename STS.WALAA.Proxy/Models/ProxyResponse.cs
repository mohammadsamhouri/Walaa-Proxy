using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace STS.WALAA.Models;

public class ProxyResponse : ContentResult
{
    private object? Data { get; set; }

    private bool Success { get; set; }

    private string Message { get; set; } = string.Empty;

    public static ProxyResponse OnSuccess(HttpStatusCode statusCode, string message, object? data) => new()
    {
        Success = true,
        Data = data,
        StatusCode = (int?)statusCode,
        Message = message
    };

    public static ProxyResponse OnFailure(HttpStatusCode statusCode, string message, object? data) => new()
    {
        Success = false,
        Data = data,
        StatusCode = (int?)statusCode,
        Message = message
    };

    public static ProxyResponse OnSuccess(string message, object? data) => OnSuccess(HttpStatusCode.OK, message, data);

    public static ProxyResponse OnSuccess(HttpStatusCode statusCode, string message) => OnSuccess(statusCode, message, default);

    public static ProxyResponse OnSuccess(string message) => OnSuccess(HttpStatusCode.OK, message, default);

    public static ProxyResponse OnSuccess() => OnSuccess(HttpStatusCode.OK, "Success", default);

    public static ProxyResponse OnFailure(string message, object? data) => OnFailure(HttpStatusCode.BadRequest, message, data);

    public static ProxyResponse OnFailure(HttpStatusCode statusCode, string message) => OnFailure(statusCode, message, default);

    public static ProxyResponse OnFailure(string message) => OnFailure(HttpStatusCode.BadRequest, message, default);

    public static ProxyResponse OnFailure() => OnFailure(HttpStatusCode.BadRequest, "Error", default);

    public override Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = "application/json";
        response.StatusCode = StatusCode ?? (int)HttpStatusCode.BadRequest;

        return response.WriteAsync(JsonSerializer.Serialize(new
        {
            Success,
            Message,
            Data
        }));
    }
}