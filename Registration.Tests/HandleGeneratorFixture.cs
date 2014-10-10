using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Conference.Common.Utils;
using NUnit.Framework;

namespace Registration.Tests {
    [TestFixture]
    public class HandleGeneratorFixture {
        [Test]
        public void When_generationg_handle_then_generates_requested_length() {
            var handle = HandleGenerator.Generate(5);

            Assert.AreEqual(5, handle.Length);
        }

        [Test]
        public void When_generating_handles_then_generates_different_values() {
            Assert.AreNotEqual(HandleGenerator.Generate(5), HandleGenerator.Generate(5));
        }

        [Test]
        public void Is_thread_safe() {
            var list = new ConcurrentBag<string>();

            Parallel.For(0, 10000, i => list.Add(HandleGenerator.Generate(6)));

            Assert.AreEqual(10000, list.Count());
        }
        
        [Test]
        public void Should_generate_distinct_handles() {
            var list = new ConcurrentBag<string>();

            Parallel.For(0, 10000, i => list.Add(HandleGenerator.Generate(100)));

            Assert.AreEqual(10000, list.Distinct().Count());
        }
    }
}