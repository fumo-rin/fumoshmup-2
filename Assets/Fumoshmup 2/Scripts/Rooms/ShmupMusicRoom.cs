using rinCore;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FumoShmup2
{
    public class ShmupMusicRoom : rinCore.MusicRoom
    {
        [SerializeField] List<ShmupStage> MusicStages = new();
        protected override List<MusicWrapper> Tracklist()
        {
            HashSet<MusicWrapper> result = base.Tracklist().ToHashSet();
            foreach (ShmupNodeStage item in MusicStages.Where(x => x is ShmupNodeStage nodeStage))
            {
                foreach (var music in item.NodeMusic)
                {
                    result.Add(music);
                }
            }
            return result.ToList();
        }
    }
}
