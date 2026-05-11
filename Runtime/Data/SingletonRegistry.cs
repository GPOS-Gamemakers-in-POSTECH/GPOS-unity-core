using System.Collections.Generic;
using UnityEngine;

namespace GPOS.Core
{
    [CreateAssetMenu(
        fileName = "SingletonRegistry",
        menuName = "Singleton/Singleton Registry"
    )]
    public class SingletonRegistry : ScriptableObject
    {
        [Tooltip("List of singleton prefabs to be created automatically")]
        public List<GameObject> prefabs = new();
    }
}
