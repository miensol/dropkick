namespace dropkick.Configuration.Dsl.Http
{
    public interface HttpGetOptions
    {
        HttpGetOptions Path(string pathToAppendToHostName);
        HttpGetOptions BaseUriIsInFile(string fileNameToGetUriFrom, string xpathToAtrubiute);
        HttpGetOptions ExpectSuccessStatusCode();
        HttpGetOptions InvalidWordsAre(params string[] textToMakeAlert);
    }
}