using Engine.FarMath;

namespace ProjectMercury.Controllers
{
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;

    /// <summary>
    /// A single event on the timeline.
    /// </summary>
    public struct TimelineEvent
    {
        /// <summary>
        /// The time offset of the event in whole and fractional seconds.
        /// </summary>
        public float TimeOffset;

        /// <summary>
        /// The name of the emitter to trigger at the specified time offset.
        /// </summary>
        public string EmitterName;

        /// <summary>
        /// The position of the trigger.
        /// </summary>
        public FarPosition TriggerPosition;

        /// <summary>
        /// 
        /// </summary>
        public Vector2 TriggerImpulse;

        /// <summary>
        /// 
        /// </summary>
        public float TriggerRotation;

        /// <summary>
        /// 
        /// </summary>
        public float TriggerScale;
    }

    /// <summary>
    /// Defines a simple controller which triggers emitters based on a timeline.
    /// </summary>
    public sealed class TimelineController : Controller
    {
        /// <summary>
        /// Counts the total seconds.
        /// </summary>
        public float TotalSeconds { get; private set; }

        /// <summary>
        /// Gets the timeline for a single trigger.
        /// </summary>
        public List<TimelineEvent> Timeline { get; private set; }

        /// <summary>
        /// Gets or sets the queue of timeline events which are due to be processed.
        /// </summary>
        private List<TimelineEvent> EventQueue { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineController"/> class.
        /// </summary>
        public TimelineController()
        {
            this.Timeline = new List<TimelineEvent>();
            this.EventQueue = new List<TimelineEvent>();
        }

        /// <summary>
        /// Called by the particle effect when it is triggered.
        /// </summary>
        /// <param name="position">The desired position of the trigger.</param>
        /// <param name="impulse"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        /// <remarks>
        /// This method should not be called directly, the ParticleEffect class
        /// will defer control to this method when the controller is assigned.
        /// </remarks>
        protected internal override void Trigger(ref FarPosition position, ref Vector2 impulse, float rotation = 0.0f, float scale = 1.0f)
        {
            foreach (TimelineEvent timelineEvent in this.Timeline)
            {
                this.EventQueue.Add(new TimelineEvent
                {
                    EmitterName = timelineEvent.EmitterName,
                    TimeOffset = timelineEvent.TimeOffset + this.TotalSeconds,
                    TriggerPosition = position,
                    TriggerImpulse = impulse,
                    TriggerRotation = rotation,
                    TriggerScale = scale
                });
            }
        }

        /// <summary>
        /// Called by the particle effect when it is updated.
        /// </summary>
        /// <param name="deltaSeconds">Elapsed time in whole and fractional seconds.</param>
        /// <remarks>This method should not be called directly, the ParticleEffect class
        /// will defer control to this method when the controller is assigned.</remarks>
        protected internal override void Update(float deltaSeconds)
        {
            this.TotalSeconds += deltaSeconds;

            if (this.EventQueue.Count > 0)
                for (int i = this.EventQueue.Count - 1; i >= 0; i--)
                {
                    TimelineEvent timelineEvent = this.EventQueue[i];

                    if (timelineEvent.TimeOffset <= this.TotalSeconds)
                    {
                        this.ParticleEffect[timelineEvent.EmitterName].Trigger(ref timelineEvent.TriggerPosition, ref timelineEvent.TriggerImpulse, timelineEvent.TriggerRotation, timelineEvent.TriggerScale);

                        this.EventQueue.RemoveAt(i);
                    }
                }

            base.Update(deltaSeconds);
        }
    }
}