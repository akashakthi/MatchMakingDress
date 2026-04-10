using UnityEngine;

public class AutoResetData : MonoBehaviour
{
    void Awake()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("DATA DI RESET OTOMATIS");
    }
}