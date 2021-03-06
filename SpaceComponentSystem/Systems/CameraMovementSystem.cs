﻿using System;
using System.Collections.Generic;
using Engine.ComponentSystem.Messages;
using Engine.ComponentSystem.Spatial.Systems;
using Engine.ComponentSystem.Systems;
using Engine.FarMath;
using Engine.Serialization;
using Space.ComponentSystem.Messages;

namespace Space.ComponentSystem.Systems
{
    [Packetizable(false), PresentationOnlyAttribute]
    public class CameraMovementSystem : AbstractSystem
    {
        [PacketizeIgnore]
        private List<MoveCamera.Positions> _positions = new List<MoveCamera.Positions>();

        private long _frame;
        private FarPosition _lastPosition;
        private FarPosition _step;
        private float _lastZoom;
        private float _zoomStep;
        private bool _returnToSender;
        private long _returnSpeed;

        [MessageCallback]
        public void OnDraw(Draw message)
        {
            if (_positions.Count <= 0)
            {
                if (_returnToSender) //we need to get back
                {
                    var avatar = ((LocalPlayerSystem) Manager.GetSystem(LocalPlayerSystem.TypeId)).LocalPlayerAvatar;
                    if (avatar <= 0)
                    {
                        return;
                    }
                    var cam = ((CameraSystem) Manager.GetSystem(CameraSystem.TypeId));
                    if (_frame == 0)
                    {
                        _lastPosition = cam.CameraPosition;
                        _lastZoom = cam.Zoom;
                        FarPosition desPosition;
                        float angle;
                        var interpolation = (InterpolationSystem) Manager.GetSystem(InterpolationSystem.TypeId);
                        interpolation.GetInterpolatedTransform(avatar, out desPosition, out angle);

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
                        cam.CameraPosition += _step;
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
                    _lastPosition = cam.CameraPosition;
                    _lastZoom = cam.Zoom;
                    var desPosition = _positions[0].Destination;
                    if (desPosition.Equals(FarPosition.Zero))
                    {
                        desPosition = _lastPosition;
                    }
                    _step = (desPosition - _lastPosition) / _positions[0].Speed;
                    var desZoom = _positions[0].Zoom;
                    if (Math.Abs(desZoom - 0f) < 0.0001f) //no zoom == stay where we are
                    {
                        desZoom = _lastZoom;
                    }

                    _zoomStep = (desZoom - _lastZoom) / _positions[0].Speed;
                }
                if (_frame == _positions[0].Speed)
                {
                    _frame = 0;

                    _positions.RemoveAt(0);
                    if (_positions.Count == 0 && !_returnToSender)
                    {
                        cam.ResetCamera();
                        cam.ResetZoom();
                    }
                }
                else
                {
                    cam.CameraPosition += _step;
                    cam.Zoom += _zoomStep;
                    _frame++;
                }
            }
        }

        [MessageCallback]
        public void OnMoveCamera(MoveCamera message)
        {
            Move(message);
        }

        public void Move(MoveCamera move)
        {
            _returnToSender = move.Return;
            _returnSpeed = move.ReturnSpeed;

            _positions.AddRange(move.Position);
        }
    }
}