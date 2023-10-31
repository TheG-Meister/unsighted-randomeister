using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.core;

public class Constants
{

    //Plugin constants
    public const string GUID = "dev.gmeister.unsighted.randomeister";
    public const string NAME = "Unsighted Randomeister";
    public const string VERSION = "0.2.0";

    //Slot constants
    public const int MODES = 3;
    public const int PAGES_PER_MODE = 2;
    public const int SLOTS_PER_PAGE = 3;

    public const int STORY_MODE = 0;

    //Item pool constants
    public const string VANILLA_POOL = "Vanilla";
    public const string ALMOST_ALL_ITEMS_POOL = "Almost every item";

    //Directory structure constants
    public const string PATH_DEFAULT = "unsighted-randomeister/";
    public const string PATH_FILE_DATA = "saves/";
    public const string PATH_CHEST_LOGS = "chest/";
    public const string PATH_LOGS = "logs/";

    public const char CHEST_ID_SEPARATOR = '_';
    public const char SCENE_TRANSITION_ID_SEPARATOR = '_';

}
