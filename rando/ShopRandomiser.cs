using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.rando;

public class ShopRandomiser
{

    public static readonly List<ItemPriceRange> itemPriceRanges = ReadItemPriceRanges();

    public class ItemPriceRange
    {
        public string item;
        public int minPrice;
        public int maxPrice;
    }

    public static List<ItemPriceRange> ReadItemPriceRanges()
    {
        List<ItemPriceRange> result = new();

        List<string> rawLines = Properties.Resources.ItemPrices.Replace("\r", "").Split('\n').ToList();
        rawLines.RemoveAll(line => string.IsNullOrEmpty(line));

        foreach (string line in rawLines)
        {
            string[] split = line.Split('\t');
            if (split.Length != 3) throw new ApplicationException("A line did not have 3 fields");

            ItemPriceRange priceRange = new()
            {
                item = split[0],
                minPrice = int.Parse(split[1]),
                maxPrice = int.Parse(split[2])
            };

            result.Add(priceRange);
        }

        return result;
    }

    public Dictionary<string, float> RandomiseItemPrices(Random random)
    {
        Dictionary<string, float> result = new();

        foreach (ItemPriceRange priceRange in itemPriceRanges)
        {
            result.Add(priceRange.item, priceRange.minPrice + random.Next(priceRange.maxPrice - priceRange.minPrice + 1));
        }

        return result;
    }

    public Dictionary<string, List<string>> RandomiseShopListings()
    {
        Dictionary<string, List<string>> result = new();



        return result;
    }

}
