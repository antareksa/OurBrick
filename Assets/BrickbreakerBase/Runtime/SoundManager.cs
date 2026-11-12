using System.Collections.Generic;
using UnityEngine;

namespace BrickbreakerBase
{
    public enum SoundType
    {
        Shoot,
        WallBounce,
        HitBlock,
        Destroyed,
        BallReturn,
        CollectCoin,
        WaveClear,
    }

    [System.Serializable]
    public class SoundEntry
    {
        public SoundType Type;
        public AudioClip Clip;
    }

    // Ships with an empty clip list on purpose (no audio in the package) -- add entries through
    // BrickbreakerBootstrap.Sounds, or at runtime via SetClip. Playing a type with no clip is a no-op.
    public class SoundManager : MonoBehaviour
    {
        public AudioSource AudioSource;
        public List<SoundEntry> Sounds = new List<SoundEntry>();

        private readonly Dictionary<SoundType, AudioClip> _clipsByType = new Dictionary<SoundType, AudioClip>();

        public void SetClips(List<SoundEntry> entries)
        {
            _clipsByType.Clear();

            if (entries == null) return;

            foreach (SoundEntry entry in entries)
            {
                _clipsByType[entry.Type] = entry.Clip;
            }
        }

        public void SetClip(SoundType type, AudioClip clip)
        {
            _clipsByType[type] = clip;
        }

        // PlayOneShot (not swapping AudioSource.clip) so overlapping sounds -- e.g. two balls
        // hitting bricks the same frame -- layer instead of cutting each other off.
        public void Play(SoundType type)
        {
            if (AudioSource == null) return;

            if (_clipsByType.TryGetValue(type, out AudioClip clip) && clip != null)
            {
                AudioSource.PlayOneShot(clip);
            }
        }
    }
}
