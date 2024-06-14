using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace ImageFactory.Interfaces
{
    internal interface ISpriteAsyncLoader
    {
        public Task<Sprite> LoadSpriteAsync(string path, CancellationToken cancellationToken);

        public void ClearCache();
    }
}