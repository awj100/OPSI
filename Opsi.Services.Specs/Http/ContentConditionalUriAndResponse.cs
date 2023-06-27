using System.Net;
using SendGrid.Helpers.Mail;

namespace Opsi.Services.Specs.Http
{
    /// <summary>
    /// A class for handling <c>HttpClient.PostAsync</c> requests and conditionally returning a configured response based upon a conditional function.
    /// </summary>
    public class ContentConditionalUriAndResponse : UriAndResponse
    {
        /// <summary>
        /// The conditional function which determines whether <see cref="UriAndResponse.HttpResponse"/> should be returned.
        /// </summary>
        public Func<HttpContent, bool>? RequestChecker { get; set; }

        /// <summary>
        /// The asynchronous conditional function which determines whether <see cref="UriAndResponse.HttpResponse"/> should be returned.
        /// </summary>
        public Func<HttpContent, Task<bool>>? RequestCheckerAsync { get; set; }

        /// <summary>
        /// Creates an instance of <see cref="UriAndResponse"/> for handling <c>HttpClient</c> requests and returning a configured response based upon a conditional function.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> which should be handled.</param>
        /// <param name="httpResponse">The response to be returned for the specified <see cref="uri"/> argument.</param>
        /// <param name="requestChecker">The conditional function which determines whether <see cref="UriAndResponse.HttpResponse"/> should be returned.</param>
        public ContentConditionalUriAndResponse(Uri uri, HttpResponseMessage httpResponse, Func<HttpContent, bool> requestChecker) : base(uri, httpResponse)
        {
            RequestChecker = requestChecker ?? throw new ArgumentNullException(nameof(requestChecker), $"If no request content checking is required then use {typeof(UriAndResponse).AssemblyQualifiedName} instead of {typeof(ContentConditionalUriAndResponse).AssemblyQualifiedName}.");
        }

        /// <summary>
        /// Creates an instance of <see cref="UriAndResponse"/> for handling <c>HttpClient</c> requests and returning a configured response based upon a conditional function.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> which should be handled.</param>
        /// <param name="httpResponse">The response to be returned for the specified <see cref="uri"/> argument.</param>
        /// <param name="requestCheckerAsync">The asynchronous conditional function which determines whether <see cref="UriAndResponse.HttpResponse"/> should be returned.</param>
        public ContentConditionalUriAndResponse(Uri uri, HttpResponseMessage httpResponse, Func<HttpContent, Task<bool>> requestCheckerAsync) : base(uri, httpResponse)
        {
            RequestCheckerAsync = requestCheckerAsync ?? throw new ArgumentNullException(nameof(requestCheckerAsync), $"If no request content checking is required then use {typeof(UriAndResponse).AssemblyQualifiedName} instead of {typeof(ContentConditionalUriAndResponse).AssemblyQualifiedName}.");
        }

        internal protected override async Task<HttpResponseMessage> HandleRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var fallbackResponse = new HttpResponseMessage(HttpStatusCode.NotImplemented);

            if (request.Content == null)
            {
                return fallbackResponse;
            }

            var permittedMethods = new List<string>
            {
                HttpMethod.Patch.Method,
                HttpMethod.Post.Method,
                HttpMethod.Put.Method
            };

            if (!permittedMethods.Contains(request.Method.Method))
            {
                throw new InvalidOperationException($"Invalid HTTP method: {request.Method.Method}. The conditional form of {typeof(ContentConditionalUriAndResponse).AssemblyQualifiedName} can only be used with PATCH, POST or PUT requests.");
            }

            var expectationMet = RequestChecker != null
                ? RequestChecker(request.Content)
                : await RequestCheckerAsync!(request.Content);

            if (!expectationMet)
            {
                return new HttpResponseMessage(HttpStatusCode.NotImplemented);
            }

            return request.RequestUri != null && request.RequestUri.PathAndQuery == Uri.PathAndQuery
                       ? HttpResponse
                       : fallbackResponse;
        }
    }
}
