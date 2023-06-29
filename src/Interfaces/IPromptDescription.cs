using Newtonsoft.Json;

namespace Seahaven.Interfaces
{
    public interface IPromptDescription
    {
        [JsonIgnore]
        abstract string Prompt { get; }
    }
}
