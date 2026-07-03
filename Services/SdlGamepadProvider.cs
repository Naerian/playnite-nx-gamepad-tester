using GamepadTester.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace GamepadTester.Services
{
    public sealed class SdlGamepadProvider : IGamepadInputProvider
    {
        private readonly uint initFlags = Sdl2Native.SdlInitGameController | Sdl2Native.SdlInitHaptic | Sdl2Native.SdlInitEvents;
        private readonly object syncRoot = new object();
        private IntPtr controller;
        private int? selectedInstanceId;

        public SdlGamepadProvider()
        {
            Sdl2Native.SDL_Init(initFlags);
            LoadControllerMappings();
            OpenSelectedOrFirstController();
        }

        public GamepadState ReadState()
        {
            lock (syncRoot)
            {
                Sdl2Native.SDL_GameControllerUpdate();

                if (controller != IntPtr.Zero && Sdl2Native.SDL_GameControllerGetAttached(controller) == 0)
                {
                    CloseController();
                }

                if (controller == IntPtr.Zero)
                {
                    OpenSelectedOrFirstController();
                }

                if (controller == IntPtr.Zero)
                {
                    return new GamepadState();
                }

                var name = GetControllerName();
                ushort vendorId;
                ushort productId;
                GetDeviceIds(out vendorId, out productId);

                return new GamepadState
                {
                    IsConnected = true,
                    ControllerName = name,
                    VendorId = vendorId,
                    ProductId = productId,
                    Layout = DetectLayout(name, vendorId, productId),
                    EightBitDoModel = DetectEightBitDoModel(name, vendorId, productId),
                    LeftStick = new StickState
                    {
                        X = NormalizeAxis(Sdl2Native.SDL_GameControllerGetAxis(controller, SdlControllerAxis.LeftX)),
                        Y = -NormalizeAxis(Sdl2Native.SDL_GameControllerGetAxis(controller, SdlControllerAxis.LeftY))
                    },
                    RightStick = new StickState
                    {
                        X = NormalizeAxis(Sdl2Native.SDL_GameControllerGetAxis(controller, SdlControllerAxis.RightX)),
                        Y = -NormalizeAxis(Sdl2Native.SDL_GameControllerGetAxis(controller, SdlControllerAxis.RightY))
                    },
                    LeftTrigger = NormalizeTrigger(Sdl2Native.SDL_GameControllerGetAxis(controller, SdlControllerAxis.TriggerLeft)),
                    RightTrigger = NormalizeTrigger(Sdl2Native.SDL_GameControllerGetAxis(controller, SdlControllerAxis.TriggerRight)),
                    Buttons = new GamepadButtonState
                    {
                        South = IsPressed(SdlControllerButton.A),
                        East = IsPressed(SdlControllerButton.B),
                        West = IsPressed(SdlControllerButton.X),
                        North = IsPressed(SdlControllerButton.Y),
                        LeftShoulder = IsPressed(SdlControllerButton.LeftShoulder),
                        RightShoulder = IsPressed(SdlControllerButton.RightShoulder),
                        Back = IsPressed(SdlControllerButton.Back),
                        Start = IsPressed(SdlControllerButton.Start),
                        Guide = IsPressed(SdlControllerButton.Guide),
                        LeftStick = IsPressed(SdlControllerButton.LeftStick),
                        RightStick = IsPressed(SdlControllerButton.RightStick),
                        DpadUp = IsPressed(SdlControllerButton.DpadUp),
                        DpadDown = IsPressed(SdlControllerButton.DpadDown),
                        DpadLeft = IsPressed(SdlControllerButton.DpadLeft),
                        DpadRight = IsPressed(SdlControllerButton.DpadRight)
                    }
                };
            }
        }

        public IReadOnlyList<GamepadControllerInfo> GetControllers()
        {
            lock (syncRoot)
            {
                var controllers = new List<GamepadControllerInfo>();
                var count = Sdl2Native.SDL_NumJoysticks();

                for (var index = 0; index < count; index++)
                {
                    if (Sdl2Native.SDL_IsGameController(index) != 1)
                    {
                        continue;
                    }

                    var name = GetControllerNameForIndex(index);
                    var vendorId = Sdl2Native.SDL_JoystickGetDeviceVendor(index);
                    var productId = Sdl2Native.SDL_JoystickGetDeviceProduct(index);

                    controllers.Add(new GamepadControllerInfo
                    {
                        JoystickIndex = index,
                        InstanceId = Sdl2Native.SDL_JoystickGetDeviceInstanceID(index),
                        Name = name,
                        VendorId = vendorId,
                        ProductId = productId,
                        Layout = DetectLayout(name, vendorId, productId),
                        EightBitDoModel = DetectEightBitDoModel(name, vendorId, productId)
                    });
                }

                return controllers;
            }
        }

        public void SelectController(int instanceId)
        {
            lock (syncRoot)
            {
                if (selectedInstanceId.HasValue && selectedInstanceId.Value == instanceId)
                {
                    return;
                }

                selectedInstanceId = instanceId;
                CloseController();
                OpenSelectedOrFirstController();
            }
        }

        public bool TryRumble(ushort lowFrequency, ushort highFrequency, uint durationMs)
        {
            lock (syncRoot)
            {
                if (controller == IntPtr.Zero)
                {
                    return false;
                }

                return Sdl2Native.SDL_GameControllerRumble(controller, lowFrequency, highFrequency, durationMs) == 0;
            }
        }

        private void LoadControllerMappings()
        {
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gamecontrollerdb.txt"),
                Path.Combine(Environment.CurrentDirectory, "gamecontrollerdb.txt")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    var rw = Sdl2Native.SDL_RWFromFile(candidate, "rb");
                    if (rw != IntPtr.Zero)
                    {
                        Sdl2Native.SDL_GameControllerAddMappingsFromRW(rw, 1);
                    }

                    return;
                }
            }
        }

        private void OpenSelectedOrFirstController()
        {
            var count = Sdl2Native.SDL_NumJoysticks();
            if (selectedInstanceId.HasValue)
            {
                for (var index = 0; index < count; index++)
                {
                    if (Sdl2Native.SDL_IsGameController(index) == 1 &&
                        Sdl2Native.SDL_JoystickGetDeviceInstanceID(index) == selectedInstanceId.Value)
                    {
                        controller = Sdl2Native.SDL_GameControllerOpen(index);
                        if (controller != IntPtr.Zero)
                        {
                            return;
                        }
                    }
                }
            }

            for (var index = 0; index < count; index++)
            {
                if (Sdl2Native.SDL_IsGameController(index) == 1)
                {
                    controller = Sdl2Native.SDL_GameControllerOpen(index);
                    if (controller != IntPtr.Zero)
                    {
                        selectedInstanceId = Sdl2Native.SDL_JoystickGetDeviceInstanceID(index);
                        return;
                    }
                }
            }
        }

        private string GetControllerName()
        {
            var namePointer = Sdl2Native.SDL_GameControllerName(controller);
            var controllerName = Marshal.PtrToStringAnsi(namePointer);
            return controllerName ?? "Game controller";
        }

        private static string GetControllerNameForIndex(int joystickIndex)
        {
            var namePointer = Sdl2Native.SDL_GameControllerNameForIndex(joystickIndex);
            var controllerName = Marshal.PtrToStringAnsi(namePointer);
            return controllerName ?? "Game controller";
        }

        private bool IsPressed(SdlControllerButton button)
        {
            return Sdl2Native.SDL_GameControllerGetButton(controller, button) == 1;
        }

        private void GetDeviceIds(out ushort vendorId, out ushort productId)
        {
            vendorId = 0;
            productId = 0;

            var joystick = Sdl2Native.SDL_GameControllerGetJoystick(controller);
            if (joystick == IntPtr.Zero)
            {
                return;
            }

            vendorId = Sdl2Native.SDL_JoystickGetVendor(joystick);
            productId = Sdl2Native.SDL_JoystickGetProduct(joystick);
        }

        private static float NormalizeAxis(short value)
        {
            var normalized = value < 0 ? value / 32768f : value / 32767f;
            return Clamp(normalized, -1f, 1f);
        }

        private static float NormalizeTrigger(short value)
        {
            return Clamp(value / 32767f, 0f, 1f);
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            if (value > maximum)
            {
                return maximum;
            }

            return value;
        }

        private static GamepadLayout DetectLayout(string controllerName, ushort vendorId, ushort productId)
        {
            var name = (controllerName ?? string.Empty).ToLowerInvariant();

            // 8BitDo's USB vendor ID is 0x2DC8. Some models in XInput mode still present an Xbox-like name.
            if (vendorId == 0x2DC8 || name.Contains("8bitdo"))
            {
                return GamepadLayout.EightBitDo;
            }

            if ((vendorId == 0x057E && productId == 0x2009) || name.Contains("switch pro") || name.Contains("nintendo switch pro"))
            {
                return GamepadLayout.SwitchPro;
            }

            if (name.Contains("dualshock") || name.Contains("dualsense") || name.Contains("playstation") || name.Contains("wireless controller"))
            {
                return GamepadLayout.PlayStation;
            }

            if (name.Contains("xbox") || name.Contains("xinput"))
            {
                return GamepadLayout.Xbox;
            }

            return GamepadLayout.Generic;
        }

        private static EightBitDoModel DetectEightBitDoModel(string controllerName, ushort vendorId, ushort productId)
        {
            var name = (controllerName ?? string.Empty).ToLowerInvariant();

            if (vendorId != 0x2DC8 && !name.Contains("8bitdo"))
            {
                return EightBitDoModel.Unknown;
            }

            if (productId == 0x3019 || name.Contains("8bitdo 64"))
            {
                return EightBitDoModel.Controller64;
            }

            if (productId == 0x6009 || name.Contains("pro 3"))
            {
                return EightBitDoModel.Pro3;
            }

            if (productId == 0x301B || productId == 0x301C || productId == 0x301D || name.Contains("ultimate 2c"))
            {
                return EightBitDoModel.Ultimate2CWireless;
            }

            if (productId == 0x310B || productId == 0x6012 || productId == 0x3011 || productId == 0x3012 || productId == 0x3013 || name.Contains("ultimate 2") || name.Contains("ultimate"))
            {
                return EightBitDoModel.Ultimate2Wireless;
            }

            return EightBitDoModel.Unknown;
        }

        private void CloseController()
        {
            if (controller != IntPtr.Zero)
            {
                Sdl2Native.SDL_GameControllerClose(controller);
                controller = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            CloseController();
            Sdl2Native.SDL_QuitSubSystem(initFlags);
        }
    }
}
