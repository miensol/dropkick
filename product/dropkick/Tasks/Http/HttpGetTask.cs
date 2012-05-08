namespace dropkick.Tasks.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Xml;
    using DeploymentModel;
    using Magnum.Extensions;

    internal class HttpGetTask : Task
    {
        readonly PhysicalServer _server;

        public HttpGetTask(PhysicalServer server)
        {
            _server = server;
        }

        public string Name
        {
            get { return "Get given uri content using http"; }
        }

        public string PathToAppend { get; set; }

        public string FileToGetBaseUriFrom { get; set; }

        public string XPathForUri { get; set; }

        public HttpStatusCode? ExpectedStatusCode { get; set; }

        public IEnumerable<string> WordsThatCauseAlert { get; set; }

        public DeploymentResult VerifyCanRun()
        {
            var result = new DeploymentResult();
            WithValidUriDo(result, uri => result.AddGood("Get {0}", uri));
            return result;
        }

        public DeploymentResult Execute()
        {
            var result = new DeploymentResult();
            WithValidUriDo(result, uri => ValidateUriContent(uri, result));
            return result;
        }

        void ValidateUriContent(Uri uriToValidate, DeploymentResult result)
        {
            var request = (HttpWebRequest) WebRequest.Create(uriToValidate);
            try
            {
                using (var response = (HttpWebResponse) request.GetResponse())
                {
                    ValidateResponse(result, response, request);
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = (HttpWebResponse) ex.Response;
                    ValidateResponse(result, response, request);
                }
            }
        }

        void ValidateResponse(DeploymentResult result, HttpWebResponse response, HttpWebRequest request)
        {
            if (ExpectedStatusCode.HasValue)
            {
                if (response.StatusCode == ExpectedStatusCode)
                {
                    GotResponse(result, response, request);
                }
                else
                {
                    result.AddAlert("Got respone from {0} with invalid status code {1} {2}", request.RequestUri,
                                    response.StatusCode, response.StatusDescription);
                }
            }
            else
            {
                GotResponse(result, response, request);
            }
        }

        void GotResponse(DeploymentResult result, HttpWebResponse response, HttpWebRequest request)
        {
            result.AddGood("Got response from {0}: {1} {2}", request.RequestUri, response.StatusCode,
                           response.StatusDescription);
            if (WordsThatCauseAlert.Any())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var allText = reader.ReadToEnd();
                    if (WordsThatCauseAlert.Any(allText.Contains))
                    {
                        result.AddAlert(
                            "Response from {0} contains one of {1} which may indicate errors in configuration of service",
                            request.RequestUri, string.Join(", ", WordsThatCauseAlert));
                    }
                }
            }
        }

        string CanExtractXpathFromFile(DeploymentResult result)
        {
            var targetPath = _server.MapPath(FileToGetBaseUriFrom);
            if (File.Exists(targetPath) == false)
            {
                result.AddAlert("The file to get uri from does not exist {0}", targetPath);
                return string.Empty;
            }
            return ReadXmlDocument(result, doc =>
                {
                    var navigator = doc.CreateNavigator();
                    if (XPathForUri.IsNotEmpty())
                    {
                        var node = navigator.SelectSingleNode(XPathForUri);
                        if (node != null)
                        {
                            return node.Value;
                        }
                        result.AddAlert("Didn't find {0} in file {1}", XPathForUri, doc.BaseURI);
                    }
                    return string.Empty;
                });
        }

        string ReadXmlDocument(DeploymentResult result, Func<XmlDocument, string> xmlAction)
        {
            var targetPath = _server.MapPath(FileToGetBaseUriFrom);
            try
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(targetPath);
                return xmlAction(xmlDocument);
            }
            catch (UnauthorizedAccessException)
            {
                result.AddAlert("Cannot read file {0} due to inssufficnient permissions", targetPath);
            }
            catch (ArgumentException)
            {
                result.AddAlert("The file {0} is not valid xml", targetPath);
            }
            catch (XmlException)
            {
                result.AddAlert("The file {0} is not valid xml", targetPath);
            }
            return string.Empty;
        }

        void WithValidUriDo(DeploymentResult result, Action<Uri> jobToDoWithValidUri)
        {
            var baseUriString = _server.Name;
            try
            {
                var baseUri = new UriBuilder("http", baseUriString);
                if (FileToGetBaseUriFrom.IsNotEmpty())
                {
                    baseUriString = CanExtractXpathFromFile(result);
                    if (baseUriString.IsNotEmpty())
                    {
                        baseUri = new UriBuilder(baseUriString);
                    }
                }
                if (PathToAppend.IsNotEmpty())
                {
                    baseUri.Path = PathToAppend;
                }
                var resultUri = baseUri.Uri;
                jobToDoWithValidUri(resultUri);
            }
            catch (UriFormatException)
            {
                result.AddAlert("Could not construct uri to get from {0}", baseUriString);
            }
        }
    }
}