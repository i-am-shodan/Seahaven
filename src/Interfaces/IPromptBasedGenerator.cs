namespace Seahaven.Interfaces
{
    public interface IPromptBasedGenerator
    {
        Task<T> DeserializeResponseFromJson<T>(string prompt, int attempt = 0);
        Task<string> GetResponseAsString(string prompt);
        void SetDeployment(string deployment);
        void SetKey(string key);
        void SetURI(string uri);
    }
}
