using UnityEngine;

public static class SECTR_Constants
{
	public enum ReparentingMode
	{
		Bounds = 0,
		Position = 1
	}

	public const string MAJOR_VERSION = "1";

	public const string MINOR_VERSION = "4.0";

	public static readonly Color UI_SEPARATOR_LINE_COLOR = new Color(0.455f, 0.455f, 0.455f);

	public const string GAIA_SPAWN_GROUP = "Gaia_Spawns";

	public const string PATH_AudioHUDGraphMaterial = "Scripts/Audio/Assets/AudioHUD_Graph.mat";

	public const string PATH_VisGizmoMaterial = "Scripts/Vis/Assets/FrustumDebug.mat";

	public const string PATH_AudioIcons = "Scripts/Audio/Editor/Icons/";

	public const string GX_ABOUT = "\nSECTR is a suite of modules for Unity that allows you to build the best looking, sounding, and most efficient games possible, all by taking advantage of the structure already present in your game world. If you want to stream an open world, bring a huge game to mobile, or take advantage of the latest techniques in audio occlusion and propagation, SECTR is your solution.\n\nMain Features\n - SECTR CORE: Sector Creation Kit\n - SECTR AUDIO: Immersive Spatial Audio\n - SECTR STREAM: Seamless Scene Streaming\n - SECTR VIS: Dynamic Occlusion Culling\n\n - SECTR COMPLETE: Contains all the packages\n";
}
