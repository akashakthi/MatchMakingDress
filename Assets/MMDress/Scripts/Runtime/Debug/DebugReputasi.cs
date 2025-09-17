using UnityEngine;
using MMDress.Runtime.Reputation;

public sealed class RepDebugKeys : MonoBehaviour
{
    [SerializeField] private ReputationService rep;
    private void Awake() { if (!rep) rep = FindObjectOfType<ReputationService>(true); }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightBracket)) rep?.AddPercent(+1f);
        if (Input.GetKeyDown(KeyCode.LeftBracket)) rep?.AddPercent(-1f);
    }
}
