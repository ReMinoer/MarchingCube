using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class BlobEmitter : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    private readonly List<Blob> _blobs = new List<Blob>();

    void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        var particles = new ParticleSystem.Particle[_particleSystem.maxParticles];
        int aliveParticleNumber = _particleSystem.GetParticles(particles);

        Blob blob;
        int i = 0;
        while (i < aliveParticleNumber)
        {
            if (i >= _blobs.Count)
            {
                var blobGameObject = new GameObject("Blob");
                blobGameObject.transform.parent = transform;

                blob = blobGameObject.AddComponent<Blob>();
                _blobs.Add(blob);
            }
            else
                blob = _blobs[i];

            blob.transform.position = _particleSystem.transform.position + _particleSystem.transform.rotation * particles[i].position;
            i++;
        }

        while (i < _blobs.Count)
        {
            blob = _blobs[i];
            _blobs.RemoveAt(i);
            Destroy(blob.gameObject);
        }
    }
}
