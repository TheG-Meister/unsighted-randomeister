using System;

namespace dev.gmeister.unsighted.randomeister.logging;

public class MovementLogger : IDisposable
{
	private StreamWriter stream;

    private string? currentLocation;

	public MovementLogger(string path)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		this.stream = new StreamWriter(path, true);
	}

	private string BoolToString(bool value)
	{
		return value ? "1" : "";
	}

	public void SetLocation(string location, bool transition, bool teleport)
	{
		if (currentLocation != null && currentLocation != location)
        {
			List<string> printItemNames = new();

			List<string> weaponNames = PseudoSingleton<Helpers>.instance.GetPlayerData().currentWeapons.Select(data => data.equipmentName).ToList();
			printItemNames.AddRange(weaponNames);

			List<string> itemNames = PseudoSingleton<Helpers>.instance.GetPlayerData().playerItems.Select(data => data.itemName).ToList();
			if (itemNames.Contains("JumpBoots")) printItemNames.Add("JumpBoots");
            if (itemNames.Contains("Spinner")) printItemNames.Add("Spinner");

            string items = string.Join(",", printItemNames);
			string data = string.Join(",", PseudoSingleton<Helpers>.instance.GetPlayerData().dataStrings);

            List<string> fields = new() { currentLocation, location, BoolToString(transition), BoolToString(teleport), items, data};
			
			this.stream.WriteLine(string.Join("\t", fields));
			this.stream.Flush();
        }
        currentLocation = location;
    }

    public void Dispose()
    {
		this.stream.Dispose();
    }
}
