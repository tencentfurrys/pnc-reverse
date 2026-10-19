using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CMF;
using UnityEngine.Scripting;

namespace Unity.Entities.CodeGeneratedRegistry
{
	[Preserve]
	public class AssemblyTypeRegistry
	{
		[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 3432)]
		public struct ConstantData0
		{
		}

		public static readonly TypeRegistry TypeRegistry;

		// Original binary constant blob (3432 bytes) was lost in decompilation;
		// zeroed memory is used instead. Kept mutable so Unsafe.AsPointer is valid.
		private static ConstantData0 Value0;

		unsafe static AssemblyTypeRegistry()
		{
			TypeRegistry typeRegistry = new TypeRegistry
			{
				AssemblyName = "Assembly-CSharp-firstpass",
				HasAssemblyWideDisableAutoCreation = false,
				TypeInfosPtr = (TypeManager.TypeInfo*)Unsafe.AsPointer(ref Value0),
				TypeInfosCount = 33,
				Types = new Type[33]
				{
					typeof(ClickToMoveController),
					typeof(CeilingDetector),
					typeof(AlignRigidbodyToTarget),
					typeof(AnimationControl),
					typeof(AudioControl),
					typeof(TurnTowardCameraDirection),
					typeof(TurnTowardControllerVelocity),
					typeof(TurnTowardTransformDirection),
					typeof(CameraController),
					typeof(CameraDistanceRaycaster),
					typeof(ThirdPersonCameraController),
					typeof(AdvancedWalkerController),
					typeof(Controller),
					typeof(SidescrollerController),
					typeof(SimpleWalkerController),
					typeof(Mover),
					typeof(FlipAtRightAngle),
					typeof(GravityTunnel),
					typeof(MovingPlatform),
					typeof(TriggerArea),
					typeof(CameraInput),
					typeof(CameraJoystickInput),
					typeof(CameraMouseInput),
					typeof(CharacterInput),
					typeof(CharacterJoystickInput),
					typeof(CharacterKeyboardInput),
					typeof(DemoMenu),
					typeof(DisableShadows),
					typeof(FPSCounter),
					typeof(MouseCursorLock),
					typeof(RotateObject),
					typeof(SmoothPosition),
					typeof(SmoothRotation)
				},
				TypeNames = new string[33]
				{
					"ClickToMoveController", "CeilingDetector", "AlignRigidbodyToTarget", "CMF.AnimationControl", "CMF.AudioControl", "CMF.TurnTowardCameraDirection", "CMF.TurnTowardControllerVelocity", "CMF.TurnTowardTransformDirection", "CMF.CameraController", "CMF.CameraDistanceRaycaster",
					"CMF.ThirdPersonCameraController", "CMF.AdvancedWalkerController", "CMF.Controller", "CMF.SidescrollerController", "CMF.SimpleWalkerController", "CMF.Mover", "CMF.FlipAtRightAngle", "CMF.GravityTunnel", "CMF.MovingPlatform", "CMF.TriggerArea",
					"CMF.CameraInput", "CMF.CameraJoystickInput", "CMF.CameraMouseInput", "CMF.CharacterInput", "CMF.CharacterJoystickInput", "CMF.CharacterKeyboardInput", "CMF.DemoMenu", "CMF.DisableShadows", "CMF.FPSCounter", "CMF.MouseCursorLock",
					"CMF.RotateObject", "CMF.SmoothPosition", "CMF.SmoothRotation"
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
				SystemTypes = new Type[0],
				SystemTypeNames = new string[0],
				SystemTypeSizes = new int[0],
				SystemTypeHashes = new long[0],
				BoxedEquals = EqualsBoxed,
				BoxedEqualsPtr = EqualsBoxedPointer,
				BoxedGetHashCode = GetHashCodeBoxed,
				SetSharedTypeIndices = SetSharedStaticTypeIndices
			};
			TrySetEmptySystemAttributes(typeRegistry);
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

		// SystemTypeInfo indices are owned by the Entities assembly; the per-type shared
		// index writes are not expressible externally, so they are skipped.
		public unsafe static void SetSharedStaticTypeIndices(int* pTypeInfos, int count)
		{
			if (count != 33)
			{
				throw new ArgumentException("The passed in 'count' does not match the expected count of '33' component types");
			}
		}
	}
}
