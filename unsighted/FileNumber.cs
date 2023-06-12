using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static dev.gmeister.unsighted.randomeister.core.Constants;

namespace dev.gmeister.unsighted.randomeister.unsighted;

public class FileNumber
{

    private readonly int number;

    public FileNumber(int number)
    {
        this.number = number;
    }

    public FileNumber(int index, int mode)
    {
        this.number = index % SLOTS_PER_PAGE + mode * SLOTS_PER_PAGE + (int) Math.Floor((double) index / SLOTS_PER_PAGE) * SLOTS_PER_PAGE * MODES;
    }

    public int Get() { return number; }

    public int Index()
    {
        return this.number % SLOTS_PER_PAGE + SLOTS_PER_PAGE * this.Page();
    }

    public int PageIndex()
    {
        return this.number % SLOTS_PER_PAGE;
    }

    public int Mode()
    {
        return (int) Math.Floor((double) this.number / SLOTS_PER_PAGE) % SLOTS_PER_PAGE;
    }

    public bool IsStory()
    {
        return this.Mode() == STORY_MODE;
    }

    public int Page()
    {
        return (int) Math.Floor((double) this.number / (MODES * SLOTS_PER_PAGE));
    }

    public bool IsValid()
    {
        int page = this.Page();
        return page >= 0 && page < PAGES_PER_MODE;
    }

}
