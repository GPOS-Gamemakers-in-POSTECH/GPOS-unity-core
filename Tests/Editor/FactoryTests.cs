using System.Collections.Generic;
using GPOS.Core.Factory;
using NUnit.Framework;

namespace GPOS.Core.Tests
{
    public class FactoryTests
    {
        private enum Kind { A, B }

        [Test]
        public void 등록한_키로_생성할_수_있다()
        {
            var factory = new Factory<Kind, string>();
            factory.Register(Kind.A, () => "apple");

            Assert.AreEqual("apple", factory.Create(Kind.A));
        }

        [Test]
        public void 등록하지_않은_키는_예외를_던진다()
        {
            var factory = new Factory<Kind, string>();
            Assert.Throws<KeyNotFoundException>(() => factory.Create(Kind.B));
        }

        [Test]
        public void TryCreate_는_예외_없이_실패를_알린다()
        {
            var factory = new Factory<Kind, string>();
            Assert.IsFalse(factory.TryCreate(Kind.A, out _));

            factory.Register(Kind.A, () => "apple");
            Assert.IsTrue(factory.TryCreate(Kind.A, out string product));
            Assert.AreEqual("apple", product);
        }

        [Test]
        public void 같은_키로_다시_등록하면_교체된다()
        {
            var factory = new Factory<Kind, string>();
            factory.Register(Kind.A, () => "old");
            factory.Register(Kind.A, () => "new");

            Assert.AreEqual("new", factory.Create(Kind.A));
        }

        [Test]
        public void Unregister_후에는_생성할_수_없다()
        {
            var factory = new Factory<Kind, string>();
            factory.Register(Kind.A, () => "apple");

            Assert.IsTrue(factory.Unregister(Kind.A));
            Assert.IsFalse(factory.IsRegistered(Kind.A));
        }

        [Test]
        public void null_생성자_등록은_예외를_던진다()
        {
            var factory = new Factory<Kind, string>();
            Assert.Throws<System.ArgumentNullException>(() => factory.Register(Kind.A, null));
        }
    }
}
