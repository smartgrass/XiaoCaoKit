using System;
using UnityEngine;

namespace MFPC.Input.PlayerInput
{
    public interface IPlayerInput
    {
        public float CalculatedHorizontalLookDirection { get; }
        public float CalculatedVerticalLookDirection { get; }
        public float LeanDirection { get; }
        bool IsSprint { get; }
        Vector2 MoveDirection { get; }
        event Action OnJumpAction;
        event Action OnSitAction;
    }
}