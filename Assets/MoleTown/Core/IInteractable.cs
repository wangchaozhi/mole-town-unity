using UnityEngine;

namespace MoleTown
{
    public interface IInteractable
    {
        Vector3 InteractPosition { get; }
        float InteractDistance { get; }
        string HoverHint { get; }
        void Interact();
    }
}
