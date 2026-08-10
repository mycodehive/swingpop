using System.Reflection;
using NUnit.Framework;
using SwingPop.Gameplay.Shot;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SwingPop.Tests
{
    public sealed class ShotInputControllerTests
    {
        private GameObject host;
        private Keyboard keyboard;
        private bool ownsKeyboard;

        [SetUp]
        public void SetUp()
        {
            keyboard = Keyboard.current;
            if (keyboard == null)
            {
                keyboard = InputSystem.AddDevice<Keyboard>();
                ownsKeyboard = true;
            }

            host = new GameObject("Shot Input Controller Test");
            ShotInputController controller = host.AddComponent<ShotInputController>();
            typeof(ShotInputController).GetMethod(
                    "Awake",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(controller, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            if (ownsKeyboard && keyboard != null)
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [TestCase("noSpinAction", Key.Digit1, Key.Numpad1)]
        [TestCase("topSpinAction", Key.Digit2, Key.Numpad2)]
        [TestCase("backSpinAction", Key.Digit3, Key.Numpad3)]
        [TestCase("leftSideSpinAction", Key.Digit4, Key.Numpad4)]
        [TestCase("rightSideSpinAction", Key.Digit5, Key.Numpad5)]
        public void SpinPresetAction_ResolvesMainRowAndNumpadKeys(
            string actionFieldName,
            Key mainRowKey,
            Key numpadKey)
        {
            ShotInputController controller = host.GetComponent<ShotInputController>();
            FieldInfo actionField = typeof(ShotInputController).GetField(
                actionFieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(actionField, Is.Not.Null);
            InputAction action = actionField.GetValue(controller) as InputAction;
            Assert.That(action, Is.Not.Null);
            Assert.That(action.controls, Does.Contain(keyboard[mainRowKey]));
            Assert.That(action.controls, Does.Contain(keyboard[numpadKey]));
            Assert.That(action.bindings.Count, Is.EqualTo(2));
        }
    }
}
