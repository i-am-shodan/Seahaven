namespace Seahaven.Interfaces
{
    public interface IRandomBasedGenerator
    {
        int Next(int min, int max);

        int Next();
    }
}
