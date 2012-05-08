namespace dropkick.Configuration.Dsl.Http
{
    using System.Net;
    using DeploymentModel;
    using Tasks;
    using Tasks.Http;

    public static class Extension
    {
         public static HttpGetOptions HttpGet(this ProtoServer ps)
         {
             var proto = new HttpGetProtoTask();
             ps.RegisterProtoTask(proto);
             return proto;
         }
    }

    public interface HttpGetOptions
    {
        HttpGetOptions Path(string pathToAppendToHostName);
        HttpGetOptions BaseUriIsInFile(string fileNameToGetUriFrom, string xpathToAtrubiute);
        HttpGetOptions ExpectSuccessStatusCode();
        HttpGetOptions InvalidWordsAre(params string[] textToMakeAlert);
    }

    public class HttpGetProtoTask : BaseProtoTask, HttpGetOptions
    {
        string _pathToAppend;
        string _xpathAttribte;
        string _fileToGetUriFrom;
        HttpStatusCode? _expectedStatusCode;
        string[] _textThatCauseAlert = new string[0];

        public override void RegisterRealTasks(PhysicalServer server)
        {
            server.AddTask(new HttpGetTask(server)
            {
                PathToAppend = _pathToAppend,
                FileToGetBaseUriFrom = _fileToGetUriFrom,
                XPathForUri = _xpathAttribte,
                ExpectedStatusCode = _expectedStatusCode,
                WordsThatCauseAlert = _textThatCauseAlert
            });
        }

        public HttpGetOptions Path(string pathToAppendToHostName)
        {
            _pathToAppend = pathToAppendToHostName;
            return this;
        }

        public HttpGetOptions BaseUriIsInFile(string fileNameToGetUriFrom, string xpathToAtrubiute)
        {
            _fileToGetUriFrom = fileNameToGetUriFrom;
            _xpathAttribte = xpathToAtrubiute;
            return this;
        }

        public HttpGetOptions ExpectSuccessStatusCode()
        {
            _expectedStatusCode = HttpStatusCode.OK;
            return this;
        }        

        public HttpGetOptions InvalidWordsAre(params string[] textThatCausesErrors)
        {
            _textThatCauseAlert = textThatCausesErrors ?? new string[0];
            return this;
        }
    }    
}