using System;
using System.Linq;
using Ibinimator.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ibinimator.Test
{
    [TestClass]
    public class EnumerableTests
    {
        [TestMethod]
        public void Split()
        {
            var test = Enumerable.Range(1, 20);
            var value = test.Split((i, _) => i % 4 == 0).Select(x => x.ToArray()).ToArray();
        }
    }
}
