using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridFountainController : MonoBehaviour
{
    public bool debug;
    public int startAtMs;
    public string playbackTitle = "woc_test";
    public AudioSource clip;
    public Sun sun;

    // private bool active;
    private GridFountain[] allGridFountains;
    private GridFountain[][] orderedFountains = new GridFountain[20][];

    private EventMap currentEventMap;
    private Priority_Queue.SimplePriorityQueue<int> eventQueue;
    private float eventDeltaTime;
    private int nextEvent = -1;
    private Dictionary<int, EventObject> eventsTable = new Dictionary<int, EventObject>();
    private Dictionary<string, Selection[]> selectionRegister = new Dictionary<string, Selection[]>();
    private bool playingEvents;
    private int globalOffsetMs;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(init());
    }

    IEnumerator init()
    {
        yield return new WaitForSeconds(3.0f);
        Screen.SetResolution(1920, 1080, true);
        allGridFountains = GetComponentsInChildren<GridFountain>();

        // create the ordered fountains from this
        orderedFountains[0] = new GridFountain[46];
        orderedFountains[1] = new GridFountain[46];
        orderedFountains[2] = new GridFountain[46];
        orderedFountains[3] = new GridFountain[46];
        orderedFountains[4] = new GridFountain[46];
        orderedFountains[5] = new GridFountain[46];
        orderedFountains[6] = new GridFountain[44];
        orderedFountains[7] = new GridFountain[41];
        orderedFountains[8] = new GridFountain[39];
        orderedFountains[9] = new GridFountain[35];
        orderedFountains[10] = new GridFountain[33];
        orderedFountains[11] = new GridFountain[29];
        orderedFountains[12] = new GridFountain[26];
        orderedFountains[13] = new GridFountain[22];
        orderedFountains[14] = new GridFountain[19];
        orderedFountains[15] = new GridFountain[16];
        orderedFountains[16] = new GridFountain[12];
        orderedFountains[17] = new GridFountain[9];
        orderedFountains[18] = new GridFountain[5];
        orderedFountains[19] = new GridFountain[1];


        for (int i = 0; i < allGridFountains.Length; i++)
        {
            GridFountain thisFountain = allGridFountains[i];
            //Debug.LogWarning("Trying to add to arr row " + thisFountain.row.ToString() + " col " + thisFountain.col.ToString());
            try
            {
                orderedFountains[thisFountain.row][thisFountain.col] = thisFountain;
            }
            catch
            {
                Debug.Log("broke");

            }
        }

        StartCoroutine(startShow());
    }

    IEnumerator startShow()
    {
        sun.spinSun = true;
        sun.advanceIntensity = true;
        sun.advanceCameraXRot = true;
        yield return new WaitForSeconds(1.0f);

        // start event playback
        StartEventPlayback();
    }

    private int getNextEvent()
    {
        return nextEvent;
    }

    // Update is called once per frame
    void Update()
    {
        // code for reading event queue
        if (playingEvents)
        {
            eventDeltaTime += Time.deltaTime;

            int edt = getEventDeltaTime();

            if (edt > getNextEvent())
            {
                //Debug.Log("edt " + edt + " > nextEvent" + getNextEvent());
                if (eventQueue != null && eventQueue.Count >= 0)
                {
                    EventObject selected;
                    if(eventsTable.TryGetValue(getNextEvent(), out selected))
                    {
                        executeEvent(selected);
                        if(eventQueue.Count > 0)
                        {
                            nextEvent = eventQueue.Dequeue();
                        } else
                        {
                            playingEvents = false;
                        }
                        
                    } else
                    {
                        throw new System.Exception();
                    }
                }
                else
                {
                    playingEvents = false;
                    // disabling looping for now
                    /*
                    // if we reached the end, just reset the beat delta time and start over
                    eventDeltaTime = 0;
                    nextEvent = 0;
                    // don't forget to re-write the queue
                    string jsonString = LoadResourceTextfile(playbackTitle);
                    currentEventMap = JsonUtility.FromJson<EventMap>(jsonString);
                    for (int i = 0; i < currentEventMap.eventMap.Length; i++)
                    {
                        eventQueue.Enqueue(currentEventMap.eventMap[i].index, currentEventMap.eventMap[i].index);
                    }
                    */
                }
            }
        }
    }

    public GridFountain[] deselectFountains(GridFountain[] selections, int interval)
    {
        List<GridFountain> result = new List<GridFountain>();

        // only select every _interval_ out of the selection
        for(int i = 0; i < selections.Length; i += interval)
        {
            result.Add(selections[i]);
        }

        return result.ToArray();
    }

    public GridFountain[] getRegisterSelectionByName(string name, bool reverse)
    {
        Selection[] selections = selectionRegister[name];

        if(selections != null && selections.Length > 0)
        {
            return selectFountains(selections, reverse);
        }

        return null;
    }

    public GridFountain[] selectFountains(Selection[] selectionsIn, bool reverse = false)
    {
        List<GridFountain> result = new List<GridFountain>();

        if (selectionsIn == null || selectionsIn.Length < 1)
        {
            // empty/invalid selection
            return result.ToArray();
        }

        // iterate through selections and add to result
        for (int i = 0; i < selectionsIn.Length; i++)
        {
            // first check to see if there's a register since we want that to be fast
            string registerName = selectionsIn[i].select;
            int deselect = selectionsIn[i].deselect;

            if (selectionRegister.ContainsKey(registerName))
            {   // report the original index here to make debugging easier
                if(debug)
                {
                    Debug.Log((nextEvent - globalOffsetMs) + ": Register Select: " + registerName);
                }
                
                // we can reliably get this register
                return getRegisterSelectionByName(registerName, reverse);
            }

            Selection selection = selectionsIn[i];
            List<GridFountain> thisSelection = new List<GridFountain>();
            if (selection.select == "single")
            {
                // refers to a single fountain, add it to the selection
                thisSelection.Add(orderedFountains[selection.row][selection.col]);
            }
            else if (selection.select == "row")
            {
                // refers to the entire row, add all of them
                for (int j = 0; j < orderedFountains[selection.row].Length; j++)
                {
                    thisSelection.Add(orderedFountains[selection.row][j]);
                }
            }
            else if (selection.select == "col")
            {
                // refers to the entire column, add all of them
                int colLen = getColumnLength(selection.col);
                for (int k = 0; k < colLen; k++)
                {
                    thisSelection.Add(orderedFountains[k][selection.col]);
                }
            } else if (selection.select == "rowTo")
            {
                // selection starts at one row and continues to the next, collect them all
                for (int l = selection.row; l < selection.to; l++)
                {
                    for (int m = 0; m < orderedFountains[l].Length; m++)
                    {
                        thisSelection.Add(orderedFountains[l][m]);
                    }
                }
            } else if (selection.select == "colTo")
            {
                // selection starts at one column and continues to the next, collect them all
                for (int n = selection.col; n < selection.to; n++)
                {
                    int colLen = getColumnLength(n);
                    for (int o = 0; o < colLen; o++)
                    {
                        thisSelection.Add(orderedFountains[o][n]);
                    }
                }
            }

            List<GridFountain> filtered = new List<GridFountain>();
            // deselect rows and cols

            if (selection.deselect_rows != null && selection.deselect_rows.Length > 0)
            {
                foreach (GridFountain resultFountain in thisSelection)
                {
                    bool add = true;
                    foreach (int row in selection.deselect_rows)
                    {
                        if (resultFountain.row == row)
                        {
                            add = false;
                            break;
                        }
                    }
                    if(add)
                    {
                        filtered.Add(resultFountain);
                    }
                }
            }
            if(filtered.Count > 0)
            {
                thisSelection = filtered;
            }

            // apply the deselect to just this selection, then add that to the result
            if(selection.deselect > 0)
            {
                result.AddRange(deselectFountains(thisSelection.ToArray(), deselect));
            } else
            {
                result.AddRange(thisSelection);
            }
            
        }

        if(reverse)
        {
            result.Reverse();
        }

        return result.ToArray();
    }

    private int getColumnLength(int colInd)
    {
        int result = 0;

        for (int i = 0; i < orderedFountains.Length; i++)
        {
            if (orderedFountains[i].Length >= colInd + 1)
            {
                result++;
            } else
            {
                break;
            }
        }

        return result;
    }

    private void StartEventPlayback()
    {
        // write the beatmap
        string jsonString = LoadResourceTextfile(playbackTitle);
        currentEventMap = JsonUtility.FromJson<EventMap>(jsonString);

        // build up the selection registers
        SelectionRegister[] registers = currentEventMap.selectionRegisters;

        for(int h = 0; h < registers.Length; h++)
        {
            selectionRegister.Add(registers[h].name, registers[h].selections);
        }

        // use the beatmap to generate the beat queue, apply offset if there is one
        eventQueue = new Priority_Queue.SimplePriorityQueue<int>(); // make an empty queue
                                                                    // enqueue all the beats

        globalOffsetMs = currentEventMap.offset;

        for (int i = 0; i < currentEventMap.eventMap.Length; i++)
        {
            eventQueue.Enqueue(currentEventMap.eventMap[i].index + globalOffsetMs, currentEventMap.eventMap[i].index + globalOffsetMs);
            // create event hash table for efficiency
            eventsTable.Add(currentEventMap.eventMap[i].index + globalOffsetMs, currentEventMap.eventMap[i]);
        }

        
        // reset beat delta time
        eventDeltaTime = 0;
        nextEvent = eventQueue.Dequeue();

        if(debug && startAtMs > 0)
        {
            // set audio start time
            float startTimeSec = startAtMs / 1000;
            clip.time = eventDeltaTime = startTimeSec;
        }

        // start audio
        clip.Play();

        playingEvents = true;
    }

    public static string LoadResourceTextfile(string name)
    {
        TextAsset targetFile = Resources.Load<TextAsset>(name);
        return targetFile.text;
    }

    private int getEventDeltaTime() // use me to get MS
    {
        return Mathf.FloorToInt(eventDeltaTime * 1000);
    }

    // where the magic finally happens. Fire those puppies!
    private void executeEvent(EventObject eventObject)
    {
        // do whatever COOL THINGS
        //Debug.Log("Fired event action: " + eventObject.action);
        //Debug.Log("For duration: " + eventObject.duration);

        // select
        GridFountain[] selected = selectFountains(eventObject.selection, eventObject.reverse_selection);
        
        //Debug.Log("Selected count: " + selected.Length.ToString());

        // set a color, multiple colors will be selected at random
        List<Color> colors = new List<Color>();
        if (eventObject.colors != null)
        {
            for (int l = 0; l < eventObject.colors.Length; l++)
            {
                // parse the color strings
                Color color;
                if (ColorUtility.TryParseHtmlString(eventObject.colors[l], out color))
                {
                    colors.Add(color);
                }
            }
            float floatDuration = float.Parse(eventObject.duration);
            Color targetColor = Color.white;
            if (eventObject.target_color != null && eventObject.target_color != "")
            {
                ColorUtility.TryParseHtmlString(eventObject.target_color, out targetColor);
            }
            // pass parsed colors into setting helper
            setColors(selected, colors.ToArray(), eventObject.color_select, targetColor, floatDuration);
        }


        // and particle gravity
        float gravity;
        if (float.TryParse(eventObject.gravity, out gravity))
        {
            // first set all selected to the provided gravity
            setGravity(selected, gravity);
            // apply the distrobution if provided
            if (eventObject.distribute_gravity == "linear")
            {
                float dist;
                if (float.TryParse(eventObject.gravity_distrobution, out dist))
                {
                    distributeGravity(selected, dist);
                }
            }
        }

        float duration = float.Parse(eventObject.duration);
        float floatTargetGravity = 0f;
        if (eventObject.target_gravity != null)
        {
            floatTargetGravity = float.Parse(eventObject.target_gravity);
        }

        float floatAdvanceGravity = 0f;
        if (eventObject.advance_gravity != null)
        {
            floatAdvanceGravity = float.Parse(eventObject.advance_gravity);
        }
        float switchSpeed;

        switch (eventObject.action)
        {
            case "on":
                // turn on selected
                for (int i = 0; i < selected.Length; i++)
                {
                    if (duration > Mathf.Epsilon)
                    {
                        selected[i].on(duration, floatTargetGravity, floatAdvanceGravity);
                    }
                    else
                    {
                        selected[i].on();
                    }
                }
                break;

            case "off":
                // turn off selected
                for (int j = 0; j < selected.Length; j++)
                {
                    selected[j].off();
                }
                break;

            case "toggle":
                for (int k = 0; k < selected.Length; k++)
                {
                    selected[k].toggle();
                }
                break;

            case "random":
                
                if(float.TryParse(eventObject.duration, out duration) && float.TryParse(eventObject.switch_speed, out switchSpeed))
                {
                    StartCoroutine(FireFountainsOverTime(selected, duration, switchSpeed, eventObject.switch_count, true, floatTargetGravity, floatAdvanceGravity));
                }
                break;

            case "chase":
                if (float.TryParse(eventObject.duration, out duration) && float.TryParse(eventObject.switch_speed, out switchSpeed))
                {
                    StartCoroutine(FireFountainsOverTime(selected, duration, switchSpeed, eventObject.switch_count, false, floatTargetGravity, floatAdvanceGravity));
                }
                break;
        }
    }

    void distributeGravity(GridFountain[] fountainsIn, float distrobution)
    {
        // always start applying the distrobution from the beginning of the list
        float gravityAdvance = fountainsIn[0].fountainParticles.main.gravityModifier.constant;
        for(int i = 1; i < fountainsIn.Length; i++)
        {
            gravityAdvance += distrobution;
            var main = fountainsIn[i].fountainParticles.main;
            main.gravityModifier = gravityAdvance;
        }
    }

    void setGravity(GridFountain[] fountainsIn, float gravity)
    {
        for (int i = 0; i < fountainsIn.Length; i++)
        {
            if(!fountainsIn[i].active)
            {
                var main = fountainsIn[i].fountainParticles.main;
                main.gravityModifier = gravity;
            }
        }
    }

    void setColors(GridFountain[] fountainsIn, Color[] colors, string colorSelect, Color targetColor, float lerpDuration = 0.0f) // still accepts one color in an array to set it
    {
        for (int i = 0; i < fountainsIn.Length; i++)
        {
            GridFountain thisFountain = fountainsIn[i];
            if (colors.Length == 1)
            {
                // just the one color, set it
                thisFountain.setColor(colors[0], targetColor, lerpDuration);
            } else
            {
                if(colorSelect == "sequential")
                {
                    int selectInd = 0;
                    for (int f = 0; f < fountainsIn.Length; f++)
                    {
                        if((selectInd + 1) > colors.Length)
                        {
                            selectInd = 0;
                        }
                        if(!fountainsIn[f].active)
                        {
                            fountainsIn[f].setColor(colors[selectInd++], targetColor, lerpDuration);
                        }
                    }
                    break;
                }
                else if (colorSelect == "random")
                {
                    // select a color from the list at random and set it to this fountain
                    System.Random rand = new System.Random();
                    int colorInd = rand.Next(0, colors.Length - 1);
                    if(!thisFountain.active)
                    {
                        thisFountain.setColor(colors[colorInd], targetColor, lerpDuration);
                    }
                }
            }
        }
        
    }

    IEnumerator FireFountainsOverTime(GridFountain[] fountainsIn, float duration, float switchSpeed, int count, bool random, float gravity, float advanceGravity)
    {
        if(count == 0)
        {
            count = 1;
        }

        List<int> lastSelected = new List<int>();
        List<int> thisSelected = new List<int>();

        float timeLeft = duration / 10;

        System.Random rand = new System.Random();

        int serialSelectInd = 0;

        while (timeLeft > Mathf.Epsilon)
        {
            yield return new WaitForSeconds(switchSpeed);
            // turn off last set
            foreach(int lastFountainIndex in lastSelected)
            {
                fountainsIn[lastFountainIndex].off();
            }

            // select this set
            for(int i = 0; i < count; i++)
            {
                if(random)
                {
                    // random fountain
                    int randInd = rand.Next(fountainsIn.Length);
                    while (thisSelected.Contains(randInd))
                    {
                        randInd = rand.Next(fountainsIn.Length);
                    }
                    thisSelected.Add(randInd);
                } else
                {
                    int nextInd = serialSelectInd++;
                    // assign serially
                    if(nextInd <= fountainsIn.Length - 1)
                    {
                        thisSelected.Add(nextInd);
                    }
                }
            }

            // turn this set on
            foreach(int fountainIndex in thisSelected)
            {
                fountainsIn[fountainIndex].on(duration, gravity, advanceGravity);
            }

            // this set is now last set
            lastSelected = new List<int>(thisSelected);
            thisSelected.Clear();

            // decrement time
            timeLeft -= (Time.fixedDeltaTime);
            //Debug.Log("Timeleft" + timeLeft);
        }

        // turn off last set
        foreach (int lastFountainIndex in lastSelected)
        {
            fountainsIn[lastFountainIndex].off();
        }

    }

    // turn off all fountains
    public void allOff()
    {
        for(int i = 0; i < allGridFountains.Length; i++)
        {
            allGridFountains[i].off();
        }
    }
}

[System.Serializable]
public class EventMap
{
    public string title;
    public int offset;
    public SelectionRegister[] selectionRegisters;
    public EventObject[] eventMap;
}


[System.Serializable]
public class SelectionRegister
{
    public string name;
    public Selection[] selections;
}

[System.Serializable]
public class EventObject
{
    public int index;
    public string action;
    public string duration;
    public string switch_speed;
    public int switch_count;
    public Selection[] selection;
    public bool reverse_selection;
    public string[] colors;
    public string color_select;
    public string target_color;
    public string gravity;
    public string target_gravity;
    public string advance_gravity;
    public string distribute_gravity;
    public string gravity_distrobution;
}

[System.Serializable]
public class Selection
{
    public int col;
    public int row;
    public int to;
    public string select;
    public int deselect;
    public int[] deselect_rows;
}
