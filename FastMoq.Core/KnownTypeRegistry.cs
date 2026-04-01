using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Net.Http;
using System.Security.Claims;
using FastMoq.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FastMoq
{
    internal static class KnownTypeRegistry
    {
        private static readonly IReadOnlyList<KnownTypeRegistration> BuiltInRegistrations =
        [
            new KnownTypeRegistration(typeof(IFileSystem))
            {
                DirectInstanceFactory = TryGetBuiltInFileSystem,
            },
            new KnownTypeRegistration(typeof(HttpClient))
            {
                DirectInstanceFactory = TryGetBuiltInHttpClient,
            },
            new KnownTypeRegistration(typeof(DbContext))
            {
                IncludeDerivedTypes = true,
                ManagedInstanceFactory = TryGetBuiltInDbContext,
            },
            new KnownTypeRegistration(typeof(HttpContext))
            {
                ConfigureMock = ConfigureBuiltInHttpContextMock,
            },
            new KnownTypeRegistration(typeof(IHttpContextAccessor))
            {
                IncludeDerivedTypes = true,
                ConfigureMock = ConfigureBuiltInHttpContextAccessorMock,
                ApplyObjectDefaults = ApplyBuiltInHttpContextAccessorDefaults,
            },
        ];

        internal static bool TryGetDirectInstance(Mocker mocker, Type type, out object? instance)
        {
            foreach (var registration in GetInstanceRegistrations(mocker))
            {
                if (registration.Matches(type) && registration.TryCreateDirectInstance(mocker, type, out instance))
                {
                    return true;
                }
            }

            instance = null;
            return false;
        }

        internal static bool TryGetManagedInstance(Mocker mocker, Type requestedType, out object? instance)
        {
            foreach (var registration in GetInstanceRegistrations(mocker))
            {
                if (registration.Matches(requestedType) && registration.TryCreateManagedInstance(mocker, requestedType, out instance))
                {
                    return true;
                }
            }

            instance = null;
            return false;
        }

        internal static void ConfigureMock(Mocker mocker, Type type, Mock mock)
        {
            foreach (var registration in GetPostProcessingRegistrations(mocker))
            {
                if (registration.Matches(type))
                {
                    registration.ConfigureMock?.Invoke(mocker, type, mock);
                }
            }
        }

        internal static void ApplyObjectDefaults(Mocker mocker, object? obj)
        {
            if (obj == null)
            {
                return;
            }

            var objectType = obj.GetType();
            foreach (var registration in GetPostProcessingRegistrations(mocker))
            {
                if (registration.Matches(objectType))
                {
                    registration.ApplyObjectDefaults?.Invoke(mocker, obj);
                }
            }
        }

        private static IEnumerable<KnownTypeRegistration> GetInstanceRegistrations(Mocker mocker)
        {
            return mocker.KnownTypeRegistrations.Concat(BuiltInRegistrations);
        }

        private static IEnumerable<KnownTypeRegistration> GetPostProcessingRegistrations(Mocker mocker)
        {
            return BuiltInRegistrations.Concat(mocker.KnownTypeRegistrations);
        }

        private static object? TryGetBuiltInFileSystem(Mocker mocker, Type type)
        {
            if (mocker.Behavior.Has(MockFeatures.FailOnUnconfigured))
            {
                return null;
            }

            return !mocker.Contains<IFileSystem>() && type.IsEquivalentTo(typeof(IFileSystem))
                ? mocker.fileSystem
                : null;
        }

        private static object? TryGetBuiltInHttpClient(Mocker mocker, Type type)
        {
            if (mocker.Behavior.Has(MockFeatures.FailOnUnconfigured))
            {
                return null;
            }

            return !mocker.Contains<HttpClient>() && type.IsEquivalentTo(typeof(HttpClient))
                ? mocker.HttpClient
                : null;
        }

        private static object? TryGetBuiltInDbContext(Mocker mocker, Type requestedType)
        {
            var mock = mocker.GetMockDbContext(requestedType);
            return mock.Object;
        }

        private static void ConfigureBuiltInHttpContextMock(Mocker mocker, Type type, Mock mock)
        {
            if (type == typeof(HttpContext) && mock is Mock<HttpContext> httpContextMock)
            {
                httpContextMock.SetupMockProperty(x => x.Session!, mocker.GetObject<ISession>()!);
                httpContextMock.SetupMockProperty(x => x.Items, new Dictionary<object, object?>());
                httpContextMock.SetupMockProperty(x => x.User, new ClaimsPrincipal());
            }
        }

        private static void ConfigureBuiltInHttpContextAccessorMock(Mocker mocker, Type _, Mock mock)
        {
            if (mock is Mock<IHttpContextAccessor> accessorMock)
            {
                accessorMock.SetupMockProperty(x => x.HttpContext!, mocker.GetObject<HttpContext>()!);
                return;
            }

            if (mock is Mock<HttpContextAccessor> accessorClassMock)
            {
                accessorClassMock.SetupMockProperty(x => x.HttpContext!, mocker.GetObject<HttpContext>()!);
            }
        }

        private static void ApplyBuiltInHttpContextAccessorDefaults(Mocker mocker, object obj)
        {
            if (!mocker.InnerMockResolution || !mocker.Behavior.Has(MockFeatures.AutoInjectDependencies))
            {
                return;
            }

            if (obj is IHttpContextAccessor accessor && accessor.HttpContext == null)
            {
                accessor.HttpContext = mocker.GetObject<HttpContext>();
            }
        }
    }
}
