namespace Common
{
    public static class GameDefine
    {
        #region Controller
        public const float MoveSpeed = 5f;
        public const float RotateSpeed = 15f;
        #endregion

        #region MiningItemStackView
        public const float MiningBackOffset = 0.6f;
        public const float MiningCurveDepth = 0.4f;
        public const float MiningStackHeight = 0.2f;
        public const float MiningHeightOffset = 1f;
        public const float MiningTiltFactor = 3f;
        public const float MiningMaxTilt = 15f;
        public const float MiningTiltSpeed = 10f;
        public const float MiningWobbleDuration = 0.6f;
        public const float MiningWobbleFrequency = 3f;
        public const float MiningWobbleAmplitude = 1f;
        #endregion

        #region HandCuffMachineStackView
        public const float HandCuffStackHeight = 0.15f;
        public const float HandCuffHeightOffset = 0.1f;
        public const float HandCuffColumnOffset = 0.3f;
        public const float HandCuffWobbleDuration = 0.5f;
        public const float HandCuffWobbleFrequency = 3f;
        public const float HandCuffWobbleAmplitude = 8f;
        #endregion

        #region HandCuffOutputStackView
        public const float HandCuffOutputStackHeight = 0.15f;
        public const float HandCuffOutputHeightOffset = 0.1f;
        public const float HandCuffOutputColumnOffset = 0.3f;
        public const float HandCuffOutputWobbleDuration = 0.5f;
        public const float HandCuffOutputWobbleFrequency = 3f;
        public const float HandCuffOutputWobbleAmplitude = 8f;
        #endregion

        #region HandCuffCarryView
        public const float HandCuffCarryFrontOffset = 0.4f;
        public const float HandCuffCarryStackHeight = 0.2f;
        public const float HandCuffCarryHeightOffset = 1f;
        #endregion

        #region HandCuffMachineZone
        public const float HandCuffPlayerItemOffset = 1.5f;
        #endregion

        #region HandCuffSellZone
        public const float HandCuffAIQueueStopDistance = 1.2f;
        public const int HandCuffSellMoneyPerHandCuff = 10;
        public const int HandCuffAIMaxQueueSize = 3;
        public const float HandCuffSellStackHeight = 0.15f;
        public const float HandCuffSellHeightOffset = 0.1f;
        public const float HandCuffSellColumnOffset = 0f;
        public const float HandCuffSellWobbleDuration = 0.5f;
        public const float HandCuffSellWobbleFrequency = 3f;
        public const float HandCuffSellWobbleAmplitude = 8f;
        #endregion

        #region MoneyCarryView
        public const float MoneyCarryBackOffset = 0.4f;
        public const float MoneyCarryStackHeight = 0.15f;
        public const float MoneyCarryHeightOffset = 1f;
        #endregion

        #region MoneyStackZone
        public const int MoneyStackColumns = 2;
        public const int MoneyStackRows = 3;
        public const float MoneyStackHeight = 0.05f;
        public const float MoneyStackHeightOffset = 0.1f;
        public const float MoneyStackColumnOffset = 0.15f;
        public const float MoneyStackRowOffset = 0.15f;
        public const float MoneyStackWobbleDuration = 0.5f;
        public const float MoneyStackWobbleFrequency = 3f;
        public const float MoneyStackWobbleAmplitude = 8f;
        #endregion

        #region JoystickInputArea
        public const float JoystickStandbyTime = 4f;
        #endregion

        public enum SceneType
        {
            BootStrap = 0,
            Main = 1
        }

        public enum ObjectState
        {
            None,
            Ready,
            Playing,
            Sleep,
            Error
        }

        public enum InGameCommonAnimation
        {
            Idle,
            Walk,
            Mining,
            Holding
        }

        public enum InGameObjectAnimation
        {
            Appear,
            Disappear
        }

        public enum InputType
        {
            Joystick
        }

        public enum ObjectType
        {
            Mining,
            Character,
        }

        public enum CharacterState
        {
            Idle,
            Mining
        }
    }
}