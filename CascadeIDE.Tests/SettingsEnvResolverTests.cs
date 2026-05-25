using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SettingsEnvResolverTests
{
    [Fact]
    public void Resolve_prefers_non_empty_environment_over_literal()
    {
        var name = "CASCADE_TEST_SETTINGS_ENV_" + Guid.NewGuid().ToString("N");
        try
        {
            Environment.SetEnvironmentVariable(name, @"C:\from-env\tool.exe");
            var value = SettingsEnvResolver.Resolve(@"C:\literal.exe", name);
            Assert.Equal(@"C:\from-env\tool.exe", value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    [Fact]
    public void ResolveLaunchPath_PATH_sentinel_returns_empty_not_process_PATH_variable()
    {
        var pathList = Environment.GetEnvironmentVariable("PATH");
        try
        {
            Environment.SetEnvironmentVariable("PATH", @"C:\should-not-be-used-as-exe.exe");
            Assert.Equal("", SettingsEnvResolver.ResolveLaunchPath(@"C:\literal.exe", "PATH"));
            Assert.Equal("", SettingsEnvResolver.ResolveLaunchPath("", "path"));

            var settings = new CascadeIdeSettings
            {
                Languages = new LanguagesSettings
                {
                    CSharp = new CSharpLanguageServerSettings
                    {
                        Mode = "OmniSharp",
                        OmniSharp = new LanguageServerLaunchProfile
                        {
                            Executable = @"C:\ignored-when-path-sentinel.exe",
                            ExecutableEnv = "PATH",
                        },
                    },
                },
            };

            var (_, exe, _) = settings.Languages.CSharp.ResolveForRuntime();
            Assert.Equal("", exe);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", pathList);
        }
    }

    [Fact]
    public void Resolve_falls_back_to_literal_when_env_missing_or_empty()
    {
        var name = "CASCADE_TEST_MISSING_" + Guid.NewGuid().ToString("N");
        Assert.Equal("literal", SettingsEnvResolver.Resolve("literal", name));
        Assert.Equal("literal", SettingsEnvResolver.Resolve("literal", ""));
    }

    [Fact]
    public void Language_profile_executable_env_applied_in_ResolveForRuntime()
    {
        var name = "CASCADE_TEST_LSP_EXE_" + Guid.NewGuid().ToString("N");
        try
        {
            Environment.SetEnvironmentVariable(name, @"D:\tools\OmniSharp.exe");
            var settings = new CascadeIdeSettings
            {
                Languages = new LanguagesSettings
                {
                    CSharp = new CSharpLanguageServerSettings
                    {
                        Mode = "OmniSharp",
                        OmniSharp = new LanguageServerLaunchProfile
                        {
                            Executable = "",
                            ExecutableEnv = name,
                        },
                    },
                },
            };

            var (_, exe, _) = settings.Languages.CSharp.ResolveForRuntime();
            Assert.Equal(@"D:\tools\OmniSharp.exe", exe);
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    [Fact]
    public void Intercom_transport_resolves_base_url_from_env()
    {
        var name = "CASCADE_TEST_INTERCOM_URL_" + Guid.NewGuid().ToString("N");
        try
        {
            Environment.SetEnvironmentVariable(name, "http://127.0.0.1:9090");
            var t = new IntercomTransportSettings
            {
                BaseUrl = "http://127.0.0.1:5080",
                BaseUrlEnv = name,
            };
            Assert.Equal("http://127.0.0.1:9090", t.ResolveBaseUrl());
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }
}
