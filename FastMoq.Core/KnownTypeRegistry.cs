using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Net.Http;
using System.Security.Claims;
using FastMoq.Extensions;
using FastMoq.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
                ConfigureMock = ConfigureBuiltInFileSystemMock,
            },
            new KnownTypeRegistration(typeof(HttpClient))
            {
                DirectInstanceFactory = TryGetBuiltInHttpClient,
            },
            new KnownTypeRegistration(typeof(Uri))
            {
                DirectInstanceFactory = TryGetBuiltInUri,
            },
            new KnownTypeRegistration(typeof(HttpContext))
            {
                DirectInstanceFactory = TryGetBuiltInHttpContext,
                ManagedInstanceFactory = TryGetBuiltInHttpContext,
                ConfigureMock = ConfigureBuiltInHttpContextMock,
            },
            new KnownTypeRegistration(typeof(ControllerContext))
            {
                DirectInstanceFactory = TryGetBuiltInControllerContext,
                ManagedInstanceFactory = TryGetBuiltInControllerContext,
                ApplyObjectDefaults = ApplyBuiltInControllerContextDefaults,
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

            if (TryGetBuiltInDbContext(mocker, requestedType, out instance))
            {
                return true;
            }

            instance = null;
            return false;
        }

        internal static bool TryGetCustomManagedInstance(Mocker mocker, Type requestedType, out object? instance)
        {
            foreach (var registration in mocker.KnownTypeRegistrations)
            {
                if (registration.Matches(requestedType) && registration.TryCreateManagedInstance(mocker, requestedType, out instance))
                {
                    return true;
                }
            }

            instance = null;
            return false;
        }

        internal static bool TryGetBuiltInManagedInstance(Mocker mocker, Type requestedType, out object? instance)
        {
            foreach (var registration in BuiltInRegistrations)
            {
                if (registration.Matches(requestedType) && registration.TryCreateManagedInstance(mocker, requestedType, out instance))
                {
                    return true;
                }
            }

            instance = null;
            return false;
        }

        internal static void ConfigureMock(Mocker mocker, Type type, IFastMock mock)
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
            if ((mocker.Policy.EnabledBuiltInTypeResolutions & BuiltInTypeResolutionFlags.FileSystem) == 0)
            {
                return null;
            }

            return !mocker.Contains<IFileSystem>() && type.IsEquivalentTo(typeof(IFileSystem))
                ? mocker.fileSystem
                : null;
        }

        private static object? TryGetBuiltInHttpClient(Mocker mocker, Type type)
        {
            if ((mocker.Policy.EnabledBuiltInTypeResolutions & BuiltInTypeResolutionFlags.HttpClient) == 0)
            {
                return null;
            }

            return !mocker.Contains<HttpClient>() && type.IsEquivalentTo(typeof(HttpClient))
                ? mocker.HttpClient
                : null;
        }

        private static object? TryGetBuiltInHttpContext(Mocker mocker, Type type)
        {
            if (!type.IsEquivalentTo(typeof(HttpContext)) || mocker.Contains<HttpContext>())
            {
                return null;
            }

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(),
            };
            httpContext.Items = new Dictionary<object, object?>();

            mocker.AddType<HttpContext>(_ => httpContext, replace: true);
            return httpContext;
        }

        private static object? TryGetBuiltInUri(Mocker mocker, Type type)
        {
            if ((mocker.Policy.EnabledBuiltInTypeResolutions & BuiltInTypeResolutionFlags.Uri) == 0)
            {
                return null;
            }

            if (!type.IsEquivalentTo(typeof(Uri)))
            {
                return null;
            }

            return mocker.Uri;
        }

        private static bool TryGetBuiltInDbContext(Mocker mocker, Type requestedType, out object? instance)
        {
            instance = null;
            if ((mocker.Policy.EnabledBuiltInTypeResolutions & BuiltInTypeResolutionFlags.DbContext) == 0)
            {
                return false;
            }

            return DatabaseSupportBridge.TryCreateManagedInstance(mocker, requestedType, out instance);
        }

        private static object? TryGetBuiltInControllerContext(Mocker mocker, Type type)
        {
            if (!type.IsEquivalentTo(typeof(ControllerContext)) || mocker.HasTypeRegistration(typeof(ControllerContext)))
            {
                return null;
            }

            var controllerContext = new ControllerContext
            {
                HttpContext = mocker.GetObject<HttpContext>() ?? new DefaultHttpContext(),
            };

            mocker.AddType(controllerContext, replace: true);
            return controllerContext;
        }

        private static void ConfigureBuiltInHttpContextMock(Mocker mocker, Type type, IFastMock mock)
        {
            if (type == typeof(HttpContext) && mock.NativeMock is Mock<HttpContext> httpContextMock)
            {
                httpContextMock.SetupMockProperty(x => x.Session!, mocker.GetObject<ISession>()!);
                httpContextMock.SetupMockProperty(x => x.Items, new Dictionary<object, object?>());
                httpContextMock.SetupMockProperty(x => x.User, new ClaimsPrincipal());
            }
        }

        private static void ConfigureBuiltInHttpContextAccessorMock(Mocker mocker, Type _, IFastMock mock)
        {
            if (mock.NativeMock is Mock<IHttpContextAccessor> accessorMock)
            {
                accessorMock.SetupMockProperty(x => x.HttpContext!, mocker.GetObject<HttpContext>()!);
                return;
            }

            if (mock.NativeMock is Mock<HttpContextAccessor> accessorClassMock)
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

        private static void ApplyBuiltInControllerContextDefaults(Mocker mocker, object obj)
        {
            if (obj is ControllerContext controllerContext && controllerContext.HttpContext == null)
            {
                controllerContext.HttpContext = mocker.GetObject<HttpContext>() ?? new DefaultHttpContext();
            }
        }

        private static void ConfigureBuiltInFileSystemMock(Mocker mocker, Type type, IFastMock mock)
        {
            if (type != typeof(IFileSystem))
            {
                return;
            }

            if (mock.NativeMock is not Mock<IFileSystem> fileSystemMock)
            {
                return;
            }

            var fileSystem = mocker.fileSystem;
            fileSystemMock.SetupMockProperty(x => x.File, fileSystem.File);
            fileSystemMock.SetupMockProperty(x => x.Directory, fileSystem.Directory);
            fileSystemMock.SetupMockProperty(x => x.Path, fileSystem.Path);
            fileSystemMock.SetupMockProperty(x => x.FileInfo, fileSystem.FileInfo);
            fileSystemMock.SetupMockProperty(x => x.FileStream, fileSystem.FileStream);
            fileSystemMock.SetupMockProperty(x => x.DriveInfo, fileSystem.DriveInfo);
            fileSystemMock.SetupMockProperty(x => x.DirectoryInfo, fileSystem.DirectoryInfo);
        }
    }
}
