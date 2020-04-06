﻿/*
MIT License

Copyright (c) 2020 Steffen Vetne

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrecisionSurfaceEffects
{
    [RequireComponent(typeof(ParticleSystem))]
    public class SurfaceParticles : MonoBehaviour
    {
        //Fields
        [Header("Quality")]
        public bool inheritVelocities = true;

        [Header("Color")]
        public bool setColor = true; //this isn't the case for sparks or cartooney white puffs

        [Header("Shape")]
        public float shapeScaler = 1;
        public float constantShapeScale = 0.2f;

        [Header("Speed")]
        public float baseSpeedMultiplier = 1;
        public float speedMultiplierBySpeed = 1;

        [Header("Count")]
        public float countByImpulse;
        public int maxCount = 100;

        [Header("Size")]
        public float baseScaler = 1;
        public float scalerByImpulse= 1;
        public float maxScale = 4;

        [HideInInspector]
        public new ParticleSystem particleSystem;

        private SurfaceParticles instance;
        private ParticleSystem temporarySystem;

        private static readonly ContactPoint[] contacts = new ContactPoint[64];
        private static readonly ParticleSystem.Particle[] sourceParticles = new ParticleSystem.Particle[1000];
        private static readonly ParticleSystem.Particle[] destinationParticles = new ParticleSystem.Particle[10000];

        private float startSpeedMultiplier;
        private float startSizeCM;



        //Methods
        public SurfaceParticles GetInstance()
        {
            if(gameObject.scene.name != null)
                return this;

            if (instance == null)
                instance = Instantiate(this);

            return instance;
        }

        public static void GetData(Collision c, out float impulse, out float speed, out Vector3 rot, out Vector3 center, out float radius)
        {
            impulse = c.impulse.magnitude;
            speed = c.relativeVelocity.magnitude;


            Vector3 normal;
            radius = 0;

            int contactCount = c.GetContacts(contacts);
            if (contactCount == 1)
            {
                var contact = contacts[0];
                center = contact.point;
                normal = contact.normal;
            }
            else
            {
                normal = new Vector3();
                center = new Vector3();

                for (int i = 0; i < contactCount; i++)
                {
                    var contact = contacts[i];
                    normal += contact.normal;
                    center += contact.point;
                }

                normal.Normalize();
                float invCount = 1f / contactCount;
                center *= invCount;

                for (int i = 0; i < contactCount; i++)
                {
                    var contact = contacts[i];
                    radius += (contact.point - center).magnitude; //this doesn't care if it is lateral to normal, but should it?
                }

                radius *= invCount;
            }

            rot = Quaternion.FromToRotation(Vector3.up, normal).eulerAngles;
        }

        public void PlayParticles(Collision c, float weight, float impulse, float speed, Vector3 rot, Vector3 center, float radius)
        {
            if (inheritVelocities  && temporarySystem == null)
            {
                var inst = Instantiate(this);
                temporarySystem = inst.GetComponent<ParticleSystem>();
                Destroy(inst);
            }

            ParticleSystem system = inheritVelocities ? temporarySystem : particleSystem;


            radius *= shapeScaler;
            radius += constantShapeScale;

            var main = system.main;
            main.startSpeedMultiplier = startSpeedMultiplier * (baseSpeedMultiplier + speed * speedMultiplierBySpeed);


            var shape = system.shape;
            shape.position = center;
            shape.radius = radius;
            shape.rotation = rot;


            float scale =  Mathf.Min(baseScaler + scalerByImpulse * impulse, maxScale);
            var ss = main.startSize;
            ss.curveMultiplier = scale * startSizeCM;
            main.startSize = ss;


            var countf = Mathf.Min(countByImpulse * impulse, maxCount) * weight;
            int count = (int)countf;
            if (Random.value < countf - count)
                count++;


            system.Emit(count);

            if (inheritVelocities)
            {
                Vector3 Vel(Rigidbody r)
                {
                    if (r == null)
                        return Vector3.zero;
                    return r.GetPointVelocity(center);
                }
                var vel0 = Vel(c.GetContact(0).thisCollider.attachedRigidbody);
                var vel1 = Vel(c.rigidbody);

                int dstCount = particleSystem.GetParticles(destinationParticles);

                int maxDst = Mathf.Min(destinationParticles.Length, main.maxParticles);
                var dstPC = particleSystem.particleCount;
                int takingCount = Mathf.Min(maxDst - dstPC, temporarySystem.GetParticles(sourceParticles));
                for (int i = 0; i < takingCount; i++)
                {
                    var particle = sourceParticles[i];

                    float rand = Random.value;
                    particle.velocity += vel0 * (1 - rand) + vel1 * rand;

                    destinationParticles[dstCount] = particle;
                    dstCount++;
                }

                particleSystem.SetParticles(destinationParticles, dstCount);
                temporarySystem.Clear(); // SetParticles(destinationParticles, 0);
            }
        }



        //Lifecycle
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!particleSystem)
                particleSystem = GetComponent<ParticleSystem>();
        }
#endif

        private void Start()
        {
            startSizeCM = particleSystem.main.startSize.curveMultiplier;
            startSpeedMultiplier = particleSystem.main.startSpeedMultiplier;

            transform.SetParent(null);
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            var e = particleSystem.emission;
            e.enabled = false;
        }
    }
}