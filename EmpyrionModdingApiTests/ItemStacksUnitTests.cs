using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmpyrionModdingApiTests
{
    [TestClass]
    public class ItemStacksUnitTests
    {
        [TestMethod]
        public void TestAddStack()
        {
            var testee = new SharedCode.ItemStacks();

            // test overflow on adding stacks
            testee.AddStack(new SharedCode.ItemStack(1, (int.MaxValue - 10)));
            testee.AddStack(new SharedCode.ItemStack(1, 20));

            Assert.AreEqual(2, testee.Count);
            Assert.AreEqual(int.MaxValue, testee[0].Amount);
            Assert.AreEqual(10, testee[1].Amount);
        }
    }
}
