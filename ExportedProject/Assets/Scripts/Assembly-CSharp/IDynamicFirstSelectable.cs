using UnityEngine.UI;

public interface IDynamicFirstSelectable
{
	Selectable FirstSelectable { get; }

	void EnsureBuilt();
}
