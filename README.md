# Summary

I started this fun project as a way to practice turning the ideas in my head into reality. While watching the World of Color show again late last year I found myself thinking deeply about what it would take to recreate the entire show digitally and what kind of advantages that might entail from a business perspective. I firmly believe that a simulation application like this can prove to be of immense benefit to creating content for real-world systems by serving as a content creation tool to enhance a content pipeline. Empowering artists who are not software experts is the key to a high quality and efficient content pipeline.

Here are some of the features that this v1 implements. Their implementation is briefly discussed in the technical summary below.

* **Accurate topographical reproduction of Paradise Bay, including lagoon, fountain platform, Incredicoaster, Pixar Pal-Around Ferris Wheel, and pretty much everything else**

* **Accurately positioned grid fountain array matched to Disney design docs available online**

* **Grid fountain objects that support particle and light emission; properties can be changed over time using the provided fountain action schema**

* **Highly accurate, customizeable and easy to use audio sync and fountain control approach based on hand crafted JSON**

* **Debug support for quick content creation**

# Technical Summary

**Topography**
When reproducing anything in the real world care must be taken to ensure proper scale and perspective are maintained. I started with a topographical baseline of Paradise Bay by extracting 3D image data from Google Earth and using it to create a somewhat low resolution mesh of Paradise Bay. This baseline model serves as the master scale to which higher resolution models can be used to replace the lower resolution ones. This was especially essential when it came to scaling the Incredicoaster and Pal-Around Wheel. Some wonderful folks out there modeled these structures and made them publicly available online. However, the one and only Incredicoaster mesh I was able to find deviated from the actual shape of the coaster as shown by the Google Earth data. This required a lot of hand-editing of that mesh to get it somewhat acceptable. I am not a 3D modeler and it was a lot of effort for me to get this to an acceptable state. If you inspect the mesh for Incredicoaster you'll see that it's quite the mess and not coherent, but it serves its purpose of being scaled correctly from the perspective of the viewing area. The mesh for the ferris wheel was pretty close and only needed very minor mesh scaling. An endgame improvement for this application would be to produce and implement highly accurate 3D meshes of Paradise Bay. Somehow I don't think Disney will be making those available any time soon.

**Grid Fountain Array**
If you're not familiar with the show's hardware you should know that the lagoon contains over 1,200 individual fountains. Some of these fountains even have multiple spray heads that it can switch between on-the-fly. The majority of these fountains (about half) consist of the most basic kind of fountain: the grid fountain. Grid fountains are only capable of shooting straight up into the air but since they are arranged in a grid over the entire fountain platform, they provide a sturdy base to structure content on. There are many other kinds of fountains in the show and it's these fountains combined with the amazing color effects that contribute to the overall quality/impact of the show. In v1 I focused only on implementing the workhorse grid fountain. Future versions should focus on adding the other kinds of fountains like the water whip (vectoring fountain) and projection screen fountains to bring the show to its full potential.

**Fountain Control System**
For reference, here is the JSON for the demo included in the repo. https://github.com/JetsterDajet/WorldOfColor/blob/master/Assets/Resources/woc_test.txt

This is what most of the work went into. In a nutshell, we have a priority queue keyed by an integer that represents milliseconds elapsed and valued to an object that directly controls selected fountains at that time. As an example, here's what the action JSON for the very first fountain hit of the show looks like
```
{
   "index": 30735,
   "action": "on",
   "duration" : "0.2",
   "selection": [{
        "select": "front_rows_hit_A"
    }],
    "colors": [
    "#6ba4ff"
    ],
    "gravity": "25"
}
```

`index` refers to milliseconds elapsed since the audio stream started playback. Use your favorite audio application that displays elapsed milliseconds and use that number here for synchronization.

`action` refers to the action that will be performed on the fountain selection. `on` activates all fountains in the selection, whether they were already active or not. `chase` is another kind of action that activates and deactivates fountains over time to create a moving fountain effect.

`duration` refers to how long in seconds before the selected fountains will be turned off.

`selection` refers to the actively selected fountains which will be performing the actions described in this event. In this example only a single string is provided. In this case, we're asking to take a pre-selected collection of fountains which have been defined in `selectionRegisters`. Selection registers allow us to be more efficient by already having complex selections ready to go at run time. It's also a lot easier to enter selections in event blocks like above.  More on the selection JSON below.

`colors` refers to the colors that will be applied to the selection. In this case, it's just a single color provided, so that color will be set to all selected fountains. It's also possible to provide additional colors and for those colors to be assigned randomly or sequentially. Further, color can be lerped over time. Inspect the color blocks in the example JSON for examples of how to do this.

`gravity` refers to the gravity applied to all the select fountains. Lower gravity means the fountain will shoot higher, lower gravity means lower. Generally, I've been using a gravity min/max range of 10-150, this seems to work well. Gravity can be assigned sequentially via linear or quadratic functions which gives us some nice parabolas and such. Further, gravity can be changed over time like color so crescendos and decrescendos have visual impact. Inspect the gravity blocks in the example JSON for examples of how to do this.

*Selection*
Here's what the actual selection object in from the registers in JSON looks like for `front_rows_hit_A`

```
{
   "name" : "front_rows_hit_A",
    "selections" : [{
        "select": "rowTo",
        "row" : 11,
        "to" : 19,
        "deselect" : 3
     }]
}
```
`name` refers to the name of the selection, so it can be referred to from the register in action events.

`selections` is the total collection of selections that make up this selection. Multiple selections will be added together.

`select` refers to how fountains will be selected from the grid, based on column and row indices. `row` will select an entire row as `column` selects an entire column. `rowTo` and `colTo` select multiple rows and columns. Use the `to` field to specify up to what index row/columns should be selected.

`deselect` refers to a statically sequential deselection that allows some of the selection to be removed. Every nth element is deselected based on the provided integer. I added this so I could thin out blocks of fountains all firing together which is too dense and probably physically impossible due to water pressure limitations.

There are a few other unique control features that should be relatively self-explanatory by reading through the JSON and source. Of course, if anyone has absolutely any questions about this feel free to contact me.