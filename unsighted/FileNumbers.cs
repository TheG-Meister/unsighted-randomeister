using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static dev.gmeister.unsighted.randomeister.core.Constants;

namespace dev.gmeister.unsighted.randomeister.unsighted;

public class FileNumbers
{

    public static int GetIndex(int slot)
    {
        int page = FileNumbers.GetPage(slot);
        return slot % SLOTS_PER_PAGE + SLOTS_PER_PAGE * page;
    }

    public static int GetPageIndex(int slot)
    {
        return slot % SLOTS_PER_PAGE;
    }

    public static int GetMode(int slot)
    {
        return (int)Math.Floor((double) slot / SLOTS_PER_PAGE) % SLOTS_PER_PAGE;
    }

    public static int GetPage(int slot)
    {
        return (int)Math.Floor((double) slot / (MODES * SLOTS_PER_PAGE));
    }

    public static int ToGameSlot(int index, int mode)
    {
        return index % SLOTS_PER_PAGE + mode * SLOTS_PER_PAGE + (int)Math.Floor((double)index / SLOTS_PER_PAGE) * SLOTS_PER_PAGE * MODES;
    }

}
