using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridFountain : MonoBehaviour
{

    public Color fountainColor; // color of both the light and emitted particles
    public Light gridLight; // light that's shined on the fountainBase
    public ParticleSystem fountainParticles;
    public GameObject fountainBase;

    public int row = -1;
    public int col = -1;

    private bool active;
    private bool advanceGravity = false;
    private float advanceGravityBy;
    private float targetGravity;
    private Color startColor;
    private Color targetColor;
    private float lerpDuration;
    private float lerped;

    // Start is called before the first frame update
    void Start()
    {
        if(int.TryParse(transform.parent.name, out col) && int.TryParse(transform.parent.parent.name, out row))
        {
            if(row > -1 && col > -1)
            {
                // Debug.Log("Added fountain at row " + row.ToString() + " col " + col.ToString());
            } else
            {
                Debug.LogError("Coordinates parse error 1");
            }
        } else
        {
            Debug.LogError("Error");
        }
        if(this.col < 0 || this.row < 0)
        {
            Debug.LogError("erro");
        }
    }

    // this works but isn't the best, usually should use on() with a duration or off()
    public void toggle()
    {
        active = !active;

        if(fountainParticles)
        {
            if (active)
            {
                fountainParticles.Play();
                gridLight.enabled = true;
                fountainBase.gameObject.SetActive(true);
            }
            else
            {
                fountainParticles.Stop();
                gridLight.enabled = false;
                fountainBase.gameObject.SetActive(false);
            }
        } else
        {
            Debug.LogError("No particles!");
        }
        
    }

    public void on(float timeOutSeconds = 0f, float targetGravity = 0f, float advanceGravity = 0f)
    {
        active = true;
        if(fountainParticles)
        {
            fountainParticles.Play();
            StartCoroutine(activateGridLight());
        }

        if(timeOutSeconds > Mathf.Epsilon)
        {
            this.targetGravity = targetGravity;
            advanceGravityBy = advanceGravity;
            // coroutine that delays turning this off
            StartCoroutine(delayOff(timeOutSeconds)); 
        }
    }

    IEnumerator activateGridLight()
    {
        // put a small delay on this to sync it better with the particle emission
        yield return new WaitForSeconds(0.1f);
        gridLight.enabled = true;
        fountainBase.gameObject.SetActive(true);
    }

    IEnumerator deactivateGridLight()
    {
        // use this delay for the same reasons as above
        yield return new WaitForSeconds(0.15f);
        gridLight.enabled = false;
        fountainBase.gameObject.SetActive(false);
    }

    public void off()
    {
        active = false;
        if(fountainParticles)
        {
            fountainParticles.Stop();
            StartCoroutine(deactivateGridLight());
        }
    }

    IEnumerator delayOff(float timeoutSeconds)
    {
        // see if we should change gravity on update
        if(targetGravity > Mathf.Epsilon && advanceGravityBy > Mathf.Epsilon)
        {
            advanceGravity = true;
        }

        yield return new WaitForSeconds(timeoutSeconds);
        off();
    }

    public void setColor(Color startColor, Color targetColor, float lerpDuration = 0.0f)
    {
        fountainColor = startColor;
        gridLight.color = startColor;
        ParticleSystem.MainModule ma = fountainParticles.main;
        ma.startColor = new Color(startColor.r, startColor.g, startColor.b, 0.4f);

        this.startColor = startColor;
        if (lerpDuration > Mathf.Epsilon && targetColor != Color.white)
        {
            this.targetColor = targetColor;
            this.lerpDuration = lerpDuration;
        } else
        {
            // set target color same as start and no color change will happen
            this.targetColor = this.startColor;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(fountainParticles != null && advanceGravity)
        {
            // continue to advance gravity by provided value until we hit the target
            var main = fountainParticles.main;
            bool increasing = (targetGravity - main.gravityModifier.constant) > 0;
            float updated;
            if(increasing)
            {
                updated = main.gravityModifier.constant + advanceGravityBy;
                if (main.gravityModifier.constant > targetGravity)
                {
                    advanceGravity = false;
                }
            } else
            {
                updated = main.gravityModifier.constant - advanceGravityBy;
                if (main.gravityModifier.constant < targetGravity)
                {
                    advanceGravity = false;
                }
            }
            main.gravityModifier = updated;
        }

        // lerp color
        if(startColor != targetColor && fountainParticles != null && lerped < lerpDuration)
        {
            var main = fountainParticles.main;
            Color newColor = Color.Lerp(startColor, targetColor, lerped / 4);
            main.startColor = gridLight.color = newColor;
            lerped += Time.deltaTime;
        }
    }
}
