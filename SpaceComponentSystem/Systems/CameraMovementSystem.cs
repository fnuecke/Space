using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.ComponentSystem.Common.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Session;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Systems
{
    public class CameraMovementSystem : AbstractSystem, IUpdatingSystem,IMessagingSystem
    {
        private List<MoveCamera.Positions> _positions = new List<MoveCamera.Positions>();
        private long _frame;
        private FarPosition _lastPosition;
        private FarPosition _step;
        private float _lastZoom;
        private float _zoomStep;
        private bool _returnToSender;
        private long _returnSpeed;
        private readonly IClientSession _session;
        /// <summary>
        /// Initializes a new instance of the <see cref="CameraSystem"/> class.
        /// </summary>
        /// <param name="graphics">The graphics.</param>
        /// <param name="services">The services.</param>
        /// <param name="session">The session.</param>
        public CameraMovementSystem( IClientSession session)
        {   _session = session;
         
        }
        public void Update(long frame)
        {
            if (_positions.Count <= 0)
            {
                if(_returnToSender)//we need to get back
                {
                    var avatar = ((AvatarSystem)Manager.GetSystem(AvatarSystem.TypeId)).GetAvatar(_session.LocalPlayer.Number);
                    if (avatar <= 0)
                    {
                        return;
                    }
                    var cam = ((CameraSystem)Manager.GetSystem(CameraSystem.TypeId));
                    if (_frame == 0)
                    {
                        _lastPosition = cam.CameraPositon;
                        _lastZoom = cam.Zoom;
                        FarPosition desPosition;
                        var interpolation = (InterpolationSystem)Manager.GetSystem(InterpolationSystem.TypeId);
                        interpolation.GetInterpolatedPosition(avatar, out desPosition);

                        _step = (desPosition - _lastPosition) / _returnSpeed;
                        var desZoom = cam.CameraZoom;
                        if (Math.Abs(desZoom - 0f) < 0.0001f) //no zoom == stay where we are
                        {
                            desZoom = _lastZoom;
                        }

                        _zoomStep = (desZoom - _lastZoom) / _returnSpeed;
                    }
                    if (_frame == _returnSpeed)
                    {
                        _frame = 0;

                       
                        cam.ResetCamera();
                        cam.ResetZoom();
                        _returnToSender = false;

                    }
                    else
                    {
                        cam.CameraPositon += _step;
                        cam.Zoom += _zoomStep;
                        _frame++;
                    }


                }
            }
            else
            {


                var cam = ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId));
                if (_frame == 0)
                {
                    _lastPosition = cam.CameraPositon;
                    _lastZoom = cam.Zoom;
                    var desPosition = _positions[0].Destination;
                    if (desPosition.Equals(FarPosition.Zero))
                    {
                        desPosition = _lastPosition;
                    }
                    _step = (desPosition - _lastPosition)/_positions[0].Speed;
                    var desZoom = _positions[0].Zoom;
                    if (Math.Abs(desZoom - 0f) < 0.0001f) //no zoom == stay where we are
                    {
                        desZoom = _lastZoom;
                    }

                    _zoomStep = (desZoom - _lastZoom)/_positions[0].Speed;
                }
                if (_frame == _positions[0].Speed)
                {
                    _frame = 0;

                    _positions.RemoveAt(0);
                    if (_positions.Count == 0&&!_returnToSender)
                    {
                        cam.ResetCamera();
                        cam.ResetZoom();
                    }

                }
                else
                {
                    cam.CameraPositon += _step;
                    cam.Zoom += _zoomStep;
                    _frame++;
                }
            }
        }

        /// <summary>
        /// Receives the specified message.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The message.</param>
        public void Receive<T>(ref T message) where T : struct
         {
            if(message is MoveCamera)
            {
                Move((MoveCamera)(ValueType)message);    
            }
         }

        public void Move(MoveCamera move)
        {
            _returnToSender = move.Return;
            _returnSpeed = move.ReturnSpeed;
              
            _positions.AddRange(move.Position);
        }
    }
}
