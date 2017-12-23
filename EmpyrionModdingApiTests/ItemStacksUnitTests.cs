using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmpyrionModApiTests
{
    [TestClass]
    public class ItemStacksUnitTests
    {
        [TestMethod]
        public void TestAddStackOverflow()
        {
            var testee = new EmpyrionModApi.ItemStacks();

            // test overflow on adding stacks
            testee.AddStack(new EmpyrionModApi.ItemStack(1, (int.MaxValue - 10)));
            testee.AddStack(new EmpyrionModApi.ItemStack(1, 20));

            Assert.AreEqual(2, testee.Count);
            Assert.AreEqual(int.MaxValue, testee[0].Amount);
            Assert.AreEqual(10, testee[1].Amount);
        }

        [TestMethod]
        public void TestAddStackReferences()
        {
            var toAdd = new EmpyrionModApi.ItemStacks();
            toAdd.AddStack(new EmpyrionModApi.ItemStack(1, 1));

            var testee = new EmpyrionModApi.ItemStacks();
            testee.AddStacks(toAdd);

            Assert.AreEqual(1, testee.Count);
            Assert.AreEqual(1, testee[0].Amount);
            Assert.AreEqual(1, toAdd[0].Amount);

            testee.AddStacks(toAdd);

            Assert.AreEqual(1, testee.Count);
            Assert.AreEqual(2, testee[0].Amount);
            Assert.AreEqual(1, toAdd[0].Amount);
        }
    }
}
