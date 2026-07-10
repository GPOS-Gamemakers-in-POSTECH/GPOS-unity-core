using System;
using GPOS.Core.Collections;
using NUnit.Framework;
using UnityEngine;

namespace GPOS.Core.Tests
{
    public class SerializableDictionaryTests
    {
        [Serializable]
        private class StringIntDictionary : SerializableDictionary<string, int> { }

        [Test]
        public void JsonUtility_직렬화_왕복_후_내용이_보존된다()
        {
            var original = new StringIntDictionary { ["a"] = 1, ["b"] = 2 };

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<StringIntDictionary>(json);

            Assert.AreEqual(2, restored.Count);
            Assert.AreEqual(1, restored["a"]);
            Assert.AreEqual(2, restored["b"]);
        }

        [Test]
        public void 역직렬화_시_중복_키는_첫_값만_유지한다()
        {
            var dict = new StringIntDictionary { ["a"] = 1 };

            // 직렬화 리스트에 중복 키가 있는 상황을 흉내냅니다.
            dict.OnBeforeSerialize();
            string json = JsonUtility.ToJson(dict).Replace("\"_keys\":[\"a\"]", "\"_keys\":[\"a\",\"a\"]")
                                                  .Replace("\"_values\":[1]", "\"_values\":[1,9]");
            var restored = JsonUtility.FromJson<StringIntDictionary>(json);

            Assert.AreEqual(1, restored.Count);
            Assert.AreEqual(1, restored["a"]);
        }
    }
}
