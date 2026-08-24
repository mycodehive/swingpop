using UnityEngine;

namespace SwingPop.Online
{
    public interface IMatchMessageSerializer
    {
        string Serialize<T>(T message);
        T Deserialize<T>(string payload);
    }

    public sealed class JsonMatchMessageSerializer : IMatchMessageSerializer
    {
        public string Serialize<T>(T message)
        {
            return JsonUtility.ToJson(message);
        }

        public T Deserialize<T>(string payload)
        {
            return JsonUtility.FromJson<T>(payload);
        }
    }
}
