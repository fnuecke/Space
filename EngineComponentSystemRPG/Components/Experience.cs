using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.Serialization;

namespace Engine.ComponentSystem.RPG.Components
{
    public sealed class Experience : Component
    {
        #region Type ID

        /// <summary>The unique type ID for this object, by which it is referred to in the manager.</summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>The type id unique to the entity/component system in the current program.</summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Properties

        /// <summary>The current level.</summary>
        public int Level
        {
            get { return _level; }
        }

        /// <summary>The maximum level.</summary>
        public int MaxLevel
        {
            get { return _maxLevel; }
        }

        /// <summary>The current amount of experience.</summary>
        public int Value
        {
            get { return _value; }
            set
            {
                // Skip if nothing changes.
                if (_value == value)
                {
                    return;
                }

                // Bound value.
                value = System.Math.Max(0, value);
                value = System.Math.Min((int) (_multiplier * System.Math.Pow(_maxLevel - 1, _exponent)), value);

                // Check again if anything changes (e.g. when already max level).
                if (_value == value)
                {
                    return;
                }

                // Set new value.
                _value = value;

                // See if we leveled up or down.
                if (_value >= _currentLevelValue && _value < _nextLevelValue)
                {
                    // Nope, skip the rest.
                    return;
                }

                // Prepare message.
                ExperienceLevelChanged message;
                message.Component = this;
                message.OldLevel = _level;

                // We want to compute the level we should now be at (we might have lost
                // or gained multiple levels).
                // Our formula is: xp(lvl) = m * (lvl-1)^e
                // Or: value = _multiplier * pow(_level - 1, _exponent)
                // Thus: _level = 1 + pow(value / _multiplier, 1 / _exponent)
                _level = 1 + (int) System.Math.Pow(value / _multiplier, 1f / _exponent);
                _currentLevelValue = (int) (_multiplier * System.Math.Pow(_level - 1, _exponent));
                _nextLevelValue = (int) (_multiplier * System.Math.Pow(_level, _exponent));

                // Send level change message.
                if (Enabled && Manager != null)
                {
                    message.NewLevel = _level;
                    Manager.SendMessage(message);
                }
            }
        }

        /// <summary>Experience that was required to reach current level.</summary>
        public int RequiredForCurrentLevel
        {
            get { return _currentLevelValue; }
        }

        /// <summary>Experience required to reach next level.</summary>
        public int RequiredForNextLevel
        {
            get { return _nextLevelValue; }
        }

        #endregion

        #region Fields

        /// <summary>The multiplier used for computing required experience for level up.</summary>
        private float _multiplier;

        /// <summary>The exponent used for computing required experience for level up.</summary>
        private float _exponent;

        /// <summary>The maximum level that can be reached.</summary>
        private int _maxLevel;

        /// <summary>The current amount of experience.</summary>
        private int _value;

        /// <summary>The current level.</summary>
        [PacketizeIgnore]
        private int _level = 1;

        /// <summary>Experience required to reach current level.</summary>
        [PacketizeIgnore]
        private int _currentLevelValue;

        /// <summary>Experience required to reach next level.</summary>
        [PacketizeIgnore]
        private int _nextLevelValue;

        #endregion

        #region Initialization

        /// <summary>Initializes the component using the specified parameters.</summary>
        /// <param name="maxLevel">The max level.</param>
        /// <param name="multiplier">The multiplier.</param>
        /// <param name="exponent">The exponent.</param>
        /// <returns>The initialized component.</returns>
        public Experience Initialize(int maxLevel, float multiplier, float exponent)
        {
            _multiplier = multiplier;
            _exponent = exponent;
            _maxLevel = maxLevel;
            _nextLevelValue = (int) (_multiplier * System.Math.Pow(_level, _exponent));

            return this;
        }

        /// <summary>Reset the component to its initial state, so that it may be reused without side effects.</summary>
        public override void Reset()
        {
            base.Reset();

            _level = 1;
            _maxLevel = 1;
            _value = 0;
            _currentLevelValue = 0;
            _nextLevelValue = 0;
            _multiplier = 0f;
            _exponent = 0f;
        }

        #endregion

        #region Serialization

        [OnPostDepacketize]
        public void Depacketize(IReadablePacket packet)
        {
            _level = 1 + (int) System.Math.Pow(_value / _multiplier, 1f / _exponent);
            _currentLevelValue = (int) (_multiplier * System.Math.Pow(_level - 1, _exponent));
            _nextLevelValue = (int) (_multiplier * System.Math.Pow(_level, _exponent));
        }

        #endregion
    }
}