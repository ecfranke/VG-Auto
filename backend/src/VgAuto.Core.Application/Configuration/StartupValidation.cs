using System;
using System.Collections.Generic;
using VgAuto.Core.Application.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace VgAuto.Core.Application.Configuration
{
    /// <summary>Refuses to start with missing or placeholder secrets.</summary>
    public static class StartupValidation
    {
        private static readonly string[] PlaceholderMarkers = { "[your-", "[smtp-", "[random-", "changeme" };

        public static void Validate(IConfiguration configuration, IHostEnvironment environment)
        {
            var errors = new List<string>();

            try { AppJwtToken.EnsureJwtSecret(configuration["JwtOptions:Secret"]); }
            catch (ArgumentException ex) { errors.Add(ex.Message); }

            var consumerSecret = configuration["JwtOptions:ConsumerSecret"];
            if (string.IsNullOrWhiteSpace(consumerSecret) || consumerSecret.Length < 16)
                errors.Add("JwtOptions:ConsumerSecret must be set (at least 16 characters) and equal SERVER_SECRET of the frontend.");

            foreach (var key in new[] { "JwtOptions:Secret", "JwtOptions:ConsumerSecret", "DbOptions:Password" })
            {
                var value = configuration[key];
                if (value != null && Array.Exists(PlaceholderMarkers, m => value.Contains(m, StringComparison.OrdinalIgnoreCase)))
                    errors.Add($"{key} still contains a placeholder value.");
            }

            if (!environment.IsDevelopment())
            {
                var corsMode = configuration["Cors:Mode"];
                if (string.Equals(corsMode, "open", StringComparison.OrdinalIgnoreCase))
                    errors.Add("Cors:Mode 'open' is only allowed in Development. Configure Cors:AllowedOrigins instead.");
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException("Invalid configuration:" + Environment.NewLine + " - " + string.Join(Environment.NewLine + " - ", errors));
            }
        }
    }
}
