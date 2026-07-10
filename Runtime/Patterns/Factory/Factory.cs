using System;
using System.Collections.Generic;

namespace GPOS.Core.Factory
{
    /// <summary>
    /// Registry-based factory: map a key to a creator delegate and produce instances on demand.
    /// Useful for spawning by enum, id, or type name without a large switch statement.
    /// </summary>
    public class Factory<TKey, TProduct> : IFactory<TKey, TProduct>
    {
        private readonly Dictionary<TKey, Func<TProduct>> _creators = new();

        /// <summary>Registers (or replaces) the creator used for <paramref name="key"/>.</summary>
        public void Register(TKey key, Func<TProduct> creator)
        {
            if (creator == null)
                throw new ArgumentNullException(nameof(creator));

            _creators[key] = creator;
        }

        /// <summary>Removes the creator for <paramref name="key"/>. Returns true if one was removed.</summary>
        public bool Unregister(TKey key) => _creators.Remove(key);

        public bool IsRegistered(TKey key) => _creators.ContainsKey(key);

        /// <summary>Creates a product for <paramref name="key"/>, throwing if the key is unknown.</summary>
        public TProduct Create(TKey key)
        {
            if (!_creators.TryGetValue(key, out Func<TProduct> creator))
                throw new KeyNotFoundException($"[Factory] No creator registered for key '{key}'.");

            return creator();
        }

        /// <summary>Creates a product for <paramref name="key"/> without throwing when unknown.</summary>
        public bool TryCreate(TKey key, out TProduct product)
        {
            if (_creators.TryGetValue(key, out Func<TProduct> creator))
            {
                product = creator();
                return true;
            }

            product = default;
            return false;
        }

        public void Clear() => _creators.Clear();
    }
}
