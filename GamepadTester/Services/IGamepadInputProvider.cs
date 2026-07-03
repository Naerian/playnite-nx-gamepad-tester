using GamepadTester.Models;
using System;
using System.Collections.Generic;

namespace GamepadTester.Services
{
    public interface IGamepadInputProvider : IDisposable
    {
        GamepadState ReadState();
        IReadOnlyList<GamepadControllerInfo> GetControllers();
        void SelectController(int instanceId);
        bool TryRumble(ushort lowFrequency, ushort highFrequency, uint durationMs);
    }
}
