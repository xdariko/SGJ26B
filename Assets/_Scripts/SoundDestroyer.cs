using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundDestroyer : MonoBehaviour
{
    private AudioSource _audioSourse;
    private float _clipLength;

    private void Awake()
    {
        _audioSourse = GetComponent<AudioSource>();
    }

    private IEnumerator Start()
    {
        _clipLength = _audioSourse.clip.length;

        yield return new WaitForSeconds(_clipLength);

        Destroy(gameObject);
    }
}
