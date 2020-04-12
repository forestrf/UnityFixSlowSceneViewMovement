using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

/*
When pressing a keyboard key the operative system repeats the key press many times.
Unity decides to queue all of those repeated key presses when moving in the scene view and generates several "slow" events
that trigger OnGUI Repaints and more. So many events that the queue keeps growing if the computer can't process all of them
fast enough, to the point of keep moving after long after the keys have been released.

Events that get queued until all of them are performed. If your computer is not fast enough, it means that the editor will keep
on executing those key presses long after you stopped touching the keyboard.

This scripts changes the Windows settings of the keyboard, slowing down as much as it can the settings of key press repeat speed,
while moving on the scene. Then restores the previous settings.
*/

namespace Ashkatchap {
	public class OptimizeSceneView {
		[InitializeOnLoadMethod]
		static void Init() {
			SceneView.beforeSceneGui += new OptimizeSceneView().BeforeSceneGUI;
		}

		KeyboardRepetition keyboardRepetition = new KeyboardRepetition();
		void BeforeSceneGUI(SceneView sceneView) {
			Event current = Event.current;

			switch (current.type) {
				case EventType.MouseDown:
					keyboardRepetition.Disable();
					break;
				case EventType.MouseUp:
				case EventType.DragExited:
				case EventType.ContextClick:
				case EventType.MouseLeaveWindow:
					keyboardRepetition.Restore();
					break;
			}
		}

		~OptimizeSceneView() {
			keyboardRepetition.Restore();
		}

		class KeyboardRepetition {
			const int SPI_GETKEYBOARDDELAY = 0x0016;
			const int SPI_SETKEYBOARDDELAY = 0x0017;
			const int SPI_GETKEYBOARDSPEED = 0x000A;
			const int SPI_SETKEYBOARDSPEED = 0x000B;

			int originalKeyboardDelay;
			int originalKeyboardSpeed;

			public KeyboardRepetition() {
				Read();
			}

			public unsafe void Read() {
				fixed (int* oldPtr = &originalKeyboardDelay) {
					bool success = SystemParametersInfoA(SPI_GETKEYBOARDDELAY, 0, new IntPtr(oldPtr), 0);
				}
				fixed (int* oldPtr = &originalKeyboardSpeed) {
					bool success = SystemParametersInfoA(SPI_GETKEYBOARDSPEED, 0, new IntPtr(oldPtr), 0);
				}
			}
			public unsafe void Disable() {
				int fixKeyboardDelay = 3;
				int* oldPtr = &fixKeyboardDelay;
				bool success = SystemParametersInfoA(SPI_SETKEYBOARDDELAY, 0, new IntPtr(oldPtr), 0);

				int fixKeyboardSpeed = 0;
				oldPtr = &fixKeyboardSpeed;
				success = SystemParametersInfoA(SPI_SETKEYBOARDSPEED, 0, new IntPtr(oldPtr), 0);
			}
			public unsafe void Restore() {
				fixed (int* oldPtr = &originalKeyboardDelay) {
					bool success = SystemParametersInfoA(SPI_SETKEYBOARDDELAY, 0, new IntPtr(oldPtr), 0);
				}
				fixed (int* oldPtr = &originalKeyboardSpeed) {
					bool success = SystemParametersInfoA(SPI_SETKEYBOARDSPEED, 0, new IntPtr(oldPtr), 0);
				}
			}


			[DllImport("user32.dll", SetLastError = true)]
			static extern bool SystemParametersInfoA(int uiAction, int uiParam, IntPtr pvParam, int fWinIni);
		}
	}
}
