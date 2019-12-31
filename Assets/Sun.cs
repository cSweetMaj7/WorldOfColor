using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun : MonoBehaviour
{
    public bool spinSun = true;
    public bool advanceIntensity = true;
    public float spinAdvanceRate = 1.0f;
    public float intensityAdvanceRate = 0.05f;
    public bool advanceCameraXRot = true;
    public Transform cameraTransform;
    public float cameraXRotRate = 0.05f;

    public float stopAtCameraRot = -1.0f;

    Light sunLight;

    // Start is called before the first frame update
    void Start()
    {
        sunLight = gameObject.GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Mathf.Abs(cameraTransform.eulerAngles.x - stopAtCameraRot) < 1)
        {
            return;
        }

        if(advanceCameraXRot)
        {
            cameraTransform.eulerAngles = new Vector3(
            cameraTransform.eulerAngles.x - cameraXRotRate,
            cameraTransform.eulerAngles.y,
            cameraTransform.eulerAngles.z
            );
        }

        if(spinSun)
        {
            gameObject.transform.eulerAngles = new Vector3(
            gameObject.transform.eulerAngles.x - spinAdvanceRate,
            gameObject.transform.eulerAngles.y,
            gameObject.transform.eulerAngles.z
            );
        }

        if(advanceIntensity)
        {
            if(sunLight)
            {
                sunLight.intensity += intensityAdvanceRate;
            } else
            {
                Debug.LogError("Couldn't get sun light object");
            }
        }
    }
}
