using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlobShadowDecal;
using DunGen.Adapters;
using EasyTextEffects.Samples;
using FronkonGames.Retro.NTSC;
using PixelCrushers.GridController;
using PixelCrushers.GridController.Glyphs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;
using UnityEngine.Scripting;

namespace Unity.Entities.CodeGeneratedRegistry
{
	[Preserve]
	public class AssemblyTypeRegistry
	{
		[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 23816)]
		public struct ConstantData0
		{
		}

		public static readonly TypeRegistry TypeRegistry;

		private static ConstantData0 Value0;

		unsafe static AssemblyTypeRegistry()
		{
			TypeRegistry typeRegistry = new TypeRegistry
			{
				AssemblyName = "Assembly-CSharp",
				HasAssemblyWideDisableAutoCreation = false,
				TypeInfosPtr = (TypeManager.TypeInfo*)Unsafe.AsPointer(ref Value0),
				TypeInfosCount = 229,
				Types = new Type[229]
				{
					typeof(SECTR_AudioDemoUI),
					typeof(SECTR_CompleteDemoUI),
					typeof(SECTR_CharacterMotor),
					typeof(SECTR_DemoUI),
					typeof(SECTR_FPController),
					typeof(SECTR_FPSController),
					typeof(SECTR_FogDisable),
					typeof(SECTR_GhostController),
					typeof(SECTR_TreeSpawner),
					typeof(SECTR_Wanderer),
					typeof(SECTR_StreamDemoUI),
					typeof(SECTR_VisDemoUI),
					typeof(SECTR_AudioBus),
					typeof(SECTR_AudioCue),
					typeof(SECTR_AudioEnvironment),
					typeof(SECTR_AudioEnvironmentTrigger),
					typeof(SECTR_AudioEnvironmentZone),
					typeof(SECTR_AudioSource),
					typeof(SECTR_AudioSystem),
					typeof(SECTR_CharacterAudio),
					typeof(SECTR_ComputeRMS),
					typeof(SECTR_DoorAudio),
					typeof(SECTR_ImpactAudio),
					typeof(SECTR_MusicTrigger),
					typeof(SECTR_PointSource),
					typeof(SECTR_PropagationSource),
					typeof(SECTR_RegionSource),
					typeof(SECTR_SplineSource),
					typeof(SECTR_StartMusic),
					typeof(SECTR_TriggerSource),
					typeof(SECTR_Door),
					typeof(SECTR_FloatingPointFix),
					typeof(SECTR_FloatingPointFixMember),
					typeof(SECTR_FloatingPointFixParticleSystem),
					typeof(SECTR_Hull),
					typeof(SECTR_Member),
					typeof(SECTR_Portal),
					typeof(SECTR_Sector),
					typeof(SECTR_SectorUtils),
					typeof(SECTR_Chunk),
					typeof(SECTR_ChunkRef),
					typeof(SECTR_GroupLoader),
					typeof(SECTR_Hibernator),
					typeof(SECTR_LightmapRef),
					typeof(SECTR_Loader),
					typeof(SECTR_LoadingDoor),
					typeof(SECTR_NeighborLoader),
					typeof(SECTR_RegionLoader),
					typeof(SECTR_StartLoader),
					typeof(SECTR_TriggerLoader),
					typeof(SECTR_Culler),
					typeof(SECTR_CullingCamera),
					typeof(SECTR_LOD),
					typeof(SECTR_Occluder),
					typeof(AnimationEventHelper),
					typeof(ArenaEnemySpawner),
					typeof(ArenaSpawnerTriggerHandler),
					typeof(ArenaSpawnGroup),
					typeof(ArenaSpawnPoint),
					typeof(AudioSettingsInitializer),
					typeof(Billboard),
					typeof(BreakableObject),
					typeof(CameraSwapTrigger),
					typeof(ArenaTriggerChest),
					typeof(CurrencyChest),
					typeof(CinematicCameraSwapTrigger),
					typeof(BrawlerEnemyAI),
					typeof(ChargingEnemyAI),
					typeof(EnemyAI),
					typeof(EnemyAttackDetector),
					typeof(EnemyGrabDetector),
					typeof(EnemyData),
					typeof(EnemyHealthBar),
					typeof(EnemyProjectile),
					typeof(GrabProjectile),
					typeof(MimicEnemy),
					typeof(ProjectileEnemyAI),
					typeof(SpinningEnemyAI),
					typeof(DragonEnemyAI),
					typeof(DragonAttackDetector),
					typeof(DragonGrabDetector),
					typeof(ProximityDragonEnemyAI),
					typeof(ProximityDragonAttackDetector),
					typeof(ProximityDragonGrabDetector),
					typeof(SweeperEnemySpawner),
					typeof(EnemySpawner),
					typeof(EnemySpawnerTriggerHandler),
					typeof(FPSLimiter),
					typeof(EnemyGalleryEntry),
					typeof(GalleryProgressManager),
					typeof(GalleryTabController),
					typeof(GalleryUnlockTrigger),
					typeof(GameOverScreen),
					typeof(GrabScreen),
					typeof(GrappleScreenobject),
					typeof(HealingCameraSwapTrigger),
					typeof(InputDeviceTracker),
					typeof(InputManager),
					typeof(PersistentEventSystem),
					typeof(NoteInteractable),
					typeof(PlayerInteractor),
					typeof(ItemDropper),
					typeof(LevelStatsTracker),
					typeof(LevelTransition),
					typeof(LoadingScreen),
					typeof(MainMenuManager),
					typeof(Navigator),
					typeof(BuffScreenEffect),
					typeof(CurrencyManager),
					typeof(FirstPersonController),
					typeof(HurtScreenEffect),
					typeof(InputSystemCameraInput),
					typeof(InputSystemCharacterInput),
					typeof(NavigatorSpawner),
					typeof(OverheatedScreenEffect),
					typeof(PlayerClass),
					typeof(PlayerClassInitializer),
					typeof(PlayerClassManager),
					typeof(PlayerStateSaver),
					typeof(PlayerStats),
					typeof(WeaponAnimationEventBridge),
					typeof(PlayerSpawnPoint),
					typeof(RunRestartController),
					typeof(SecretDoorBlock),
					typeof(SecretDoorButton),
					typeof(SettingsLoader),
					typeof(AllTagsTileConnectionRule),
					typeof(DunGenAStarIntegration),
					typeof(AudioTrigger),
					typeof(TriggerHandler),
					typeof(LadderClimber),
					typeof(PitfallTeleporter),
					typeof(PitfallTriggerForwarder),
					typeof(TrapCameraSwapTrigger),
					typeof(GasTrap),
					typeof(LingeringHeatCloud),
					typeof(SteamTrap),
					typeof(AlertPanel),
					typeof(ClassSelectionUI),
					typeof(ClassSelectionUI.EquipmentSlotHover),
					typeof(ClassUnlockSlotUI),
					typeof(ControllerRefocus),
					typeof(CurrencyDisplayUI),
					typeof(CurrencyProximityTrigger),
					typeof(DamageNumber),
					typeof(DamageNumberManager),
					typeof(DioramaGalleryUI),
					typeof(EnemyGalleryUI),
					typeof(EventTriggerSelectionBridge),
					typeof(ItemUnlockSlotUI),
					typeof(ItemUnlocksScreenUI),
					typeof(LevelEndScreen),
					typeof(MiniBossHealthBarUI),
					typeof(NoteViewer),
					typeof(OptionsMenu),
					typeof(OptionsTabs),
					typeof(PatronListData),
					typeof(PatronScroller),
					typeof(PeekScenesUI),
					typeof(RebindEntry),
					typeof(RebindMenu),
					typeof(UISelectionScroller),
					typeof(CompletionUnlock),
					typeof(ChestManager),
					typeof(ChestManagerInitializer),
					typeof(ChestSpawner),
					typeof(ChoiceItemPedestal),
					typeof(ChestItemPool),
					typeof(RerollPedestal),
					typeof(ShopItemPedestal),
					typeof(SimpleItemChest),
					typeof(SimpleItemPedestal),
					typeof(UnlockPedestal),
					typeof(ArmorData),
					typeof(ConsumableSystem),
					typeof(CurrencyPickup),
					typeof(GridInventorySystem),
					typeof(InventoryItem),
					typeof(InventoryItemPickup),
					typeof(InventorySlotUI),
					typeof(InventoryUI),
					typeof(PickupPoolManager),
					typeof(PauseMenuManager),
					typeof(BuffDebuffSystem),
					typeof(BuffDebuffUI),
					typeof(DualWieldingSystem),
					typeof(AudioPool),
					typeof(VFXPoolManager),
					typeof(CoyoteTimeTrinketEffect),
					typeof(CritExplosionTrinketEffect),
					typeof(EscalatingDamageTrinketEffect),
					typeof(ExplosiveProjectileTrinketEffect),
					typeof(GrabDamageBonusTrinketEffect),
					typeof(GuaranteedCritChestTrinketEffect),
					typeof(HeatScaledDamageTrinketEffect),
					typeof(HomingTrinketEffect),
					typeof(LifeStealTrinketEffect),
					typeof(LightBoostTrinketEffect),
					typeof(LuckyChestTrinketEffect),
					typeof(MaxHeatGrabImmunityTrinketEffect),
					typeof(MeleeProjectileTrinketEffect),
					typeof(OverheatDamageTrinketEffect),
					typeof(ReviveTrinketEffect),
					typeof(TrinketData),
					typeof(TrinketManager),
					typeof(TooltipIcons),
					typeof(TooltipStatRow),
					typeof(ItemUnlockManager),
					typeof(ItemUnlockPanel),
					typeof(TestItemUnlock),
					typeof(FloorSpike),
					typeof(FloorSpikeProjectile),
					typeof(Projectile),
					typeof(ImpactAudioPlayer),
					typeof(ProjectileData),
					typeof(ProjectileWeaponLauncher),
					typeof(WeaponData),
					typeof(InputGlyphDatabase),
					typeof(InputGlyphSet),
					typeof(InputPromptLabel),
					typeof(SequenceManager),
					typeof(NTSCDemo),
					typeof(Rotate),
					typeof(Swing),
					typeof(SECTRDoorwayLink),
					typeof(SECTRPortalCullingAdapter),
					typeof(OrbitBehaviour),
					typeof(SineBehaviour),
					typeof(ShadowDecal)
				},
				TypeNames = new string[229]
				{
					"SECTR_AudioDemoUI", "SECTR_CompleteDemoUI", "SECTR_CharacterMotor", "SECTR_DemoUI", "SECTR_FPController", "SECTR_FPSController", "SECTR_FogDisable", "SECTR_GhostController", "SECTR_TreeSpawner", "SECTR_Wanderer",
					"SECTR_StreamDemoUI", "SECTR_VisDemoUI", "SECTR_AudioBus", "SECTR_AudioCue", "SECTR_AudioEnvironment", "SECTR_AudioEnvironmentTrigger", "SECTR_AudioEnvironmentZone", "SECTR_AudioSource", "SECTR_AudioSystem", "SECTR_CharacterAudio",
					"SECTR_ComputeRMS", "SECTR_DoorAudio", "SECTR_ImpactAudio", "SECTR_MusicTrigger", "SECTR_PointSource", "SECTR_PropagationSource", "SECTR_RegionSource", "SECTR_SplineSource", "SECTR_StartMusic", "SECTR_TriggerSource",
					"SECTR_Door", "SECTR_FloatingPointFix", "SECTR_FloatingPointFixMember", "SECTR_FloatingPointFixParticleSystem", "SECTR_Hull", "SECTR_Member", "SECTR_Portal", "SECTR_Sector", "SECTR_SectorUtils", "SECTR_Chunk",
					"SECTR_ChunkRef", "SECTR_GroupLoader", "SECTR_Hibernator", "SECTR_LightmapRef", "SECTR_Loader", "SECTR_LoadingDoor", "SECTR_NeighborLoader", "SECTR_RegionLoader", "SECTR_StartLoader", "SECTR_TriggerLoader",
					"SECTR_Culler", "SECTR_CullingCamera", "SECTR_LOD", "SECTR_Occluder", "AnimationEventHelper", "ArenaEnemySpawner", "ArenaSpawnerTriggerHandler", "ArenaSpawnGroup", "ArenaSpawnPoint", "AudioSettingsInitializer",
					"Billboard", "BreakableObject", "CameraSwapTrigger", "ArenaTriggerChest", "CurrencyChest", "CinematicCameraSwapTrigger", "BrawlerEnemyAI", "ChargingEnemyAI", "EnemyAI", "EnemyAttackDetector",
					"EnemyGrabDetector", "EnemyData", "EnemyHealthBar", "EnemyProjectile", "GrabProjectile", "MimicEnemy", "ProjectileEnemyAI", "SpinningEnemyAI", "DragonEnemyAI", "DragonAttackDetector",
					"DragonGrabDetector", "ProximityDragonEnemyAI", "ProximityDragonAttackDetector", "ProximityDragonGrabDetector", "SweeperEnemySpawner", "EnemySpawner", "EnemySpawnerTriggerHandler", "FPSLimiter", "EnemyGalleryEntry", "GalleryProgressManager",
					"GalleryTabController", "GalleryUnlockTrigger", "GameOverScreen", "GrabScreen", "GrappleScreenobject", "HealingCameraSwapTrigger", "InputDeviceTracker", "InputManager", "PersistentEventSystem", "NoteInteractable",
					"PlayerInteractor", "ItemDropper", "LevelStatsTracker", "LevelTransition", "LoadingScreen", "MainMenuManager", "Navigator", "BuffScreenEffect", "CurrencyManager", "FirstPersonController",
					"HurtScreenEffect", "InputSystemCameraInput", "InputSystemCharacterInput", "NavigatorSpawner", "OverheatedScreenEffect", "PlayerClass", "PlayerClassInitializer", "PlayerClassManager", "PlayerStateSaver", "PlayerStats",
					"WeaponAnimationEventBridge", "PlayerSpawnPoint", "RunRestartController", "SecretDoorBlock", "SecretDoorButton", "SettingsLoader", "AllTagsTileConnectionRule", "DunGenAStarIntegration", "AudioTrigger", "TriggerHandler",
					"LadderClimber", "PitfallTeleporter", "PitfallTriggerForwarder", "TrapCameraSwapTrigger", "GasTrap", "LingeringHeatCloud", "SteamTrap", "AlertPanel", "ClassSelectionUI", "ClassSelectionUI+EquipmentSlotHover",
					"ClassUnlockSlotUI", "ControllerRefocus", "CurrencyDisplayUI", "CurrencyProximityTrigger", "DamageNumber", "DamageNumberManager", "DioramaGalleryUI", "EnemyGalleryUI", "EventTriggerSelectionBridge", "ItemUnlockSlotUI",
					"ItemUnlocksScreenUI", "LevelEndScreen", "MiniBossHealthBarUI", "NoteViewer", "OptionsMenu", "OptionsTabs", "PatronListData", "PatronScroller", "PeekScenesUI", "RebindEntry",
					"RebindMenu", "UISelectionScroller", "CompletionUnlock", "PixelCrushers.GridController.ChestManager", "PixelCrushers.GridController.ChestManagerInitializer", "PixelCrushers.GridController.ChestSpawner", "PixelCrushers.GridController.ChoiceItemPedestal", "PixelCrushers.GridController.ChestItemPool", "PixelCrushers.GridController.RerollPedestal", "PixelCrushers.GridController.ShopItemPedestal",
					"PixelCrushers.GridController.SimpleItemChest", "PixelCrushers.GridController.SimpleItemPedestal", "PixelCrushers.GridController.UnlockPedestal", "PixelCrushers.GridController.ArmorData", "PixelCrushers.GridController.ConsumableSystem", "PixelCrushers.GridController.CurrencyPickup", "PixelCrushers.GridController.GridInventorySystem", "PixelCrushers.GridController.InventoryItem", "PixelCrushers.GridController.InventoryItemPickup", "PixelCrushers.GridController.InventorySlotUI",
					"PixelCrushers.GridController.InventoryUI", "PixelCrushers.GridController.PickupPoolManager", "PixelCrushers.GridController.PauseMenuManager", "PixelCrushers.GridController.BuffDebuffSystem", "PixelCrushers.GridController.BuffDebuffUI", "PixelCrushers.GridController.DualWieldingSystem", "PixelCrushers.GridController.AudioPool", "PixelCrushers.GridController.VFXPoolManager", "PixelCrushers.GridController.CoyoteTimeTrinketEffect", "PixelCrushers.GridController.CritExplosionTrinketEffect",
					"PixelCrushers.GridController.EscalatingDamageTrinketEffect", "PixelCrushers.GridController.ExplosiveProjectileTrinketEffect", "PixelCrushers.GridController.GrabDamageBonusTrinketEffect", "PixelCrushers.GridController.GuaranteedCritChestTrinketEffect", "PixelCrushers.GridController.HeatScaledDamageTrinketEffect", "PixelCrushers.GridController.HomingTrinketEffect", "PixelCrushers.GridController.LifeStealTrinketEffect", "PixelCrushers.GridController.LightBoostTrinketEffect", "PixelCrushers.GridController.LuckyChestTrinketEffect", "PixelCrushers.GridController.MaxHeatGrabImmunityTrinketEffect",
					"PixelCrushers.GridController.MeleeProjectileTrinketEffect", "PixelCrushers.GridController.OverheatDamageTrinketEffect", "PixelCrushers.GridController.ReviveTrinketEffect", "PixelCrushers.GridController.TrinketData", "PixelCrushers.GridController.TrinketManager", "PixelCrushers.GridController.TooltipIcons", "PixelCrushers.GridController.TooltipStatRow", "PixelCrushers.GridController.ItemUnlockManager", "PixelCrushers.GridController.ItemUnlockPanel", "PixelCrushers.GridController.TestItemUnlock",
					"PixelCrushers.GridController.FloorSpike", "PixelCrushers.GridController.FloorSpikeProjectile", "PixelCrushers.GridController.Projectile", "PixelCrushers.GridController.ImpactAudioPlayer", "PixelCrushers.GridController.ProjectileData", "PixelCrushers.GridController.ProjectileWeaponLauncher", "PixelCrushers.GridController.WeaponData", "PixelCrushers.GridController.Glyphs.InputGlyphDatabase", "PixelCrushers.GridController.Glyphs.InputGlyphSet", "PixelCrushers.GridController.Glyphs.InputPromptLabel",
					"EasyTextEffects.Samples.SequenceManager", "FronkonGames.Retro.NTSC.NTSCDemo", "FronkonGames.Retro.NTSC.Rotate", "FronkonGames.Retro.NTSC.Swing", "DunGen.Adapters.SECTRDoorwayLink", "DunGen.Adapters.SECTRPortalCullingAdapter", "BlobShadowDecal.OrbitBehaviour", "BlobShadowDecal.SineBehaviour", "BlobShadowDecal.ShadowDecal"
				},
				EntityOffsetsPtr = null,
				EntityOffsetsCount = 0,
				BlobAssetReferenceOffsetsPtr = null,
				BlobAssetReferenceOffsetsCount = 0,
				UnityObjectReferenceOffsetsPtr = null,
				UnityObjectReferenceOffsetsCount = 0,
				WeakAssetReferenceOffsetsPtr = null,
				WeakAssetReferenceOffsetsCount = 0,
				WriteGroups = new Type[0],
				SystemTypeInfosPtr = null,
				BoxedEquals = EqualsBoxed, 				BoxedEqualsPtr = EqualsBoxedPointer, 				BoxedGetHashCode = GetHashCodeBoxed, 				SetSharedTypeIndices = SetSharedStaticTypeIndices
			};
			TrySetEmptySystemAttributes(typeRegistry);
			typeRegistry.SystemTypes = new Type[0];
			typeRegistry.SystemTypeNames = new string[0];
			typeRegistry.SystemTypeSizes = new int[0];
			typeRegistry.SystemTypeHashes = new long[0];
			typeRegistry.SetSharedTypeIndices = SetSharedStaticTypeIndices;
			TypeRegistry = typeRegistry;
		}

		// SystemAttributeWithType is internal in the shipped Unity.Entities assembly,
		// so the empty array is created through reflection (null on failure / stripping).
		private static void TrySetEmptySystemAttributes(TypeRegistry registry)
		{
			try
			{
				var field = typeof(TypeRegistry).GetField("SystemAttributes");
				if (field == null) { return; }
				var elementType = field.FieldType.GetElementType();
				field.SetValue(registry, Array.CreateInstance(elementType, 0));
			}
			catch { }
		}

		public static bool EqualsBoxed(object lhs, object rhs, int typeIndex)
		{
			return lhs == rhs || (lhs != null && rhs != null && lhs.Equals(rhs));
		}

		public unsafe static bool EqualsBoxedPointer(object lhs, void* rhs, int typeIndex)
		{
			return lhs != null;
		}

		public static int GetHashCodeBoxed(object obj, int typeIndex)
		{
			return obj != null ? obj.GetHashCode() : 0;
		}

		// SystemTypeInfo indices are owned by the Entities assembly; per-type shared
		// index writes are not expressible externally, so they are skipped.
		public unsafe static void SetSharedStaticTypeIndices(int* pTypeInfos, int count)
		{
			if (count != 229)
			{
				throw new ArgumentException("The passed in 'count' does not match the expected count of '229' component types");
			}
		}
	}
}
