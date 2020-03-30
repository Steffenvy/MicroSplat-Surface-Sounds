# Microsplat Footstep Sounds

Allows you to find audioClips in Unity depending on what a spherecast hits.

Uses: https://github.com/SubjectNerd-Unity/ReorderableInspector for some reorderability

## Usage

The component `FootSoundTester.cs` can be used to test it out.


If the spherecast hits a TerrainCollider, it will use MicroSplat's Config file to find the indices to test against by using the Albedo (Diffuse) textures of the splats

If the collider is not a TerrainCollider, it will test if the name of the MeshRenderer's material/s includes one of the keywords.

A collider has to be a non-convex MeshCollider to discern between the MeshRenderer's (submesh) materials. Otherwise it will default to the first material.
This can be a problem if the first material is not the one you want to test against.
That's when you can use the component `SurfaceTypeMarker.cs` to override the test string.

The collider has to be on the same GameObject as the MeshRenderer

The material's name can include the keyword with any capitalization:
    Grass
    GRASS
    grass
    GrAsS
    
Keywords shouldn't contains another SurfaceType's keyword


Create a FootstepSounds asset in Unity's project window (by right clicking -> Create -> FootstepSounds)

If you want to change the MicroSplat config during runtime, you need to use `FootstepSounds.InitConfig(JBooth.MicroSplat.TextureArrayConfig textureArrayConfig)`

You can get a randomized volume and pitch. `SurfaceType`s have individual control over the amount, for example Concrete shouldn't be as randomized as e.g. Mud.

You can cache the `SurfaceType` from `GetSurfaceType`, to update it less frequently. However, the performance should be ok, and the player should definitely be up to date.

If a SurfaceType can't be found (if none of the SurfaceTypes include the Terrain index, or if none of the SurfaceTypes' keywords are included in the check string (the Material name or Marker reference)) the `defaultSurfaceType` is sent.

SoundSets are used for different sized creatures, although you don't need to use multiple. The default for `GetSoundSet(soundSetID)` is the first one (0 id). The `soundSetID` can be found with `FindSoundSetID(name)`, where the string will be searched for in `soundSetNames`.
You'll need to be diligent in reordering every `SoundSet` individually, as well as ensure that each `SoundSet` is included in each `SurfaceType`. 

## Limitations

- Only works for MicroSplat. 
This can be changed:
    remove `textureArrayConfig` 
    remove `[HideInInspector]` from above `public int[] terrainIndices;`
    remove the `InitConfig` function
    remove `terrainAlbedos`, as that will no longer be used
Then you will need to specify the terrain indices manually
    
- Material names need to be proper (only include the relevant keyword, not another SurfaceType's keyword. Otherwise it might find the wrong SurfaceType)

- If you want to reorder the SurfaceTypes, 
    you need to uncomment: `//[Reorderable]` that is above `public SurfaceType[] surfaceTypes = new SurfaceType[] { new SurfaceType() };`
    Because the ReorderableInspector-master doesn't work for nested reorderables

## Follow me on Twitter

https://twitter.com/PrecisionCats
