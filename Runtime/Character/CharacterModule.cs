using UnityEngine;

namespace UyiCore.Character
{
    [RequireComponent(typeof(CharacterRoot))]
    public abstract class CharacterModule : MonoBehaviour
    {
        protected CharacterRoot Owner { get; private set; }

        public virtual void Initialize(CharacterRoot owner)
        {
            Owner = owner;
        }

        public virtual void OnTick(float deltaTime) { }
        public virtual void OnFixedTick(float fixedDeltaTime) { }
    }
}
