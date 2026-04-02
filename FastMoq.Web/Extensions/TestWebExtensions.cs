using Microsoft.AspNetCore.Mvc;

namespace FastMoq.Web.Extensions
{
    /// <summary>
    ///     Class TestWebExtensions.
    /// </summary>
    public static class TestWebExtensions
    {
        /// <summary>
        ///     Gets the content of the object result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result">The result.</param>
        /// <returns>T.</returns>
        public static T GetObjectResultContent<T>(this ActionResult<T> result)
        {
            if (result.Result is ObjectResult { Value: T value })
            {
                return value;
            }

            return result.Value!;
        }
    }
}