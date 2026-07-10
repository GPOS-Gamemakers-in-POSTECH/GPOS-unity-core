using GPOS.Core.Command;
using NUnit.Framework;

namespace GPOS.Core.Tests
{
    public class CommandInvokerTests
    {
        [Test]
        public void Execute_Undo_Redo_가_값을_올바르게_바꾼다()
        {
            var invoker = new CommandInvoker();
            int value = 0;

            invoker.Execute(new RelayCommand(() => value++, () => value--));
            Assert.AreEqual(1, value);

            invoker.Undo();
            Assert.AreEqual(0, value);

            invoker.Redo();
            Assert.AreEqual(1, value);
        }

        [Test]
        public void 새_커맨드_실행_시_Redo_스택이_비워진다()
        {
            var invoker = new CommandInvoker();
            int value = 0;

            invoker.Execute(new RelayCommand(() => value++, () => value--));
            invoker.Undo();
            Assert.IsTrue(invoker.CanRedo);

            invoker.Execute(new RelayCommand(() => value += 10, () => value -= 10));
            Assert.IsFalse(invoker.CanRedo);
        }

        [Test]
        public void 빈_스택에서_Undo_Redo_는_아무_일도_하지_않는다()
        {
            var invoker = new CommandInvoker();
            Assert.DoesNotThrow(() => invoker.Undo());
            Assert.DoesNotThrow(() => invoker.Redo());
        }

        [Test]
        public void Clear_는_모든_히스토리를_지운다()
        {
            var invoker = new CommandInvoker();
            invoker.Execute(new RelayCommand(() => { }));
            invoker.Undo();

            invoker.Clear();

            Assert.IsFalse(invoker.CanUndo);
            Assert.IsFalse(invoker.CanRedo);
        }

        [Test]
        public void null_커맨드는_무시된다()
        {
            var invoker = new CommandInvoker();
            invoker.Execute(null);
            Assert.IsFalse(invoker.CanUndo);
        }
    }
}
