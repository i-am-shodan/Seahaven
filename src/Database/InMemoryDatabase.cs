using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Seahaven.Database
{
    internal class IDValue
    {
        private static Random rand = new Random();

        internal enum IDType
        {
            Value,
            LastMatching,
            Random
        }

        internal IDType Type { get; set; }
        internal ulong Value { get; set; }
        internal bool WasProviderByUser { get; set; } = false;

        internal IDValue(IDType type, bool wasprovided = false, ulong value = 0)
        {
            this.Type = type;
            this.Value = value;
            this.WasProviderByUser = wasprovided;
        }

        internal T Get<T>() where T : class
        {
            switch (Type)
            {
                case IDType.Value:
                    return InMemoryDatabase.Get<T>(Value);
                case IDType.LastMatching:
                    var v = InMemoryDatabase.TryFindIDOfLast<T>();
                    return InMemoryDatabase.Get<T>(v);
                case IDType.Random:
                    return InMemoryDatabase.Get<T>().ToList().OrderBy(x => rand.Next()).First();
                default:
                    throw new NotImplementedException();
            }
        }
    }

    internal class InMemoryDatabase
    {
        private static ConcurrentDictionary<ulong, object> idToObjectLookup = new ConcurrentDictionary<ulong, object>();
        private static ulong lastId = 0;
        private static ulong currentId = 0;

        internal static ulong LastID() => lastId;

        public static void AddToDatabase(object o)
        {
            var id = Interlocked.Increment(ref lastId);
            currentId = id;
            idToObjectLookup[id] = o;

            if (CommandHandling.IsREPL)
            {
                var output = $"{o.GetType().Name}: {id} - {o.ToString()}";
                Console.WriteLine(output);
            }
            else
            {
                Console.WriteLine(JsonConvert.SerializeObject(o, Formatting.Indented));
            }
        }

        public static ulong TryFindIDOfLast<T>()
        {
            if (currentId != 0 && idToObjectLookup.ContainsKey(currentId) && idToObjectLookup[currentId] is T)
            {
                return currentId;
            }

            foreach (var key in idToObjectLookup.Keys.OrderByDescending(x => x))
            {
                if (idToObjectLookup[key] is T)
                {
                    return key;
                }
            }
            throw new Exception("Could not find last object of type " + typeof(T).Name);
        }

        public static T Get<T>(ulong id) where T : class
        {
            if (!idToObjectLookup.ContainsKey(id))
            {
                throw new Exception($"Error: ID ({id}) was not found");
            }
            else if (idToObjectLookup[id] is not T)
            {
                throw new Exception($"Error: ID ({id}) specified was not the right type");
            }
            return idToObjectLookup[id] as T;
        }

        public static IEnumerable<T> Get<T>() where T : class
        {
            return idToObjectLookup.Values.Where(x => x is T).Select(x => x as T);
        }

        public static void SetCurrentId(ulong id)
        {
            currentId = id;
        }

        public static bool IsId<T>(ulong id)
        {
            if (idToObjectLookup.ContainsKey(id) && idToObjectLookup[id] is T)
            {
                return true;
            }
            return false;
        }
    }
}
