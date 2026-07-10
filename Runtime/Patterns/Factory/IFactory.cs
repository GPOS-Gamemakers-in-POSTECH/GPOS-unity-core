namespace GPOS.Core.Factory
{
    /// <summary>Creates products of type <typeparamref name="TProduct"/> with no argument.</summary>
    public interface IFactory<out TProduct>
    {
        TProduct Create();
    }

    /// <summary>Creates products selected by a <typeparamref name="TKey"/>.</summary>
    public interface IFactory<in TKey, out TProduct>
    {
        TProduct Create(TKey key);
    }
}
