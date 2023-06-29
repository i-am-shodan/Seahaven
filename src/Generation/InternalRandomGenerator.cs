using Seahaven.Interfaces;

namespace Seahaven.Generation
{
    internal class InternalRandomGenerator : IRandomBasedGenerator
    {
        private Random random = new Random();

        public int Next(int min, int max)
        {
            return random.Next(min, max);
        }

        public int Next()
        {
            return random.Next();
        }
    }
}
