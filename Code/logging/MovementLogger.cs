using System;

namespace dev.gmeister.unsighted.randomeister.logging;

public class MovementLogger
{
    private string? currentLocation;

	public MovementLogger()
	{

	}

	private string BoolToString(bool value)
	{
		return value ? "1" : "";
	}

	public void SetLocation(string location, bool transition, bool teleport)
	{
		if (currentLocation != null && currentLocation != location)
        {
			List<string> fields = new() { currentLocation, location, BoolToString(transition), BoolToString(teleport) };
			Plugin.Instance.GetLogger().LogInfo(string.Join("\t", fields));
        }
        currentLocation = location;
    }

}
