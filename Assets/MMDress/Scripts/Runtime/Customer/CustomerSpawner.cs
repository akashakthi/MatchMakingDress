using UnityEngine;
using MMDress.Gameplay;
using MMDress.Core;

namespace MMDress.Customer
{
    public class CustomerSpawner : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private GameObject customerPrefab;

        private void Start()
        {
            if (!customerPrefab || !spawnPoint)
            {
                Debug.LogWarning("[MMDress] Spawner missing prefab or spawnPoint.");
                return;
            }

            var go = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
            var c = go.GetComponent<CustomerController>();
            ServiceLocator.Events.Publish(new CustomerSpawned(c));
        }
    }
}
