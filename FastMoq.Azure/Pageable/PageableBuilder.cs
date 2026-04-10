using System.Globalization;
using Azure;

namespace FastMoq.Azure.Pageable
{
    /// <summary>
    /// Creates synchronous and asynchronous Azure SDK pageable sequences for tests.
    /// </summary>
    public static class PageableBuilder
    {
        /// <summary>
        /// Creates a single Azure SDK page with the provided values.
        /// </summary>
        /// <typeparam name="T">The value type stored in the page.</typeparam>
        /// <param name="values">The values to include in the page.</param>
        /// <param name="continuationToken">The continuation token for the next page, if any.</param>
        /// <param name="response">The raw response to expose from the page.</param>
        /// <returns>A single Azure SDK page.</returns>
        public static Page<T> CreatePage<T>(IReadOnlyList<T> values, string? continuationToken = null, Response? response = null)
            where T : notnull
        {
            ArgumentNullException.ThrowIfNull(values);

            return Page<T>.FromValues(values, continuationToken, response ?? new AzureTestResponse());
        }

        /// <summary>
        /// Creates an <see cref="AsyncPageable{T}" /> from explicit pages.
        /// </summary>
        /// <typeparam name="T">The value type stored in the pageable sequence.</typeparam>
        /// <param name="pages">The pages to expose through the sequence.</param>
        /// <returns>An asynchronous pageable sequence.</returns>
        public static AsyncPageable<T> CreateAsyncPageable<T>(IEnumerable<Page<T>> pages)
            where T : notnull
        {
            ArgumentNullException.ThrowIfNull(pages);

            return AsyncPageable<T>.FromPages(pages.ToArray());
        }

        /// <summary>
        /// Creates an <see cref="AsyncPageable{T}" /> by splitting values into test pages.
        /// </summary>
        /// <typeparam name="T">The value type stored in the pageable sequence.</typeparam>
        /// <param name="values">The values to expose through the sequence.</param>
        /// <param name="pageSize">The number of values to include per page.</param>
        /// <param name="response">The raw response to expose from each page.</param>
        /// <returns>An asynchronous pageable sequence.</returns>
        public static AsyncPageable<T> CreateAsyncPageable<T>(IEnumerable<T> values, int pageSize = int.MaxValue, Response? response = null)
            where T : notnull
        {
            return CreateAsyncPageable(CreatePages(values, pageSize, response ?? new AzureTestResponse()));
        }

        /// <summary>
        /// Creates a <see cref="Pageable{T}" /> from explicit pages.
        /// </summary>
        /// <typeparam name="T">The value type stored in the pageable sequence.</typeparam>
        /// <param name="pages">The pages to expose through the sequence.</param>
        /// <returns>A synchronous pageable sequence.</returns>
        public static Pageable<T> CreatePageable<T>(IEnumerable<Page<T>> pages)
            where T : notnull
        {
            ArgumentNullException.ThrowIfNull(pages);

            return Pageable<T>.FromPages(pages.ToArray());
        }

        /// <summary>
        /// Creates a <see cref="Pageable{T}" /> by splitting values into test pages.
        /// </summary>
        /// <typeparam name="T">The value type stored in the pageable sequence.</typeparam>
        /// <param name="values">The values to expose through the sequence.</param>
        /// <param name="pageSize">The number of values to include per page.</param>
        /// <param name="response">The raw response to expose from each page.</param>
        /// <returns>A synchronous pageable sequence.</returns>
        public static Pageable<T> CreatePageable<T>(IEnumerable<T> values, int pageSize = int.MaxValue, Response? response = null)
            where T : notnull
        {
            return CreatePageable(CreatePages(values, pageSize, response ?? new AzureTestResponse()));
        }

        private static IReadOnlyList<Page<T>> CreatePages<T>(IEnumerable<T> values, int pageSize, Response response)
            where T : notnull
        {
            ArgumentNullException.ThrowIfNull(values);
            ArgumentNullException.ThrowIfNull(response);

            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than zero.");
            }

            var bufferedValues = values as IReadOnlyList<T> ?? values.ToArray();
            if (bufferedValues.Count == 0)
            {
                return [CreatePage(Array.Empty<T>(), null, response)];
            }

            var pages = new List<Page<T>>();
            for (var index = 0; index < bufferedValues.Count; index += pageSize)
            {
                var pageValues = bufferedValues.Skip(index).Take(pageSize).ToArray();
                var nextIndex = index + pageValues.Length;
                var continuationToken = nextIndex < bufferedValues.Count
                    ? nextIndex.ToString(CultureInfo.InvariantCulture)
                    : null;

                pages.Add(CreatePage(pageValues, continuationToken, response));
            }

            return pages;
        }
    }
}