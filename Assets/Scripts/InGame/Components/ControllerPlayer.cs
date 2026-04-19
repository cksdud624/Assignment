using System;
using InGame.Model;
using InGame.Object;
using InGame.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using static Common.GameDefine;

namespace InGame.Components
{
    public class ControllerPlayer : ControllerBase
    {
        private PlayerInput _playerInput;
        private InputActionMap _defaultMap;
        private JoystickView _joystick;

        public override void Init(InGameModel inGameModel, CharacterHub hub)
        {
            base.Init(inGameModel, hub);
            _playerInput = gameObject.AddComponent<PlayerInput>();
            var asset = Resources.Load<InputActionAsset>("InputSystem/PlayerAction");
            if (asset == null)
            {
                Debug.LogError("Input action asset not found");
                return;
            }

            _playerInput.actions = asset;
            if (asset.actionMaps.Count == 0)
            {
                Debug.LogError("Action maps are empty");
                return;
            }

            _defaultMap = asset.actionMaps[0];
            _playerInput.defaultActionMap = _defaultMap.name;

            BindActions();
            _defaultMap.Enable();
        }

        private void BindActions()
        {
            foreach (InputType type in Enum.GetValues(typeof(InputType)))
            {
                var action = _defaultMap.FindAction(type.ToString());
                if (action == null)
                {
                    Debug.LogError($"Action {type} not found in map {_defaultMap.name}");
                    continue;
                }
                action.performed += OnPerformed;
                action.canceled += OnCanceled;
            }
        }

        private void OnPerformed(InputAction.CallbackContext ctx)
        {
            if (!Enum.TryParse<InputType>(ctx.action.name, out var type)) return;
            switch (type)
            {
                case InputType.Joystick:
                    SetMoveDirection(ToWorldDirection(ctx.ReadValue<Vector2>()));
                    break;
            }
        }

        private void OnCanceled(InputAction.CallbackContext ctx)
        {
            if (!Enum.TryParse<InputType>(ctx.action.name, out var type)) return;
            switch (type)
            {
                case InputType.Joystick:
                    SetMoveDirection(Vector3.zero);
                    break;
            }
        }

        public void SetJoystick(JoystickView joystick)
        {
            _joystick = joystick;
            _joystick.OnPerformed += OnJoystickPerformed;
            _joystick.OnCanceled += OnJoystickCanceled;
        }

        private void OnJoystickPerformed(Vector2 v) => SetMoveDirection(ToWorldDirection(v));
        private void OnJoystickCanceled() => SetMoveDirection(Vector3.zero);

        private static Vector3 ToWorldDirection(Vector2 input)
        {
            var cam = Camera.main.transform;
            var forward = new Vector3(cam.forward.x, 0f, cam.forward.z).normalized;
            var right   = new Vector3(cam.right.x,   0f, cam.right.z).normalized;
            return forward * input.y + right * input.x;
        }

        private void OnDestroy()
        {
            if (_joystick != null)
            {
                _joystick.OnPerformed -= OnJoystickPerformed;
                _joystick.OnCanceled -= OnJoystickCanceled;
            }

            if (_defaultMap == null) return;
            foreach (InputType type in Enum.GetValues(typeof(InputType)))
            {
                var action = _defaultMap.FindAction(type.ToString());
                if (action == null) continue;
                action.performed -= OnPerformed;
                action.canceled -= OnCanceled;
            }
        }
    }
}
