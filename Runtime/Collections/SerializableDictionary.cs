using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPOS.Core.Collections
{
    /// <summary>
    /// A <see cref="Dictionary{TKey, TValue}"/> that Unity can serialize. Unity's serializer does
    /// not support dictionaries directly, so keys and values are mirrored into parallel lists
    /// during serialization and rebuilt afterwards.
    ///
    /// To expose one in the Inspector, declare a concrete <c>[Serializable]</c> subclass, e.g.:
    /// <code>
    /// [Serializable] public class StringIntDictionary : SerializableDictionary&lt;string, int&gt; { }
    /// </code>
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> _keys = new();
        [SerializeField] private List<TValue> _values = new();

        public SerializableDictionary() { }

        public SerializableDictionary(IDictionary<TKey, TValue> source) : base(source) { }

        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();

            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                _keys.Add(pair.Key);
                _values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();

            int count = Mathf.Min(_keys.Count, _values.Count);
            for (int i = 0; i < count; i++)
            {
                if (_keys[i] == null || ContainsKey(_keys[i]))
                    continue;

                Add(_keys[i], _values[i]);
            }
        }
    }
}
