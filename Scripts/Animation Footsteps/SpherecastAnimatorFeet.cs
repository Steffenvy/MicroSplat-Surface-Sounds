﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrecisionSurfaceEffects;

//You use PlayFootSound(int id) as an animation event

//This is untested though

public class SphereCastAnimatorFeet : MonoBehaviour
{
    //Fields
    public SurfaceSoundSet soundSet;

    [Space(20)]
    public Foot[] feet = new Foot[2];
    public float minWeight = 0.2f;

    [Header("Sphere Cast")]
    public LayerMask layerMask = -1;
    public float maxDistance = Mathf.Infinity;
    public Transform origin;
    public float radius = 1;


    //Datatypes
    [System.Serializable]
    public class Foot
    {
        public AudioSource[] audioSources;
    }


    //Methods
    public void PlayFootSound(int footID, float volumeMultiplier = 1, float pitchMultiplier = 1)
    {
        var foot = feet[footID];


        var pos = origin.position;
        var dir = -origin.up;


        int maxCount = foot.audioSources.Length;
        var outputs = soundSet.data.GetSphereCastSurfaceTypes
        (
            pos, dir, radius: radius,
            maxOutputCount: maxCount, shareList: true,
            maxDistance: maxDistance, layerMask: layerMask
        );
        outputs.Downshift(maxCount, minWeight);

        for (int i = 0; i < outputs.Count; i++)
        {
            var output = outputs[i];
            var vm = output.weight * output.volume * volumeMultiplier;
            var pm = output.pitch * pitchMultiplier;
            soundSet.surfaceTypeSounds[output.surfaceTypeID].PlayOneShot(foot.audioSources[i], volumeMultiplier: vm, pitchMultiplier: pm);
        }
    }
}
