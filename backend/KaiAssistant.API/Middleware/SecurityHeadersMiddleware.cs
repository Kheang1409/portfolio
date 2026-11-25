using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using KaiAssistant.Application.Options;

namespace KaiAssistant.API.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CspOptions _options;
    private const string NonceHeaderName = "X-Content-Security-Policy-Nonce";

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<CspOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate cryptographically secure nonce for this request
        var nonce = GenerateNonce();

        // Store nonce in HttpContext.Items for access in views/razor if needed
        context.Items[NonceHeaderName] = nonce;

        // Add security headers
        context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
        context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.TryAdd("X-XSS-Protection", "0");
        context.Response.Headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
        context.Response.Headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
        context.Response.Headers.TryAdd("Cross-Origin-Resource-Policy", "same-origin");

        // Build Content Security Policy with nonce
        var csp = BuildCsp(nonce);
        context.Response.Headers.TryAdd("Content-Security-Policy", csp);

        // Also add the nonce as a custom header for frontend consumption
        context.Response.Headers.TryAdd(NonceHeaderName, nonce);

        await _next(context).ConfigureAwait(false);
    }

    private static string GenerateNonce()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private string BuildCsp(string nonce)
    {
        var nonceValue = $"'nonce-{nonce}'";

        var directives = new List<string>
        {
            // Default sources - restrict everything by default
            "default-src 'self'",

            // Scripts - allow nonce'd inline scripts (Next.js hydration), strict-dynamic for trusted scripts
            $"script-src 'self' {nonceValue} 'strict-dynamic' https://www.paypal.com https://www.sandbox.paypal.com https://generativelanguage.googleapis.com",

            // Styles - allow self, nonce'd inline styles (for critical CSS), and unsafe-inline for Tailwind/Next.js
            $"style-src 'self' {nonceValue} 'unsafe-inline' https://fonts.googleapis.com",

            // Fonts - self and Google Fonts
            "font-src 'self' https://fonts.gstatic.com data:",

            // Images - self, data URIs, and https
            "img-src 'self' data: https:",

            // Connect - API calls to backend, PayPal, Gemini
            "connect-src 'self' https://www.paypal.com https://www.sandbox.paypal.com https://api-m.paypal.com https://api-m.sandbox.paypal.com https://generativelanguage.googleapis.com",

            // Frames - PayPal buttons/checkout
            "frame-src 'self' https://www.paypal.com https://www.sandbox.paypal.com",

            // Form actions - only self
            "form-action 'self'",

            // Base URI - only self
            "base-uri 'self'",

            // Object - none
            "object-src 'none'",

            // Frame ancestors - none (no framing)
            "frame-ancestors 'none'",

            // Upgrade insecure requests
            "upgrade-insecure-requests",

            // Block all mixed content
            "block-all-mixed-content"
        };

        // Allow customization via options
        if (_options.AllowUnsafeEval)
        {
            // Replace strict-dynamic with unsafe-eval if explicitly allowed (not recommended)
            var scriptDirective = directives.FirstOrDefault(d => d.StartsWith("script-src"));
            if (scriptDirective != null)
            {
                directives.Remove(scriptDirective);
                directives.Add($"script-src 'self' {nonceValue} 'unsafe-eval' https://www.paypal.com https://www.sandbox.paypal.com https://generativelanguage.googleapis.com");
            }
        }

        return string.Join("; ", directives);
    }
}

// Extension method for easy access to nonce in controllers/views
public static class SecurityHeadersMiddlewareExtensions
{
    public static string? GetCspNonce(this HttpContext context)
    {
        return context.Items.TryGetValue("X-Content-Security-Policy-Nonce", out var value) ? value as string : null;
    }
}