using Engine.ComponentSystem.Components;
using Engine.ComponentSystem.RPG.Messages;
using Engine.Serialization;

namespace Engine.ComponentSystem.RPG.Components
{
    public sealed class Experience : Component
    {
        #region Type ID

        /// <summary>
        /// The unique type ID for this object, by which it is referred to in the manager.
        /// </summary>
        public static readonly int TypeId = CreateTypeId();

        /// <summary>
        /// The type id unique to the entity/component system in the current program.
        /// </summary>
        public override int GetTypeId()
        {
            return TypeId;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The current level.
        /// </summary>
        public int Level
        {
            get { return _level; }
        }

        /// <summary>
        /// The maximum level.
        /// </summary>
        public int MaxLevel
        {
            get { return _maxLevel; }
        }

        /// <summary>
        /// The current amount of experience.
        /// </summary>
        public int Value
        {
            get { return _value; }
            set {
                // Skip if nothing changes.
                if (_value == value)
                {
                    return;
                }

                // Bound value.
                value = System.Math.Max(0, value);
                value = System.Math.Min((int)(_multiplier * System.Math.Pow(_maxLevel - 1, _exponent)), value);

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
                _level = 1 + (int)System.Math.Pow(value / _multiplier, 1f / _exponent);
                _currentLevelValue = (int)(_multiplier * System.Math.Pow(_level - 1, _exponent));
                _nextLevelValue = (int)(_multiplier * System.Math.Pow(_level, _exponent));

                // Send level change message.
                if (Enabled && Manager != null)
                {
                    message.NewLevel = _level;
                    Manager.SendMessage(message);
                }
            }
        }

        /// <summary>
        /// Experience that was required to reach current level.
        /// </summary>
        public int RequiredForCurrentLevel
        {
            get { return _currentLevelValue; }
        }

        /// <summary>
        /// Experience required to reach next level.
        /// </summary>
        public int RequiredForNextLevel
        {
            get { return _nextLevelValue; }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The multiplier used for computing required experience for level up.
        /// </summary>
        private float _multiplier;

        /// <summary>
        /// The exponent used for computing required experience for level up.
        /// </summary>
        private float _exponent;

        /// <summary>
        /// The maximum level that can be reached.
        /// </summary>
        private int _maxLevel;

        /// <summary>
        /// The current amount of experience.
        /// </summary>
        private int _value;

        /// <summary>
        /// The current level.
        /// </summary>
        [PacketizerIgnore]
        private int _level = 1;

        /// <summary>
        /// Experience required to reach current level.
        /// </summary>
        [PacketizerIgnore]
        private int _currentLevelValue;

        /// <summary>
        /// Experience required to reach next level.
        /// </summary>
        [PacketizerIgnore]
        private int _nextLevelValue;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the component by using another instance of its type.
        /// </summary>
        /// <param name="other">The component to copy the values from.</param>
        /// <returns></returns>
        public override Component Initialize(Component other)
        {
            base.Initialize(other);

            var otherExperience = (Experience)other;
            _level = otherExperience.Level;
            _maxLevel = otherExperience._maxLevel;
            _value = otherExperience.Value;
            _currentLevelValue = otherExperience._currentLevelValue;
            _nextLevelValue = otherExperience._nextLevelValue;
            _multiplier = otherExperience._multiplier;
            _exponent = otherExperience._exponent;

            return this;
        }

        /// <summary>
        /// Initializes the component using the specified parameters.
        /// </summary>
        /// <param name="maxLevel">The max level.</param>
        /// <param name="multiplier">The multiplier.</param>
        /// <param name="exponent">The exponent.</param>
        /// <returns>The initialized component.</returns>
        public Experience Initialize(int maxLevel, float multiplier, float exponent)
        {
            _multiplier = multiplier;
            _exponent = exponent;
            _maxLevel = maxLevel;
            _nextLevelValue = (int)(_multiplier * System.Math.Pow(_level, _exponent));

            return this;
        }

        /// <summary>
        /// Reset the component to its initial state, so that it may be reused
        /// without side effects.
        /// </summary>
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

        /// <summary>
        /// Bring the object to the state in the given packet. This is called
        /// after automatic depacketization has been performed.
        /// </summary>
        /// <param name="packet">The packet to read from.</param>
        public override void PostDepacketize(IReadablePacket packet)
        {
            base.PostDepacketize(packet);

            _level = 1 + (int)System.Math.Pow(_value / _multiplier, 1f / _exponent);
            _currentLevelValue = (int)(_multiplier * System.Math.Pow(_level - 1, _exponent));
            _nextLevelValue = (int)(_multiplier * System.Math.Pow(_level, _exponent));
        }

        /// <summary>
        /// Push some unique data of the object to the given hasher,
        /// to contribute to the generated hash.
        /// </summary>
        /// <param name="hasher">The hasher to push data to.</param>
        public override void Hash(Hasher hasher)
        {
            base.Hash(hasher);

            hasher
                .Put(_level)
                .Put(_maxLevel)
                .Put(_value)
                .Put(_currentLevelValue)
                .Put(_nextLevelValue)
                .Put(_multiplier)
                .Put(_exponent);
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return base.ToString() + ", Level=" + _level + ", XP=" + _value;
        }

        #endregion
    }
}
