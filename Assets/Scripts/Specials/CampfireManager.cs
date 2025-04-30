using System.Collections.Generic;
using UnityEngine;

public class CampfireManager : MonoBehaviour
{
    public static CampfireManager Instance { get; private set; }

    public List<Campfire> campfires = new List<Campfire>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        float totalTemperature = 0f;

        foreach (var campfire in campfires)
        {
            if (campfire.active)
            {
                float distance = Vector3.Distance(campfire.transform.position, Player.Instance.transform.position);

                if (distance < 2)
                {
                    totalTemperature += (2 - distance) / 2 * 0.1f;
                }
            }
        }

        Player.Instance.environmentTemperature = Mathf.Min(totalTemperature, 0.1f);
    }
}