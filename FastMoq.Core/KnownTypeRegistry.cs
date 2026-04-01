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
        internal static bool TryGetDirectInstance(Mocker mocker, Type type, out object? instance)
        {
            instance = null;

            if (mocker.Behavior.Has(MockFeatures.FailOnUnconfigured))
            {
                return false;
            }

            if (!mocker.Contains<IFileSystem>() && type.IsEquivalentTo(typeof(IFileSystem)))
            {
                instance = mocker.fileSystem;
                return true;
            }

            if (!mocker.Contains<HttpClient>() && type.IsEquivalentTo(typeof(HttpClient)))
            {
                instance = mocker.HttpClient;
                return true;
            }

            return false;
        }

        internal static bool TryGetManagedInstance(Mocker mocker, Type requestedType, out object? instance)
        {
            instance = null;

            if (!requestedType.IsAssignableTo(typeof(DbContext)))
            {
                return false;
            }

            var mock = mocker.GetMockDbContext(requestedType);
            instance = mock.Object;
            return instance != null;
        }

        internal static void ConfigureMock(Mocker mocker, Type type, Mock mock)
        {
            if (type == typeof(HttpContext) && mock is Mock<HttpContext> httpContextMock)
            {
                httpContextMock.SetupMockProperty(x => x.Session!, mocker.GetObject<ISession>()!);
                httpContextMock.SetupMockProperty(x => x.Items, new Dictionary<object, object?>());
                httpContextMock.SetupMockProperty(x => x.User, new ClaimsPrincipal());
                return;
            }

            if (type == typeof(IHttpContextAccessor) && mock is Mock<IHttpContextAccessor> accessorMock)
            {
                accessorMock.SetupMockProperty(x => x.HttpContext!, mocker.GetObject<HttpContext>()!);
                return;
            }

            if (type == typeof(HttpContextAccessor) && mock is Mock<HttpContextAccessor> accessorClassMock)
            {
                accessorClassMock.SetupMockProperty(x => x.HttpContext!, mocker.GetObject<HttpContext>()!);
            }
        }

        internal static void ApplyObjectDefaults(Mocker mocker, object? obj)
        {
            if (!mocker.InnerMockResolution || !mocker.Behavior.Has(MockFeatures.AutoInjectDependencies) || obj == null)
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
