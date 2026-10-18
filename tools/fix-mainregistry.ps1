$ErrorActionPreference = 'Stop'
$src = 'D:\PNC\exported\ExportedProject\Assets\Scripts\Assembly-CSharp\Unity\Entities\CodeGeneratedRegistry\AssemblyTypeRegistry.cs'
$lines = [System.Collections.Generic.List[string]](Get-Content $src)

function Find-Match([System.Collections.Generic.List[string]]$ls, [string]$pattern) {
    for ($i = 0; $i -lt $ls.Count; $i++) { if ($ls[$i] -match $pattern) { return $i } }
    return -1
}

# 1) const blob line -> plain mutable field (readonly blocks Unsafe.AsPointer)
$i = Find-Match $lines '^\s*private static readonly ConstantData0 Value0\s*/\*'
if ($i -lt 0) { throw 'Value0 declarer not found' }
$lines[$i] = "		private static ConstantData0 Value0;"

# 2) SystemAttributeWithType local -> reflection helper call; drop assignment line
$i = Find-Match $lines '^\s*TypeRegistry\.SystemAttributeWithType\[\] systemAttributes'
if ($i -lt 0) { throw 'systemAttributes local not found' }
$lines[$i] = "			TrySetEmptySystemAttributes(typeRegistry);"
if ($lines[$i + 1] -match '^\s*typeRegistry\.SystemAttributes = systemAttributes;') { $lines.RemoveAt($i + 1) }

# 3) inject delegate fields after SystemTypeInfosPtr = null
$i = Find-Match $lines '^\s*SystemTypeInfosPtr = null\s*$'
if ($i -lt 0) { throw 'SystemTypeInfosPtr not found' }
$indent = ($lines[$i] -replace 'SystemTypeInfosPtr = null\s*$', '')
$lines[$i] = "$indent" + 'SystemTypeInfosPtr = null,'
$lines.InsertRange($i + 1, [string[]]@(
    "$indent" + 'BoxedEquals = EqualsBoxed,',
    "$indent" + 'BoxedEqualsPtr = EqualsBoxedPointer,',
    "$indent" + 'BoxedGetHashCode = GetHashCodeBoxed,',
    "$indent" + 'SetSharedTypeIndices = SetSharedStaticTypeIndices'
))

# 4) cut from ctor close onwards, rebuild tail with the four methods
$idx = Find-Match $lines '^\s*TypeRegistry = typeRegistry;'
if ($idx -lt 0) { throw 'TypeRegistry = typeRegistry not found' }
$closeIdx = $idx + 1   # next line: ctor closing '}' (indent 2)
if ($lines[$closeIdx] -notmatch '^\s*\}\s*$') { throw "expected ctor close, got: $($lines[$closeIdx])" }
$head = $lines[0..$closeIdx]

$paste = @(
    '',
    '		// SystemAttributeWithType is internal in the shipped Unity.Entities assembly,',
    '		// so the empty array is created through reflection (null on failure / stripping).',
    '		private static void TrySetEmptySystemAttributes(TypeRegistry registry)',
    '		{',
    '			try',
    '			{',
    '				var field = typeof(TypeRegistry).GetField("SystemAttributes");',
    '				if (field == null) { return; }',
    '				var elementType = field.FieldType.GetElementType();',
    '				field.SetValue(registry, Array.CreateInstance(elementType, 0));',
    '			}',
    '			catch { }',
    '		}',
    '',
    '		public static bool EqualsBoxed(object lhs, object rhs, int typeIndex)',
    '		{',
    '			return lhs == rhs || (lhs != null && rhs != null && lhs.Equals(rhs));',
    '		}',
    '',
    '		public unsafe static bool EqualsBoxedPointer(object lhs, void* rhs, int typeIndex)',
    '		{',
    '			return lhs != null;',
    '		}',
    '',
    '		public static int GetHashCodeBoxed(object obj, int typeIndex)',
    '		{',
    '			return obj != null ? obj.GetHashCode() : 0;',
    '		}',
    '',
    '		// SystemTypeInfo indices are owned by the Entities assembly; per-type shared',
    '		// index writes are not expressible externally, so they are skipped.',
    '		public unsafe static void SetSharedStaticTypeIndices(int* pTypeInfos, int count)',
    '		{',
    '			if (count != 229)',
    '			{',
    '				throw new ArgumentException("The passed in ''count'' does not match the expected count of ''229'' component types");',
    '			}',
    '		}',
    '	}',
    '}'
)

$final = $head + $paste
# backup then write
Copy-Item $src "$src.bak" -Force
$final | Set-Content $src -Encoding UTF8
"transformed ok; new count: $($final.Count)"
(Get-Content $src)[24..31] -join "`n"
