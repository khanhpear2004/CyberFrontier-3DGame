using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioSourcePool
{
    private sealed class Slot
    {
        public AudioSource Source;
        public Transform FollowTarget;
        public int Generation;
    }

    private readonly List<Slot> slots;
    private int nextSlot;

    public AudioSourcePool(Transform parent, int capacity)
    {
        slots = new List<Slot>(Mathf.Max(1, capacity));

        for (int index = 0; index < Mathf.Max(1, capacity); index++)
        {
            GameObject sourceObject = new GameObject($"PooledAudioSource_{index:00}");
            sourceObject.transform.SetParent(parent, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            slots.Add(new Slot { Source = source });
        }
    }

    public AudioHandle Play(
        AudioCue cue,
        AudioMixerGroup output,
        Vector3 position,
        Transform followTarget,
        bool forceTwoDimensional
    )
    {
        AudioClip clip = cue != null ? cue.GetRandomClip() : null;
        if (clip == null)
        {
            return AudioHandle.Invalid;
        }

        int slotIndex = FindAvailableSlot();
        Slot slot = slots[slotIndex];
        AudioSource source = slot.Source;

        source.Stop();
        source.clip = clip;
        source.outputAudioMixerGroup = cue.OutputOverride != null ? cue.OutputOverride : output;
        source.volume = cue.GetRandomVolume();
        source.pitch = cue.GetRandomPitch();
        source.loop = cue.Loop;
        source.spatialBlend = forceTwoDimensional ? 0f : cue.SpatialBlend;
        source.minDistance = cue.MinDistance;
        source.maxDistance = cue.MaxDistance;
        source.priority = cue.Priority;
        source.transform.position = followTarget != null ? followTarget.position : position;

        slot.FollowTarget = cue.Loop ? followTarget : null;
        slot.Generation++;
        source.Play();
        return new AudioHandle(slotIndex, slot.Generation);
    }

    public void Stop(AudioHandle handle)
    {
        if (!handle.IsValid || handle.SlotIndex >= slots.Count)
        {
            return;
        }

        Slot slot = slots[handle.SlotIndex];
        if (slot.Generation != handle.Generation)
        {
            return;
        }

        slot.Source.Stop();
        slot.Source.clip = null;
        slot.FollowTarget = null;
    }

    public void UpdateFollowingSources()
    {
        foreach (Slot slot in slots)
        {
            if (slot.Source.isPlaying && slot.FollowTarget != null)
            {
                slot.Source.transform.position = slot.FollowTarget.position;
            }
        }
    }

    private int FindAvailableSlot()
    {
        for (int offset = 0; offset < slots.Count; offset++)
        {
            int index = (nextSlot + offset) % slots.Count;
            if (!slots[index].Source.isPlaying)
            {
                nextSlot = (index + 1) % slots.Count;
                return index;
            }
        }

        int fallback = nextSlot;
        nextSlot = (nextSlot + 1) % slots.Count;
        return fallback;
    }
}
