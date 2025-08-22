using UnityEngine;
using MMDress.Core;
using MMDress.Gameplay;

namespace MMDress.Customer
{
    [RequireComponent(typeof(Collider2D))]
    public class CustomerController : MonoBehaviour, IClickable
    {
        public Character.CharacterOutfitController Outfit { get; private set; }

        private void Awake()
        {
            Outfit = GetComponentInChildren<Character.CharacterOutfitController>();
        }

        public void OnClick()
        {
            ServiceLocator.Events.Publish(new CustomerSelected(this));
        }
    }
}
