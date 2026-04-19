using System.Collections.Generic;
using Common.Scene.Parameter;
using Generated.Table;
using InGame.Object;
using static Common.GameDefine;

namespace InGame.Model
{
    public class InGameObjectModel
    {
        public InGameObjectModel(SceneParameterMain sceneParameterMain)
        {
        }
        
        private readonly List<ObjectBase> _objects = new();
        public IReadOnlyList<ObjectBase> Objects => _objects;
        private readonly List<CharacterBase> _characters = new();
        public IReadOnlyList<CharacterBase> Characters => _characters;
        public CharacterBase Player { get; private set; }

        public void AddObject(ObjectBase objectBase) => _objects.Add(objectBase);
        public void RemoveObject(ObjectBase objectBase) => _objects.Remove(objectBase);

        public void ActivateAll()
        {
            foreach (var obj in _objects)
                if (obj.State == ObjectState.Ready)
                    obj.SetState(ObjectState.Playing);
        }

        public void AddCharacter(CharacterBase character, bool isPlayer = false)
        {
            _objects.Add(character);
            _characters.Add(character);
            if (isPlayer) Player = character;
        }

        public void RemoveCharacter(CharacterBase character, bool isPlayer = false)
        {
            _objects.Remove(character);
            _characters.Remove(character);
            if (isPlayer && Player == character) Player = null;
        }
    }
}
