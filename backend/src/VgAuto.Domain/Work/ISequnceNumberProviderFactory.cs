namespace VgAuto.Core.Domain
{
    public interface ISequnceNumberProviderFactory
    {
        ISequencedNumberProvider GetNumberProvider<T>();
    }
}