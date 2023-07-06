public class AntiXssMiddleware
    {
        private readonly RequestDelegate _next;

        public AntiXssMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var allowedEndPoints = new string[] { "uploadExcel", "addExcelDepartments", "submitExcel" };
            var routingurlSplited = httpContext.Request.Path.Value.Split("/");
            if (!allowedEndPoints.Any(endPoint => routingurlSplited.Any(en => en.Equals(endPoint, StringComparison.OrdinalIgnoreCase))))
            {
                // leaveOpen: true to leave the stream open after disposing,
                // so it can be read by the model binders
                using (var streamReader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    httpContext.Request.EnableBuffering();

                    using var reader = new StreamReader(httpContext.Request.Body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                    var requestBody = await reader.ReadToEndAsync();
                    httpContext.Request.Body.Position = 0; // Reset the position of the request body stream

                    // Sanitize the request body
                    var sanitizedRequestBody = Sanitize(requestBody);

                    // Replace the request body with the sanitized content
                    var byteArray = Encoding.UTF8.GetBytes(sanitizedRequestBody);
                    httpContext.Request.Body = new MemoryStream(byteArray);
                }
            }


            await _next(httpContext);
        }
        private string Sanitize(string input)
        {
            // Remove HTML tags
            var htmlDocument = new HtmlAgilityPack.HtmlDocument();
            htmlDocument.LoadHtml(input);
            var sanitized = htmlDocument.DocumentNode.InnerText;

            // Remove JavaScript code
            sanitized = Regex.Replace(sanitized, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", string.Empty, RegexOptions.IgnoreCase);

            return sanitized;
        }
    }
