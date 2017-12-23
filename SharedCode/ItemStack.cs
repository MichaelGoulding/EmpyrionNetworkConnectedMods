using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmpyrionModApi
{
    public class ItemStack
    {
        public int Id { get; set; }
        public int Amount { get; set; }

        public ItemStack()
        {

        }

        public ItemStack(int id, int amount)
        {
            this.Id = id;
            this.Amount = amount;
        }

        public ItemStack(Eleon.Modding.ItemStack itemStack)
        {
            this.Id = itemStack.id;
            this.Amount = itemStack.count;
        }

        public ItemStack Clone()
        {
            return new ItemStack(Id, Amount);
        }
    }

    public class ItemStacks : List<ItemStack>
    {
        public ItemStacks()
        {
        }

        public ItemStacks(Eleon.Modding.ItemStack[] items)
        {
            if (items != null)
            {
                foreach (var newItemStack in items)
                {
                    AddStack(new ItemStack(newItemStack));
                }
            }
        }

        public void AddStack(ItemStack newItemStack)
        {
            foreach (var itemStack in this)
            {
                if( itemStack.Id == newItemStack.Id)
                {
                    var newTotal = (long)itemStack.Amount + newItemStack.Amount;
                    if (newTotal > int.MaxValue)
                    {
                        itemStack.Amount = int.MaxValue;
                        newItemStack = new ItemStack(newItemStack.Id, (int)(newTotal - int.MaxValue));
                        break;
                    }
                    else
                    {
                        itemStack.Amount = (int)newTotal;
                        return;
                    }
                }
            }

            // If we get here, make a new stack in the list.
            this.Add(newItemStack);
        }

        public void AddStacks(ItemStacks itemStacks)
        {
            foreach (var itemStack in itemStacks)
            {
                AddStack(itemStack.Clone());
            }
        }

        public List<Eleon.Modding.ItemStack> ToEleonList()
        {
            var result = new List<Eleon.Modding.ItemStack>(this.Count);

            foreach (var itemStack in this)
            {
                result.Add(new Eleon.Modding.ItemStack(itemStack.Id, itemStack.Amount));
            }

            return result;
        }

        public Eleon.Modding.ItemStack[] ToEleonArray()
        {
            var result = new Eleon.Modding.ItemStack[this.Count];

            CopyFromThisToEleonArray(result);

            return result;
        }

        private void CopyFromThisToEleonArray(Eleon.Modding.ItemStack[] result)
        {
            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = new Eleon.Modding.ItemStack(this[i].Id, this[i].Amount);
            }
        }

        // Grab the first 35 stacks
        public Eleon.Modding.ItemStack[] ExtractOutForItemExchange()
        {
            const int MaxItemExchangeCount = 35;

            var result = new Eleon.Modding.ItemStack[Math.Min(this.Count, MaxItemExchangeCount)];

            CopyFromThisToEleonArray(result);

            this.RemoveRange(0, result.Length);

            return result;
        }
    }
}
